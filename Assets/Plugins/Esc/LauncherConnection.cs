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

namespace Esc {

	/**
	 * The LauncherConnection is a subclass of the ServerConnection singleton.
	 * It is the interface between the ESC Launcher application and the 
	 * iOS Docent application.
	 * 
	 * The LauncherConnection loads 
	 * 
	 * @see tools/games_plist_sync.py
	 * @see Esc.ServerConnection
	 */
	public class LauncherConnection : ServerConnection {
		
		//! Constant string value appended to the game ID to construct a valid URL
		private const string APP_URL_SUFFIX = "://";

		//! Callback associated with the client becoming connected
		public new event EventCallbackAction OnConnected;

		//! Callback associated with the client becoming disconnected
		public new event EventCallbackAction OnDisconnected;

		//! Docent event Callback type
		public delegate void DocentCallbackAction(string gameId = "");

		//! Callback associated with docent initialization
		public event DocentCallbackAction OnDocentInit;

		//! Callback associated with the docent loading the game
		public event DocentCallbackAction OnDocentGameLoad;

		//! Callback associated with the docent starting the game
		public event DocentCallbackAction OnDocentGameStart;

		//! Callback associated with the docent stopping the game
		public event DocentCallbackAction OnDocentGameStop;

		//! Callback associated with the docent pausing the game
		public event DocentCallbackAction OnDocentGamePause;

		//! Callback associated with the docent stopping the game
		public event DocentCallbackAction OnDocentGameEnd;

		//! Callback associated with the docent applying advanced settings 
		public event DocentCallbackAction OnDocentApplyAdvancedSettings;

		//! API constant event name used for docent events
		protected const string API_KEYWORD_GAME = "command:";

		//! Used to determine how often the games plist should be read
		private const float CHECK_GAMES_LIST_INTERVAL = 1.0f;

		//! Medialon OSC dispatcher
		private OscTransmitter oscTransmitter;
		//! Medialon heartbeat message address
		private const string MEDIALON_MESSAGE_ADDRESS = "/esc/heartbeat/";
		//! Used to determine how often a heartbeat message should be sent to medialon
		private const float MEDIALON_PING_INTERVAL = 5.0f;
		//! The IPv4 address of the Medialon endpoint for sending heartbeat messages
		private const string MEDIALON_ENDPOINT_IP = "192.168.10.51";
		//! The IPv4 port number for the Medialon endpoint for sending heartbeat messages
		private const int MEDIALON_ENDPOINT_PORT = 33220;

		//! Timer that accumulates every frame for the Medialon heartbeat
		private float medialonTime = 0.0f;

		//! Used to determine how often the games plist should be read
		public float refreshImageTimer = 10.0f;

		/**
		 * Collection of game titles and their respective IDs parsed
		 * from the ESC Launcher application's Info.plist
		 */
		public Dictionary<string, string> gamesList;

		//! Stores the game id of a currently loaded game
		private string currentLoadedGame = "";

		//! Provides a Singleton Instance of this class.
		private static LauncherConnection _instance;
		public new static LauncherConnection Instance
		{
			get {
				if (!_instance) {
					_instance = GameObject.FindObjectOfType(typeof(LauncherConnection)) as LauncherConnection;
					if (!_instance) {
						GameObject container = new GameObject();
						container.name = "LauncherGameObject";
						_instance = container.AddComponent(typeof(LauncherConnection)) as LauncherConnection;
					}
				}
				
				return _instance;
			}
		}
		
		//! Class constructor.
		public LauncherConnection () 
		{
		}

		//! Class destructor
		~LauncherConnection () 
		{
		}

		/**
		 * Overwrites the Awake method in ServerConnection.
		 * Starts the connection interface and load/parse Info.plist
		 *
		 * @see Esc.GameInterface
		 */
		public new void Awake ()
		{
			gamesList = new Dictionary<string, string>();

			GameInterface.startLauncherInterface();
			GameInterface.setHidden();
			
			this.oscTransmitter = new OscTransmitter(MEDIALON_ENDPOINT_IP, MEDIALON_ENDPOINT_PORT);
		}

