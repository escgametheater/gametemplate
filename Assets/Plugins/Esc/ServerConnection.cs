using UnityEngine;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Xml;
using Esc;
using Osc;

namespace Esc
{

	/**
	 * The ServerConnection is a singleton that is the primary interface for
	 * the controller clients that will connect to the game. It can be used
	 * to transmit its own state information. It also performs the role of 
	 * listening for incoming messages and mapping them to the client objects
	 * and re-forming them as EscEvent instances (that must be processed).
	 * 
	 * The ServerConnection object contains a list of clients that are available.
	 * It handles automatically registering those clients as they become connected.
	 * It also must be used for initializing controllers with information about
	 * the current game. It provides callbacks for handling connection status
	 * changes from the clients and the game engine itself.
	 * 
	 * NOTE: When the ServerConnnection is instantiated it creates a GameObject
	 * and adds itself as a component so that it remains persistent within the
	 * game as long as it is running.
	 * 
	 * @see Esc.Client
	 * @see Esc.Server
	 * @see Esc.EscEvent
	 * @see Esc.StateListener
	 */
	public class ServerConnection : MonoBehaviour
	{

		//! Connection event Callback type
		public delegate void EventCallbackAction ();

		//! Callback associated with the client becoming connected
		public event EventCallbackAction OnConnected;

		//! Callback associated with the client becoming disconnected
		public event EventCallbackAction OnDisconnected;

		//! Callback associated with the docent remote controller submitting new parameters
		public event EventCallbackAction OnParametersUpdated;

		//! Callback associated with the docent remote controller sending next event
		public event EventCallbackAction OnNextScreen;

		//! Client Connection event callback type
		public delegate void ClientCallbackAction (Client client);

		//! Callback associated with the client becoming connected
		public event ClientCallbackAction OnClientConnected;

		//! Callback associated with the client becoming registered
		public event ClientCallbackAction OnClientRegistered;

		//! Callback associated with the client becoming disconnected
		public event ClientCallbackAction OnClientDisconnected;

		//! Client Initialization event callback type
		public delegate void ClientInitCallbackAction (Client client,EscEvent evt);

		//! Callback associated with the client becoming initialized
		public event ClientInitCallbackAction OnClientInitialized;

		//! Launcher event callback type
		public delegate void LauncherCallbackAction (string gameId);

		//! Callback associated with the game starting
		public event LauncherCallbackAction OnGameStart;

		//! Callback associated with the game pausing
		public event LauncherCallbackAction OnGamePause;

		//! Callback associated with the game stopping
		public event LauncherCallbackAction OnGameStop;

		//! Callback associated with the game ending
		public event LauncherCallbackAction OnGameEnd;

		//! API constant event name used for registration
		protected const string API_KEYWORD_REGISTERED = "registered:";

		//! API constant event name used for initialization
		protected const string API_KEYWORD_INIT = "init:";

		//! API constant event name used for starting a game
		protected const string API_KEYWORD_START = "start:";

		//! API constant event name used for pausing
		protected const string API_KEYWORD_PAUSE = "pause:";

		//! API constant event name used for stopping a game
		protected const string API_KEYWORD_STOP = "stop:";

		//! API constant event name used for stopping a game
		protected const string API_KEYWORD_APPLY_PARAMS = "applyParams:";

		//! API constant for the docent next signal
		protected const string KEYWORD_NEXT = "next:";

		//! API constant for the docent check game loaded
		protected const string KEYWORD_CHECK_IF_LOADED = "checkIfLoaded";

		//! API constant event name used for termination
		protected const string API_KEYWORD_QUIT = "quit:";

		//! API constant event name used for ending the game
		protected const string API_KEYWORD_GAME_END = "gameEnd";

		//! API constant event name used for starting the game
		protected const string API_KEYWORD_GAME_START = "gameStart";

		//! API constant event name used for pausing the game
		protected const string API_KEYWORD_GAME_PAUSED = "gamePaused";

		//! API constant event name used for telling the docent game has been loaded
		protected const string API_KEYWORD_GAME_PREVIOUSLY_LOADED = "gamePreviouslyLoaded:";

		//! Constant for the game engine ID
		protected const string KEYWORD_GAME_ENGINE = "game-engine";

		//! Constant for the game launcher ID
		protected const string KEYWORD_GAME_LAUNCHER = "game-launcher";

