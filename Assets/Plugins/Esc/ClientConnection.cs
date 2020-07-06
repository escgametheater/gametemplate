using UnityEngine;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using Esc;
using Osc;

namespace Esc {

	/**
	 * The ClientConnection is a singleton that is the primary interface for
	 * the server and the local connection with the server. It can be used
	 * to transmit its own state information. It also performs the role of 
	 * listening for incoming messages from the server. It parses and reforms 
	 * them as EscEvent instances (that must be processed).
	 * 
	 * The ClientConnection object contains a reference to client and server objects.
	 * It handles automatically registering with the server as it becomes connected.
	 * It also must be used for initializing a controller with the game after it 
	 * recieves game information. It provides callbacks for handling connection 
	 * status changes from game engine itself.
	 * 
	 * NOTE: When the ClientConnection is instantiated it creates a GameObject
	 * and adds itself as a component so that it remains persistent within the
	 * controller as long as it is running.
	 * 
	 * @see Esc.Client
	 * @see Esc.Server
	 * @see Esc.EscEvent
	 * @see Esc.StateListener
	 */
	public class ClientConnection : MonoBehaviour {

		//! The local game server address
		private static string DEFAULT_SERVER_ADDRESS = "esc-game-server.local";

		private static string GAME_ENGINE = "game-engine";
		private static string GAME_LAUNCHER = "game-launcher";
		private static string CONNECTION_NAME = "ConnectionGameObject";

		//! Connection event Callback type
		public delegate void EventCallbackAction();
		
		//! Callback associated with the client becoming connected
		public event EventCallbackAction OnConnected;
		
		//! Callback associated with the client becoming registered
		public event EventCallbackAction OnRegistered;
		
		//! Callback associated with the client becoming initialized
		public event EventCallbackAction OnInitialized;
		
		//! Callback associated with the client becoming disconnected
		public event EventCallbackAction OnDisconnected;

		//! Callback associated with the device being plugged into a power source and charging
		public event EventCallbackAction OnPlugged;

		//! Callback associated with the device being unplugged from a power source
		public event EventCallbackAction OnUnplugged;

		//! Callback associated with the device losing its wireless connection
		public event EventCallbackAction OnWifiDisconnected;

		//! Callback associated with the device losing regaining its wireless connection
		public event EventCallbackAction OnWifiConnected;

		//! Presence event Callback type
		public delegate void PresenceEventAction(int pEvent);
		
		//! Callback associated with the client receiving a presence notification from the game server
		public event PresenceEventAction OnHandlePresence;

		//! API constant event name used for registration
		protected const string API_KEYWORD_REGISTERED = "registered:";
		//! API constant event name used for initialization
		protected const string API_KEYWORD_INIT = "init:";
		//! API constant event name used to tell controller which game skin to load
		private static string API_KEYWORD_GAME_ID = "game:";

		//! stores the server address as a local domain name
		private static string serverAddress = "";

		//! stores time accumulation to determine when the next state update should be dispatched
		private float cumulativeStateTime = 0.0f;

		//! stores frequency of state updates. default value set to once per frame at 30 fps
		private float stateUpdateFrequency = 1.0f / 30.0f; 

		//! stores time accumulation to determine when the next device update should be dispatched
		private float cumulativeDeviceTime = 0.0f;

		//! stores interval of device updates. default value set to 10 seconds
		private float deviceUpdateFrequency = 10.0f;

		//! local client representation
		public Client client;
		
		//! local server representation
		public Server server;

		//! local state variables listener
		private StateListener stateListener; 
		
		//! flag that indicates whether or not the client should always try to reconnect
		private bool autoReconnect = true;
		
		//! flag that indicates whether or not the client will attempt to reconnect at the next update step
		private bool willReconnect = false;
		
		//! stores the local IPv4 address of the client device
		private string clientIpAddress;
		
		//! stores the currently running game ID
		private string gameId;
		
		//! stores the currently running Unity3D scene name
		private string gameSceneName;
		
		//! stores the device's unique ID (permanent UDID for iOS, serial number for a desktop computer)
		private string clientUdid = "";
		
		//! stores the index given by the server; used to efficiently multiplex future communications
		private int clientIndex;

		//! Create a singleton instance of the Client Connection class and adds itself as a component to remain persistent as long as the controller is running 
		private static ClientConnection _instance;
		public static ClientConnection Instance {
			get {
				if (!_instance) {
					_instance = GameObject.FindObjectOfType(typeof(ClientConnection)) as ClientConnection;
					if (!_instance) {
						GameObject container = new GameObject();
						container.name = CONNECTION_NAME;
						_instance = container.AddComponent(typeof(ClientConnection)) as ClientConnection;

						// add components for other monobehavior elements...
					}
				}
				
				return _instance;
			}
		}