		/**
		 * Overwrites the Start method in ServerConnection.
		 */
		public new void Start ()
		{
			GameInterface.goFullScreen();
		}
		
		/**
		 * Handles docent connection messages that occur during each frame update.
		 *
		 * @see Esc.ServerConnection
		 */
		public new void Update () 
		{
			// Read games plist every once second 
			if ( GameInterface.isConnected() ) {
				if (cumulativeTime > CHECK_GAMES_LIST_INTERVAL){
					cumulativeTime = 0.0f;
					ReadGamesPlist();
				}
				else {
					cumulativeTime += Time.deltaTime;
				}
			}
			
			// Dispatch heartbeat ping to Medialon system if necessary
			if (medialonTime > MEDIALON_PING_INTERVAL){
				medialonTime = 0.0f;
				DispatchMedialonPing();
			}
			else {
				medialonTime += Time.deltaTime;
			}

			// Reconnect if necessary
			if ( this.willReconnect ) {
				this.willReconnect = false;
				GameInterface.startLauncherInterface();
			}

			while (GameInterface.hasMoreStatusChanges()) {
				int stat;
				if (0 == GameInterface.getNextStatusChange( out stat )) {
					switch (stat) {
					case 0: // connected
						//GameInterface.writeLog("socket connection established");
						break;
					case 1: // TLS connected
						break;
					case 2: // disconnected
						if (null != OnDisconnected) {
							OnDisconnected();
						}
						if ( this.autoReconnect ) willReconnect = true;
						break;
					case 3: // roster loaded;
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

			while (GameInterface.hasMoreEvents()) 
			{
				StringBuilder user = new StringBuilder(64);
				StringBuilder message = new StringBuilder(256);
								if(user == null || user.Capacity == null || message == null || message.Capacity ==  null)
										return;
				if (0 == GameInterface.getNextEvent( user, (uint) user.Capacity, message, (uint) message.Capacity )) {
					string username = user.ToString();
					string msgString = message.ToString();

					if ( username.Equals("docent") ) {
						if (msgString.StartsWith( API_KEYWORD_GAME )) {
							EscEvent evt = EscEvent.FromString( msgString );
							//Debug.Log("message: " + msgString);
							string gameIdValue = "";
							string option = "";
							int round = -1;
							int difficulty = -1;

							//! advance settings strings
							int mode1 = -1;
							int mode2 = -1;
							int mode3 = -1;
							string param1 = "";
							string param2 = "";
							string param3 = "";

							foreach (KeyValuePair<string, string> pair in evt.attributes) {
								switch ( pair.Key ) {
									case "game":
										gameIdValue = pair.Value;
										break;
									case "option":
										option = pair.Value;
										break;
									case "round":
										round = Convert.ToInt32(pair.Value);
										break;
									case "difficulty":
										difficulty = Convert.ToInt32(pair.Value);
										break;
									case "mode1":
										mode1 = Convert.ToInt32(pair.Value);
										break;
									case "mode2":
										mode2 = Convert.ToInt32(pair.Value);
										break;
									case "mode3":
										mode3 = Convert.ToInt32(pair.Value);
										break;
									case "param1":
										param1 = pair.Value;
										break;
									case "param2":
										param2 = pair.Value;
										break;
									case "param3":
										param3 = pair.Value;
										break;
								}
							}
							
							switch (option) {
								case "load":
									if (!this.currentLoadedGame.Equals("") ){
										DocentQuitGame(this.currentLoadedGame);
									}
									this.currentLoadedGame = gameIdValue; 
									DocentLoadGame(gameIdValue);
									break;
								case "start":
									DocentStartGame(gameIdValue, round, difficulty);
									break;
								case "pause":
									DocentPauseGame(gameIdValue);
									break;
								case "stop":
									DocentStopGame(gameIdValue);
									break;
								case "apply":
									DocentApplyAdvancedSettings(gameIdValue, mode1, mode2, mode3, param1, param2, param3);
									break;
								case "quit":
									DocentQuitGame(gameIdValue);
									break;
							}
						}
						else if (msgString.StartsWith( API_KEYWORD_INIT)) {
							UpdateDocentGamesList(); 
						}
					}
				}
			}
		}

		/**
		 * Dispatches a simple OSC message to the endpoint defined as:
		 * MEDIALON_ENDPOINT_IP : MEDIALON_ENDPOINT_PORT using the address:
		 * MEDIALON_MESSAGE_ADDRESS with no additional numbers attached to
		 * the packet.
		 */
		private void DispatchMedialonPing () 
		{
			if (this.oscTransmitter.IsConnected()) {
				OscMessage message = new OscMessage("");
				message.Address = MEDIALON_MESSAGE_ADDRESS;
				this.oscTransmitter.Send(message);
			}
			else {
				Debug.LogWarning("DispatchMedialonPing // transmitter not connected!");
			}
		}

		/**
		 * Loads the ESC Platform Launcher's .plist via the plugin and
		 * parses it as XML. The resulting Dictionary is then passed to
		 * the Docent application.
		 */
		public void ReadGamesPlist ()
		{
			Dictionary<string, string> newGamesList = new Dictionary<string, string>();

			string appPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			appPath = appPath.Replace("/Documents", "");

			string plistPath = appPath + "/esclauncher/Info.plist";

			StringBuilder contents = new StringBuilder(16384);

			if (0 == GameInterface.getLauncherInfoPlist( plistPath, contents, (uint) contents.Capacity ))
			{
				// load external content
				string contentsString = contents.ToString();
				XmlDocument xmlDoc = new XmlDocument();
				try {
					xmlDoc.LoadXml(contentsString);
				}
				catch (XmlException e) {
					Debug.LogWarning("Failed to load Info.plist: " + e.Message);
					return;
				}
				
				// parse loaded XML
				XmlNodeList dictList = xmlDoc.GetElementsByTagName("dict");
				XmlNode xmlNode = dictList.Item(1);

				for (int i = 0; i < xmlNode.ChildNodes.Count; i += 2) {
					XmlNode keyNode = xmlNode.ChildNodes.Item(i);
					XmlNode valNode = xmlNode.ChildNodes.Item(i + 1);

					newGamesList.Add(keyNode.InnerText, valNode.InnerText);
				}

				// Compare new plist to existing plist for equality and send a new message to docent if they are not equal
				bool gameListsAreEqual = (gamesList.Count == newGamesList.Count);
				if (gameListsAreEqual) {
				    foreach (var pair in gamesList) // If count is same, compare the key / values
				    {
						string value;
						if (newGamesList.TryGetValue(pair.Key, out value)) {	// Requires value to be equal
						    if (value != pair.Value) {
								gameListsAreEqual = false;
								break;
						    }
						}
						else {	// Requires key to be present
						    gameListsAreEqual = false;
							break;
						}
				    }
				}
				
				if (!gameListsAreEqual) {
					gamesList = newGamesList;
					UpdateDocentGamesList();
				}
			}
			else {
				Debug.LogWarning("Could not load Info.plist");
			}
		}

		/**
		 * Dispatches an EscEvent to the docent client with initial payload attributes 
		 * provided by the Game class.
		 */
		public void InitializeDocent () 
		{
			UpdateDocentGamesList(); 
			
			if (null != OnDocentInit) {
				OnDocentInit();
			}
		}
		
		/**
		 * Dispatches the currently stored list of games from the locally generated list
		 */
		public void UpdateDocentGamesList () 
		{
			EscEvent evt = new EscEvent("games", gamesList);
			string initPayloadString = evt.ToString();
			GameInterface.dispatchEvent("docent", initPayloadString);
		}

		//! Loads the game represented by the input game id
		public void DocentLoadGame (string gameId)
		{
			GameInterface.writeLog("DocentLoadGame: for id = " + gameId);
			StartCoroutine(LoadGame(gameId));
		}

		/**
		 * Dispatches an EscEvent to the game engine to start the game
		 *
		 * @param gameId the unique game name identifier
		 * @param round the round setting to dispatch
		 * @param difficulty the difficulty setting to dispatch
		 */
		public void DocentStartGame (string gameId, int round, int difficulty)
		{
			Dictionary<string, string> dict = new Dictionary<string, string>();
			dict.Add("game", gameId);
			if (-1 != round) {
				dict.Add("round", round.ToString());
			}
			if (-1 != difficulty) {
				dict.Add("difficulty", difficulty.ToString());
			}
			EscEvent evt = new EscEvent("start", dict);
			GameInterface.dispatchEvent("game-engine", evt.ToString());
			
			if (null != OnDocentGameStart) {
				OnDocentGameStart(gameId);
			}
		}

		//! Dispatches an EscEvent to the game engine to toggle pause
		public void DocentPauseGame (string gameId)
		{
			Dictionary<string, string> dict = new Dictionary<string, string>();
			dict.Add("game", gameId);
			EscEvent evt = new EscEvent("pause", dict);
			GameInterface.dispatchEvent("game-engine", evt.ToString());
			
			if (null != OnDocentGamePause) {
				OnDocentGamePause(gameId);
			}
		}


		//! Dispatches an EscEvent to the game engine to stop game
		public void DocentStopGame (string gameId)
		{
			Dictionary<string, string> dict = new Dictionary<string, string>();
			dict.Add("game", gameId);
			EscEvent evt = new EscEvent("stop", dict);
			GameInterface.dispatchEvent("game-engine", evt.ToString());
			GameInterface.writeLog("stop game");
			if (null != OnDocentGameStop) {
				OnDocentGameStop(gameId);
			}
		}

		/** 
		 * Dispatches a list of parameters to the docent remote control to initialize it
		 *
		 * @param gameId the unique game name identifier
		 * @param mode1 the first mode toggle integer
		 * @param mode2 the second mode toggle integer
		 * @param mode3 the third mode toggle integer
		 * @param param1 the first custom string paramter
		 * @param param2 the second custom string paramter
		 * @param param3 the third custom string paramter
		 * 
		 * !DEPRECATED!
		 */
		public void DocentApplyAdvancedSettings(string gameId, int mode1, int mode2, int mode3, string param1, string param2, string param3)
		{
			Dictionary<string, string> dict = new Dictionary<string, string>();
			dict.Add("game", gameId);
			if (-1 != mode1) {
				dict.Add("mode1", mode1.ToString());
			}

			if (-1 != mode2) {
				dict.Add("mode2", mode2.ToString());
			}

			if (-1 != mode3) {
				dict.Add("mode3", mode3.ToString());
			}

			if ("" != param1) {
				dict.Add("param1", param1.ToString());
			}

			if ("" != param1) {
				dict.Add("param2", param1.ToString());
			}

			if ("" != param1) {
				dict.Add("param3", param1.ToString());
			}

			EscEvent evt = new EscEvent("start", dict);
			GameInterface.dispatchEvent("game-engine", evt.ToString());

			if (null != OnDocentApplyAdvancedSettings) {
				OnDocentApplyAdvancedSettings(gameId);
			}
		}

		/**
		 * Invoked by the ESC Launcher application when a Quit action is triggered or when a new game is loaded.
		 * Dispatches an event to the plugin which then propagates it to all connected client controllers.
		 */
		public void DocentQuitGame (string gameId)
		{
			Dictionary<string, string> dict = new Dictionary<string, string>();
			dict.Add("game", gameId);
			EscEvent evt = new EscEvent("quit", dict);
			GameInterface.dispatchEvent("game-engine", evt.ToString());
			
			if (null != OnDocentGameEnd) {
				OnDocentGameEnd(gameId);
			}
		}
		
		//! Launches coroutine to load a new game denoted by the given game ID paramter
		private IEnumerator LoadGame (string gameId)
		{
			//Debug.Log("Launch Game: " + gameId);
			yield return new WaitForSeconds(0.5f);
			//Application.OpenURL(gameId + APP_URL_SUFFIX);
			GameInterface.launchGameForUrl(gameId + APP_URL_SUFFIX);

			if (null != OnDocentGameLoad) {
				OnDocentGameLoad(gameId);
			}
			
			// StopAllCoroutines();
			StopCoroutine("LoadGame");
		}
	}

}