		//! Constant for the docent name
		protected const string KEYWORD_DOCENT = "docent";

		//! A collection of automatically discovered controller clients.
		//! The contents of this list may only be added to, but never deleted from.
		public readonly List<Client> clients;

		//! the start round recieved from the game launcher
		protected int gameStartRound = 1;

		//! the difficulty recieved from the game launcher
		protected int gameDifficulty = 1;

		//! the first string parameter received from the docent remote
		protected string gameStringParameter1 = "";
		//! the second string parameter received from the docent remote
		protected string gameStringParameter2 = "";
		//! the third string parameter received from the docent remote
		protected string gameStringParameter3 = "";

		//! the first integer parameter received from the docent remote
		protected int gameModeParameter1 = -1;
		//! the second integer parameter received from the docent remote
		protected int gameModeParameter2 = -1;
		//! the third integer parameter received from the docent remote
		protected int gameModeParameter3 = -1;

		//! the connected Game's ID
		public string gameId;

		//! the connected Game has started
		public bool gameStarted = false;

		//! the connected Game is paused
		public bool gamePaused = false;

		//! local store for state listener
		private StateListener stateListener;

		//! Flag used to attempt to automatically reconnect upon a failed attempt
		protected bool autoReconnect = true;
		//! Flag that signifies that the instance will attempt a reconnection during the next frame update
		protected bool willReconnect = false;
		//! Stores the local server IP address
		private string serverIpAddress;

		// ESC Analytics
		//private EscAnalytics analytics;

		//! Timer that accumulates every frame
		protected float cumulativeTime = 0.0f;
		protected float launchDelay = 1.0f;

		//! the connected Game is paused
		private bool gameFullScreen = false;

		//! Developer mode default is false, if set to true, full screen will have keyboard access and app title bar
		private static bool developerMode = false;

		/** 
		 * A map that associates a client username with its associated Client instance in the public clients member
		 * @see ServerConnection::clients
		 */
		private Dictionary<string, uint> clientUsernameMap;

		//! private instance of the server peer object
		private Server server;

		//! Provides a Singleton Instance of this class.
		private static ServerConnection _instance;

		public static ServerConnection Instance {
			get {
				if (!_instance) {
					_instance = GameObject.FindObjectOfType (typeof(ServerConnection)) as ServerConnection;
					if (!_instance) {
						GameObject container = new GameObject ();
						container.name = "ConnectionGameObject";
						_instance = container.AddComponent (typeof(ServerConnection)) as ServerConnection;

						// Can add components for other monobehavior elements
					}
				}
				//	LoadSave.Load ("messagingCount");
				return _instance;
			}
		}

		/**
		 * Sets the internal DeveloperMode state to TRUE. Currently this is needed only to circumvent a problem
		 * where Unity3D stops reporting keyboard events to OSX when the fullscreen mode is set using the native
		 * hooks provided by Cocoa.
		 * 
		 * NOTE: this method MUST be called prior to the first instantiation of the ServerConnection singleton.
		 * 
		 * @see ServerConnection::DisableDeveloperMode
		 * @see ServerConnection::IsDeveloperModeEnabled
		 */
		public static void EnableDeveloperMode ()
		{
			developerMode = true;
		}

		/**
		 * Sets the internal DeveloperMode state to FALSE. 
		 * 
		 * NOTE: this method MUST be called prior to the first instantiation of the ServerConnection singleton.
		 * 
		 * @see ServerConnection::EnableDeveloperMode
		 * @see ServerConnection::IsDeveloperModeEnabled
		 */
		public static void DisableDeveloperMode ()
		{
			developerMode = false;
		}

		/**
		 * Returns the current state of the DeveloperMode flag.
		 * 
		 * @see ServerConnection::DisableDeveloperMode
		 * @see ServerConnection::IsDeveloperModeEnabled
		 */
		public static bool IsDeveloperModeEnabled ()
		{
			return developerMode;
		}

		//! The class constructor initializes internal structures
		public ServerConnection ()
		{
			this.clientUsernameMap = new Dictionary<string, uint> ();
			this.clients = new List<Client> ();
		}

		//! Class destructor
		~ServerConnection ()
		{
		}