		//! Constructor
		public ClientConnection ()
		{
			//this.gameSceneName = Application.loadedLevelName; 
			//this.gameSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene ().name;
		}

		//! Destructor
		~ClientConnection () 
		{
		}

		//! Returns true if Controller Interface is connected to the server, false otherwise
		public bool IsConnected () 
		{
			return ControllerInterface.isConnected();
		}

		//! Get the server address the the client will connect with 
		public string GetServerAddress () 
		{
			return ClientConnection.serverAddress;
		}

		/** 
		 * Set the server address the the client will connect with
		 * 
		 * @param address the server endpoint local domain name
		 */
		public static void SetServerAddress ( string address ) 
		{
			ClientConnection.serverAddress = address;
		}

		/** 
		 * Dispatch a custom EscEvent message to the specified network peer
		 * 
		 * @param evt an event to dispatch
		 * @param endpoint the network peer that will receive the event
		 * @return zero on success, failure otherwise
		 */
		public int DispatchEventToClient (EscEvent evt, Peer endpoint) 
		{
			return ControllerInterface.dispatchEvent(endpoint.Username(), evt.ToString());
		}

		/** 
		 * Dispatch a custom EscEvent to the game launcher
		 * 
		 * @param evt an event to dispatch
		 * @return zero on success, failure otherwise
		 */
		public int DispatchEventToGameLauncher (EscEvent evt) 
		{
			return ControllerInterface.dispatchEvent(GAME_LAUNCHER, evt.ToString());
		}

		/**
		 * Initialize the client controller interface with the client's unique identifier and the server address.
		 * Also, initializes a state listener to handle state updates
		 * 
		 * @see Esc.Server
		 * @see Esc.StateListner
		 */
		public void Awake () 
		{
			this.gameSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene ().name;
			this.server = new Server(GAME_ENGINE);
			
			this.clientIpAddress = Network.player.ipAddress; 
			if ("" == ClientConnection.serverAddress) {
				ClientConnection.serverAddress = DEFAULT_SERVER_ADDRESS;
			}

			this.stateListener = new StateListener(); 
			this.stateListener.HandleMessage += HandleStateUpdate;
			this.stateListener.Connect();
			
			this.client = new Client(this.Udid());
			
			ControllerInterface.startClientInterface(this.clientUdid, ClientConnection.serverAddress);
		}