		//! Returns the starting round received from the game launcher
		public int StartingRound ()
		{
			return this.gameStartRound;
		}

		//! Returns the game difficulty received from the game launcher
		public int GameDifficulty ()
		{
			return this.gameDifficulty;
		}

		//! Returns the first mode selection made by the docent as an integer (in the domain [0-2] or -1 if no selection has been made)
		public int GetFirstModeOption ()
		{
			return this.gameModeParameter1;
		}

		//! Returns the second mode selection made by the docent as an integer (in the domain [0-2] or -1 if no selection has been made)
		public int GetSecondModeOption ()
		{
			return this.gameModeParameter2;
		}

		//! Returns the third mode selection made by the docent as an integer (in the domain [0-2] or -1 if no selection has been made)
		public int GetThirdModeOption ()
		{
			return this.gameModeParameter3;
		}

		//! Returns the first string parameter input submitted by the docent (or an empty string if no such parameter was sent)
		public string GetFirstParameterString ()
		{
			return this.gameStringParameter1;
		}

		//! Returns the second string parameter input submitted by the docent (or an empty string if no such parameter was sent)
		public string GetSecondParameterString ()
		{
			return this.gameStringParameter2;
		}

		//! Returns the third string parameter input submitted by the docent (or an empty string if no such parameter was sent)
		public string GetThirdParameterString ()
		{
			return this.gameStringParameter3;
		}

		/**
		 * Sends 1-3 parameter strings to the docent remote to serve as acceptable defaults. This provides an opportunity
		 * for a game administrator to custom tailor the game experience to the current audience. Each game should document
		 * what parameters they are able to accept and what effect those parameters will have so an administrator will know
		 * what to expect of their inputs.
		 * 
		 * @param parameter1 the first string option 
		 * @param parameter2 the optional second string option
		 * @param parameter3 the optional third string option
		 * @return TRUE if successfully sent, FALSE otherwise
		 */
		public bool SubmitParameterStrings (string parameter1, string parameter2 = "", string parameter3 = "")
		{
			bool eventSent = false;

			if (GameInterface.isConnected ()) {
				//	LoadSave.savedData.Add (Time.time.ToString () + " : SubmitParameterStrings \n");
				string options = API_KEYWORD_APPLY_PARAMS + "param1=" + parameter1 + ",param2=" + parameter2 + ",param3=" + parameter3;
				if (0 != GameInterface.dispatchEvent (KEYWORD_DOCENT, options)) {
					Debug.LogWarning ("parameter strings message delivery failed.");
				} else {
					eventSent = true;
				}
			} else {
				Debug.LogWarning ("game not yet connected");
			}

			return eventSent;
		}

		//! Returns indication of the Esc-Unity-Plugin connection to the XMPP server.
		public bool IsConnected ()
		{
			return GameInterface.isConnected ();
		}

		//! Command to interface which dispatches start of game event to all controller clients.
		public void StartGame ()
		{
			foreach (Client client in this.clients) {
				if (!client.IsReady ()) {
					client.SetStatus (5);
					client.SetReadiness (false);
					//	LoadSave.savedData.Add (Time.time.ToString () + " : OUT :" + API_KEYWORD_GAME_END + "\n");
					GameInterface.dispatchEvent (client.Username (), API_KEYWORD_GAME_END);
				} else {
					//	LoadSave.savedData.Add (Time.time.ToString () + " : OUT :" + API_KEYWORD_GAME_START + "\n");
					GameInterface.dispatchEvent (client.Username (), API_KEYWORD_GAME_START);
				}
			}
		}

		//! Command to interface which dispatches canceling game event to all controller clients.
		public int CancelGame ()
		{
			return GameInterface.cancelGame ();
		}

		//! Command to interface which dispatches end of game event to all controller clients.
		public int EndGame ()
		{
			foreach (Client client in clients) {
				client.SetStatus (5);
				client.SetReadiness (false);
			}
			SendGameEndMessageToDocent ();
			return GameInterface.endGame ();
		}

		//! Command to interface which dispatches start of game event to all controller clients.
		public int StartRound ()
		{
			return GameInterface.startRound ();
		}

		//! Command to interface which dispatches start of game event to all controller clients.
		public int CancelRound ()
		{
			return GameInterface.cancelRound ();
		}

		//! Command to interface which dispatches start of game event to all controller clients.
		public int EndRound ()
		{
			return GameInterface.endRound ();
		}

		//! Dispatch a custom Esc Event message to the specified peer
		public int DispatchEventToClient (EscEvent evt, Peer endpoint)
		{
			WriteToLog (" sending message: " + evt.ToString () + " to " + endpoint.Username ());

			if (evt.ToString ().Length > 256) {
				Debug.LogWarning ("ServerConnection : DispatchEventToClient EscEvent length exceeds limit!");
			}
			//		LoadSave.savedData.Add (Time.time.ToString () + " : OUT :" + evt.ToString () + "\n");
			return GameInterface.dispatchEvent (endpoint.Username (), evt.ToString ());
		}

		//! Starts the server connection interface. Initializes the state listener. Captures the game product name
		public void Awake ()
		{

			#if UNITY_EDITOR
			this.gameId = UnityEditor.PlayerSettings.productName;
			#else
			StringBuilder uniqueGameId = new StringBuilder(96);
			if (0 == GameInterface.getGameIdentifier(uniqueGameId, (uint) uniqueGameId.Capacity)) {
			this.gameId = uniqueGameId.ToString();
			}
			else {
			this.gameId = "";
			}
			#endif

			if (this.gameId.Equals ("")) {
				Debug.LogError ("Product name not set!");
			}

			GameInterface.startServerInterface ();

			this.stateListener = new StateListener (); 
			this.stateListener.HandleMessage += HandleStateUpdate;

			GameInterface.setHidden (); 

			//analytics = EscAnalytics.Instance;
			//analytics.Initialize(KEYWORD_GAME_ENGINE, this.gameId, "1.0", false);
			//analytics.TrackGameSessionBegin();
		}

		//! Stores the local IP address, connnectes the state listener.
		public void Start ()
		{
			//set local IP address
			this.serverIpAddress = Network.player.ipAddress;
			this.stateListener.Connect ();

		}