		/**
		 * Handles any client connection changes that occur during each frame update.
		 * If the state listener is connected, it will call its update function.
		 * If the Controller Interface has any status changes, it will invoke the OnConnected and OnDisconnected callbacks accordingly.
		 * If the Controller Interface has any presence events, it will invoke the OnHandlePresence callback.
		 * If the Controller Interface receives a game registration event message it will invoke the OnRegistered callback. 
		 * If the Controller Interface receives a game registration event message it will invoke the OnInitialized callback.
		 * Consumes network events and converts them to EscEvent objects to be processed by the controller.
		 * Will propagate any client state changes to the server while respecting the state update frequency.
		 *
		 * @see Esc.Client
		 * @see Esc.StateListener
		 * @see Esc.ClientConnection.SetStateUpdateFrequency
		 */
		public void Update () 
		{
			// consume new state changes
			if ( this.stateListener.IsConnected() ) {
				this.stateListener.Update ();
			}

			// attempt a reconnection if necessary
			if ( this.willReconnect ) {
				this.willReconnect = false;
				ControllerInterface.startClientInterface(this.clientUdid, ClientConnection.serverAddress);
			}

			// process status changes
			while (ControllerInterface.hasMoreStatusChanges()) {
				int stat;
				if (0 == ControllerInterface.getNextStatusChange( out stat )) {
					switch (stat) {
					case 0: // connected
						break;
					case 1: // TLS connected
						break;
					case 2: // disconnected
						if (null != OnDisconnected) {
							OnDisconnected();
						}
						if ( this.autoReconnect ) willReconnect = true;
						break;
					case 3: // roster loaded
						if (null != OnConnected) {
							OnConnected();
						}
						break;
					case 4: // error
						break;
					default:
						break;
					}
				}
			}

			// process presence events
			while (ControllerInterface.hasMorePresenceEvents()) {
				StringBuilder user = new StringBuilder(48);
				int presence;
				if (0 == ControllerInterface.getNextPresenceEvent( user, (uint) user.Capacity, out presence )) {
					string username = user.ToString();
					this.server = new Server(username);
					if ( this.server.Username() == username ) {
						this.server.SetStatus( presence );
						OnHandlePresence(presence);

					}
				}
			}

			// process all incoming events
			while (ControllerInterface.hasMoreEvents()) {
				StringBuilder user = new StringBuilder(48);
				StringBuilder message = new StringBuilder(256);
				if (0 == ControllerInterface.getNextEvent( user, (uint) user.Capacity, message, (uint) message.Capacity )) {
					string username = user.ToString();
					string msgString = message.ToString();

					// handle registration
					if (msgString.StartsWith( API_KEYWORD_REGISTERED )) {
						string[] commaDelimiter = new string[] {","};
						string parameterString = msgString.Substring(API_KEYWORD_REGISTERED.Length);
						string[] parameters = parameterString.Split(commaDelimiter, StringSplitOptions.None);
						if ( parameters.Length != 3 ) {
							Debug.LogError("ClientConnection: Registration message is malformed.");
						}

						// Establish connection to server with the remote IP address
						string remoteIpAddress = parameters[0]; 
						this.client.SetRemoteEndpoint(remoteIpAddress);

						this.clientIndex = Convert.ToInt32( parameters[1] );
						this.gameId = parameters[2];

						// Load different level if game id is different than the game scene name
						if (!this.gameId.Equals(this.gameSceneName)){
							if (Application.CanStreamedLevelBeLoaded(this.gameId)) {
								Application.LoadLevel(this.gameId); 
							}
							break;
						}

						string registrationMessage = API_KEYWORD_REGISTERED + this.clientIpAddress;
						ControllerInterface.dispatchEvent(username, registrationMessage);

						if (null != OnRegistered) {
							OnRegistered();
						}


					}
					// handle initialization
					else if (msgString.StartsWith( API_KEYWORD_INIT )) {
						if (null != OnInitialized) {
							OnInitialized();
						}
					}
					// handle loading another game 
					else if (msgString.StartsWith (API_KEYWORD_GAME_ID)){
						string parameterString = msgString.Substring(API_KEYWORD_GAME_ID.Length);
						if (!parameterString.Equals(this.gameSceneName)){
							if (Application.CanStreamedLevelBeLoaded(parameterString)) {
								Application.LoadLevel(parameterString); 
							}
						}
					}
					
					EscEvent evt = EscEvent.FromString( msgString );
					this.server.AppendEvent( evt );
				}
			}
			
			// check frequency of state update frequency
			if (cumulativeStateTime > stateUpdateFrequency){
				if (null != this.client && this.client.StateVarsAreStale()) {
					this.client.PropagateStateVars(clientIndex);
				}
				cumulativeStateTime = 0.0f;
			}
			else {
				cumulativeStateTime += Time.deltaTime;
			}

			// check interval of device update frequency
			if (cumulativeDeviceTime > deviceUpdateFrequency){
				if ( ControllerInterface.isDeviceCharging() ) {
					if (null != OnPlugged) {
						OnPlugged();
					}
					ControllerInterface.dimScreen();
				}
				else {
					if (null != OnUnplugged) {
						OnUnplugged();
					}
					if ( ControllerInterface.isDeviceWifiConnected() ) {
						if (null != OnWifiConnected) {
							OnWifiConnected();
						}
						ControllerInterface.brightenScreen();
					}
					else {
						if (null != OnWifiDisconnected) {
							OnWifiDisconnected();
						}
						ControllerInterface.dimScreen();
					}
				}
				cumulativeDeviceTime = 0.0f;
			}
			else {
				cumulativeDeviceTime += Time.deltaTime;
			}
		}

		/** 
		 * Callback handler from the StateListener that maps the incoming
		 * key/value pairs to the server key/value pairs.
		 * 
		 * @param keys the string array of keys from the StateListener
		 * @param args the corresponding values for the keys
		 */
		private void HandleStateUpdate (string[] keys, ArrayList args) 
		{
			this.server.UpdateStateVars(keys, args);
		}

		//! Returns the string value of the client unique identifier
		public string Udid () 
		{
			if ("" == this.clientUdid) {
				this.clientUdid = ControllerInterface.getUniqueDeviceIdentifier();
			}
			
			return this.clientUdid;
		}

		//! Returns the string value of the scene name
		public string GameSceneName () 
		{	
			return this.gameSceneName;
		}

		//! Removes the client connection object
		public void OnDestroy () 
		{
			this.willReconnect = false;
			this.autoReconnect = false;
			this.client = null;
			ControllerInterface.stopInterface();
		}

		//! reset screen brightness when application quits
		void OnApplicationQuit () 
		{
			ControllerInterface.brightenScreen();
		}

		//! Set the number of times per second we want to propagate state key/value changes
		public void SetStateUpdateFrequency (uint frequency)
		{
			if (frequency < 1) {
				stateUpdateFrequency = 1.0f / 30.0f; 
			} 
			else {
				stateUpdateFrequency = 1.0f / (float) frequency; 
			}
		}

		//! Set the interval in number of seconds at which we want to check the device battery/wifi states
		public void SetDeviceUpdateFrequency (uint frequency)
		{
			deviceUpdateFrequency = (float) frequency;
		}

	}

}