		/**
		 * Handles any client connection changes that occur during each frame update.
		 * If the state listener is connected, it will call its update function.
		 * While the Game Server Interface has status changes, it will attempt to reconnect until connection is successful.
		 * When TLS is connected, this class's server object will instantiate with its current IP address.
		 * While the Game Server Interface has queued presence events, each event will be marshaled based upon the client's identity.
		 * If a client is not yet recognized, it is added to the client collection and a registration message containing the 
		 * game server IP is dispatched to the controller client. If the client already exists, the presence state received is 
		 * used to determine connectivity status.
		 * 
		 * While the Game Server Interface has queued events, each event will be marshaled to the client object associated 
		 * with the sender and its message string parsed for either registration, initialization, or generic events, which 
		 * are then dispatched directly to the game class.
		 * 
		 * Finally, if any client state changes have been made they are propogated to the clients.
		 *
		 * @see Esc.GameInterface
		 * @see Esc.Server
		 * @see Esc.StateListener
		 */
		public void Update ()
		{
			// Go Full Screen after delay
			if (cumulativeTime > launchDelay && !gameFullScreen) {
				if (ServerConnection.developerMode) {
					GameInterface.goFullScreenDevMode (); 
					WriteToLog ("Set Game Full Screen Dev Mode");
				} else {
					GameInterface.goFullScreen ();
					WriteToLog ("Set Game Full Screen Production Mode");

				}
				gameFullScreen = true; 
			} else {
				cumulativeTime += Time.deltaTime;
			}

			if (this.stateListener != null) {
				if (this.stateListener.IsConnected ()) {
					this.stateListener.Update ();
				}
			}

			if (this.willReconnect) {
				this.willReconnect = false;
				GameInterface.startServerInterface ();
			}

			// process status changes
			while (GameInterface.hasMoreStatusChanges ()) {

				int stat;
				if (0 == GameInterface.getNextStatusChange (out stat)) {
					switch (stat) {
					case 0: // connected
						break;
					case 1: // TLS connected
						break;
					case 2: // disconnected
						if (null != OnDisconnected) {
							OnDisconnected ();
						}
						if (this.autoReconnect)
							willReconnect = true;


						//						if (this.gameStarted){
						//							analytics.TrackGameDisconnected(); 
						//						}
						break;
					case 3: // roster loaded;
						this.server = new Server (KEYWORD_GAME_ENGINE);
						this.server.SetRemoteEndpoint ("127.0.0.1");
						if (null != OnConnected) {
							OnConnected ();
						}
						SendGameLoadedMessageToDocent ();
						//analytics.TrackGameLoad(GetClientRegisteredCount());

						break;
					case 4: // error
						break;
					default:
						break;
					}
				}
			}

			// process presence events
			while (GameInterface.hasMorePresenceEvents ()) {
				StringBuilder user = new StringBuilder (64);
				int presence;
				if (0 == GameInterface.getNextPresenceEvent (user, user.Capacity, out presence)) {
					uint index;
					string username = user.ToString ();
					if (KEYWORD_DOCENT != username && KEYWORD_GAME_LAUNCHER != username) {
						if (this.clientUsernameMap.ContainsKey (username)) {
							this.clientUsernameMap.TryGetValue (username, out index);
							if (index >= 0 && clients.Count > 0 && index < clients.Count) {
								this.clients [(int)index].SetStatus (presence);
								if (presence == 5) {
									DisconnectClient (this.clients [(int)index]);

									//									if (this.gameStarted)
									//										analytics.TrackControllerDisconnected(); 
								} else if (presence == 0 && !this.gameStarted) {
									string registrationMessage = API_KEYWORD_REGISTERED + this.serverIpAddress + "," + index.ToString () + "," + this.gameId;
									GameInterface.dispatchEvent (username, registrationMessage);
									//	LoadSave.savedData.Add (Time.time.ToString () + " : OUT :" + username + " " + registrationMessage + "\n");
									WriteToLog (" 1 registering user: " + username);

									if (null != OnClientConnected) {
										OnClientConnected (this.clients [(int)index]);
									}
								}
							}
						} else {
							if (!this.gameStarted) {
								if (clients.Count == 0) {
									index = 0;
								} else {
									index = (uint)clients.Count;
								}

								Client newClient = new Client (username);
								this.clients.Add (newClient);

								this.clientUsernameMap.Add (username, index);
								string registrationMessage = API_KEYWORD_REGISTERED + this.serverIpAddress + "," + index.ToString () + "," + this.gameId;
								GameInterface.dispatchEvent (username, registrationMessage);
								//	LoadSave.savedData.Add (Time.time.ToString () + " : OUT :" + username + " " + registrationMessage + "\n");
								WriteToLog (" 2 registering user: " + username);

								if (null != OnClientConnected) {
									OnClientConnected (newClient);
								}
							}
						}
					}
				}
			}

			// process all incoming events
			while (GameInterface.hasMoreEvents ()) {
				StringBuilder user = new StringBuilder (64);
				StringBuilder message = new StringBuilder (256);
				if (0 == GameInterface.getNextEvent (user, (uint)user.Capacity, message, (uint)message.Capacity)) {
					uint index;
					string username = user.ToString ();
					string msgString = message.ToString ();

					// Added by kevin to null check
					if (String.IsNullOrEmpty (username) || String.IsNullOrEmpty (msgString))
						continue;

					//	LoadSave.savedData.Add (Time.time.ToString () + " : IN :" + username + " " + msgString + "\n");
					WriteToLog (" " + username + " sent a message: " + msgString);

					if (username.Equals (KEYWORD_GAME_LAUNCHER)) {

						EscEvent evt = EscEvent.FromString (msgString);
						string requestedGameID = evt.attributes ["game"];

						if (msgString.StartsWith (API_KEYWORD_START)) {
							if (requestedGameID.Equals (this.gameId)) {
								this.gameStarted = true;
								this.gameStartRound = Convert.ToInt32 (evt.attributes ["round"]);
								this.gameDifficulty = Convert.ToInt32 (evt.attributes ["difficulty"]);
								StartGame ();
								OnGameStart (requestedGameID);

								// track how many total controllers are registered 
								//analytics.TrackGameLoad(GetClientRegisteredCount());

								// track the round started 
								//analytics.TrackRoundStart(this.gameStartRound.ToString());

								// track how many total controllers are ready to play the game 
								//analytics.TrackGameStart(GetClientReadyCount ()); 
							}
						} else if (msgString.StartsWith (API_KEYWORD_PAUSE)) {
							if (requestedGameID.Equals (this.gameId)) {
								this.gamePaused = !this.gamePaused;
								PauseControllers ();
								OnGamePause (requestedGameID);

								// track game pause
								//analytics.TrackGamePause(GetClientReadyCount ());
							}
						} else if (msgString.StartsWith (API_KEYWORD_STOP)) {
							if (requestedGameID.Equals (this.gameId)) {
								this.gameStarted = false;
								OnGameStop (requestedGameID);
								WriteToLog (msgString);

								// track the round ended 
								//analytics.TrackRoundEnd(this.gameStartRound.ToString());
							}
						} else if (msgString.StartsWith (API_KEYWORD_QUIT)) {
							if (requestedGameID.Equals (this.gameId)) {
								this.gameStarted = false;
								OnGameEnd (requestedGameID);
								Application.Quit ();
								WriteToLog (msgString);

								// track game end
								//analytics.TrackGameEnd(GetClientReadyCount ()); 
							}
						}
					} else if (!username.Equals (KEYWORD_DOCENT)) {
						if (this.clientUsernameMap.ContainsKey (username)) {
							this.clientUsernameMap.TryGetValue (username, out index);
							if (index >= clients.Count) {

								WriteToLog (" " + username + " has an invalid index: " + index.ToString ());

								continue;
							}
							if (msgString.StartsWith (API_KEYWORD_REGISTERED)) {
								string ipAddress = msgString.Substring (API_KEYWORD_REGISTERED.Length);
								this.clients [(int)index].Register (ipAddress);
								if (null != OnClientRegistered) {
									OnClientRegistered (this.clients [(int)index]);
								}
							} else if (msgString.StartsWith (API_KEYWORD_INIT)) {
								this.clients [(int)index].SetReadiness (true);
								if (null != OnClientInitialized) {
									OnClientInitialized (this.clients [(int)index], EscEvent.FromString (msgString));
								}
							} else {
								EscEvent evt = EscEvent.FromString (msgString);
								this.clients [(int)index].AppendEvent (evt);
							}
						}
					} else if (username.Equals (KEYWORD_DOCENT)) {
						if (msgString.StartsWith (API_KEYWORD_APPLY_PARAMS)) {
							// determine which command it is and parse it...
							EscEvent evt = EscEvent.FromString (msgString);
							int modeParam1 = 0;
							int modeParam2 = 0;
							int modeParam3 = 0;

							try {
								// WARNING: This won't work if mode3 was sent but mode 2 was not
								modeParam1 = Int32.Parse (evt.attributes ["mode1"]);
								modeParam2 = Int32.Parse (evt.attributes ["mode2"]);
								modeParam3 = Int32.Parse (evt.attributes ["mode3"]);
							} catch {
								WriteToLog (" error parsing docent parameters ");
							}

							// store parameters locally...
							this.gameStringParameter1 = evt.attributes ["param1"];
							this.gameStringParameter2 = evt.attributes ["param2"];
							this.gameStringParameter3 = evt.attributes ["param3"];
							this.gameModeParameter1 = modeParam1;
							this.gameModeParameter2 = modeParam2;
							this.gameModeParameter3 = modeParam3;

							// notify listeners that a change was applied
							if (null != OnParametersUpdated) {
								OnParametersUpdated ();
							}
						} else if (msgString.StartsWith (KEYWORD_NEXT)) {
							// notify listeners that the next button was pressed
							if (null != OnNextScreen) {
								OnNextScreen ();
							}
						} else if (msgString.StartsWith (KEYWORD_CHECK_IF_LOADED)) {

							string options = API_KEYWORD_GAME_PREVIOUSLY_LOADED + "gameId=" + this.gameId + ",gameStarted=" + this.gameStarted;
							GameInterface.dispatchEvent (username, options);
							//							LoadSave.savedData.Add (Time.time.ToString () + " : OUT :" + username + " " + options + "\n");
						}

					}
				}
			}

			if (null != this.server && this.server.StateVarsAreStale ()) {
				this.server.PropagateStateVars ();
			}
		}

		//				// Added by Kevin!!
		//				void OnApplicationQuit ()
		//				{
		//						
		//				//		LoadSave.Save ("messagingCount");
		//				}

		/** 
		 * Callback handler from the StateListener that maps the incoming
		 * key/value pairs to the server key/value pairs.
		 * 
		 * @param keys the string array of keys from the StateListener
		 * @param args the corresponding values for the keys
		 */
		private void HandleStateUpdate (string[] keys, ArrayList args)
		{
			int index = (int)args [0];

			if (0 < args.Count && index < this.clients.Count) {
				args.RemoveAt (0);
			}

			if (this.clients.Count > index) {
				this.clients [index].UpdateStateVars (keys, args);
			}
		}

		/**
		 * Dispatches a EscEvent to a controller client with initial payload attributes provided by the Game class.
		 * 
		 * @param client the client to be initialized
		 * @param initPayload the initialization key/value pairs to be dispatched
		 */
		public void InitializeController (Client client, Dictionary<string, string> initPayload)
		{
			string msgInit = API_KEYWORD_INIT.Substring (0, API_KEYWORD_INIT.Length - 1);
			EscEvent evt = new EscEvent (msgInit, initPayload);
			string initPayloadString = evt.ToString ();

			WriteToLog (" sending message: " + initPayloadString + " to " + client.Username ());
			//						LoadSave.savedData.Add (Time.time.ToString () + " : OUT :" + client.Username () + " " + initPayloadString + "\n");
			GameInterface.dispatchEvent (client.Username (), initPayloadString);
		}

		/**
		 * Dispatches an EscEvent to a controller client without any special attributes.
		 * 
		 * @param client the client to be initialized
		 */
		public void InitializeController (Client client)
		{
			GameInterface.dispatchEvent (client.Username (), API_KEYWORD_INIT);
			//				LoadSave.savedData.Add (Time.time.ToString () + " : OUT :" + client.Username () + " " + API_KEYWORD_INIT + "\n");
		}

		//! Dispatches a EscEvent to a controller client to pause.
		private void PauseControllers ()
		{
			foreach (Client client in this.clients) {
				GameInterface.dispatchEvent (client.Username (), API_KEYWORD_GAME_PAUSED);
				//LoadSave.savedData.Add (Time.time.ToString () + " : OUT :" + client.Username () + " " + API_KEYWORD_GAME_PAUSED + "\n");
			}
		}

		//! Invoked when a client is no longer present. Thus removing them from the locally stored map of clients.
		private void DisconnectClient (Client client)
		{
			client.SetReadiness (false);
			client.UnRegister ();
			if (null != OnClientDisconnected) {
				OnClientDisconnected (client);
			}
		}

		//! Return the number of players that are ready to play the game
		private int GetClientReadyCount ()
		{
			int counter = 0;
			foreach (Client client in this.clients) {
				if (client.IsReady ()) {
					counter++; 
				}
			}
			return counter; 
		}

		//! Return the status of total players registered
		private int GetClientRegisteredCount ()
		{
			int counter = 0;
			foreach (Client client in this.clients) {
				if (client.IsRegistered ()) {
					counter++; 
				}
			}
			return counter; 
		}

		//! Clean up on removal of this class.
		public void OnDestroy ()
		{
			//if (null != analytics) analytics.TrackGameSessionEnded();
			this.willReconnect = false;
			this.autoReconnect = false;
			this.clients.Clear ();
			this.clientUsernameMap.Clear ();
			GameInterface.stopInterface ();
		}

		//! Exit full screen mode.
		public void ExitFullScreen ()
		{
			GameInterface.leaveFullScreen (); 
		}

		/**
		 * Send Game End message to the Docent. This allows it to update it's view 
		 * to show the current representation of the game. It's ignored if the docent isn't available.
		 */
		private void SendGameEndMessageToDocent ()
		{
			if (0 != GameInterface.dispatchEvent (KEYWORD_DOCENT, "gameEnd")) {
				Debug.LogWarning ("Game end message delivery failed!");
			}
		}

		/**
		 * Send Game Loaded message to the Docent. This allows it to update it's view 
		 * to show the current representation of the game. It's ignored if the docent isn't available.
		 */
		private void SendGameLoadedMessageToDocent ()
		{
			if (0 != GameInterface.dispatchEvent (KEYWORD_DOCENT, "gameLoaded")) {
				Debug.LogWarning ("Game loaded message delivery failed!");
			}
		}

		public void WriteToLog (string incString)
		{
			#if QUIET
			return;
			#else
			GameInterface.writeLog (incString);
			#endif
		}
	}

}