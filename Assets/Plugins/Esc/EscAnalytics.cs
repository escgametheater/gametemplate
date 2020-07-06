using UnityEngine;
using System;
using System.Text;
using System.Collections;
using Esc; 

namespace Esc {
	
	/**
	 * EscAnalytics is a singleton that is the primary interface for tracking gameplay 
	 * and player activity utilizing Google Analytics. EscAnalytics allows the game designer 
	 * to track different game and player states as needed. Game and round starting and endings
	 * are tracked automatically. Google Analytics Sessions represent a single gameplay experience.
	 * 
	 * NOTE: The EscAnalytics singleton is meant for use with the Game Server machine only.
	 */
	public class EscAnalytics : MonoBehaviour {

		//! EscAnalytics Game Object
		private const string GAMEOBJECTNAME = "EscAnalyticsGameObject";

		//! Unknown Category Location
		private const string UNKNOWN_LOCATION = "Unknown";

		// Event Actions
		private const string GAME_LOAD = "Game Load";
		private const string GAME_START = "Game Start";
		private const string GAME_PAUSE = "Game Pause";
		private const string GAME_END = "Game End";
		private const string GAME_DISCONNECTED = "Game Disconnected";
		private const string CONTROLLER_DISCONNECTED = "Controller Disconnected";
		private const string ROUND_START = "Round Start";
		private const string ROUND_END = "Round End";

		// Event Details
		private const string TOTAL_PLAYERS = "Total Players: ";
		private const string ROUND = "Round: ";
		private const string TIME_PLAYED = "Time Played: ";
		private const string POINTS_AWARDED = "Points Awarded: ";
		private const string POINTS_SCORED = "Points Scored: ";
		private const string WINNING_PLAYER = "Winning Player: ";

		//! The Google Analytics account tracking ID 
		private string trackingID;

		//! The Google Analytics client ID 
		private string clientID = "";

		//! The Google Analytics App Name parameter
		private string appName = "EscPlatform";

		//! The Google Analytics App Version 
		private string appVersion = "1.0";

		//! The Google Analytics App Event Prefix 
		private string appPrefix = "EscPlatform-";

		//! The Google Analytics Event Label used for analytics
		private string analyticsLocationLabel = UNKNOWN_LOCATION;

		//! Use HTTPS option flag
		private bool useHTTPS = false;

		//! Flag indicates whether or not the class was initialized already
		private bool isInitialized = false;

		//! Flag to disable the tracking when using the Unity3D editor
		private bool isEnabled = true;

		//! The staticly allocated Google Analytics Instance 
		private static GoogleUniversalAnalytics gua = null;

	    //! Create a singleton instance of the EscAnalytics class
	    private static EscAnalytics _instance;
	    public static EscAnalytics Instance { 
			get {
				if (!_instance) {
					_instance = GameObject.FindObjectOfType(typeof(EscAnalytics)) as EscAnalytics;
					if (!_instance) {
						GameObject container = new GameObject();
						container.name = GAMEOBJECTNAME;
						_instance = container.AddComponent(typeof(EscAnalytics)) as EscAnalytics;

					}
				}
			
				return _instance;
			}
	    }

	    //! Constructor
		public EscAnalytics ()
		{
#if UNITY_EDITOR
			GameInterface.writeLog("EscAnalytics - using UNITY_EDITOR");
			this.isEnabled = false;
#else
			GameInterface.writeLog("EscAnalytics - not using UNITY_EDITOR");
#endif

		}

		//! Queries the host machine's current location, initializes instance.
		void Awake ()
		{
			QueryLocation();
			Application.RegisterLogCallback(HandleLogEntry);
		}
		
		//! Destroys GA structures
		void OnDestroy ()
		{
			if (null != EscAnalytics.gua) EscAnalytics.gua.destroy();
		}
		
		//! Enables or Disables the tracking depending upon the value of the input parameter
		public void EnableTracking (bool value = true) 
		{
			this.isEnabled = value;
		}
	
		/**
		 * Attempts to store a label for the location by loading and parsing a specific file from the disk.
		 * The file loaded is: ~/location
		 */
		private void QueryLocation () 
		{
			string appPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			appPath = appPath.Replace("/Documents", "");
			string locationPath = appPath + "/location";
		
			StartCoroutine(LoadLocationFile(locationPath));
		}

		/**
		 * Takes any string input and sanitizes it against a list of acceptable characters
		 * 
		 * @param input any string input that might be used as a tracking label
		 * @param maxLength the maximum number of characters to be read from the input file
		 * @return new, sanitized version of the input string
		 */
		private string SanitizeLabel(string input, int maxLength = 96)
		{
			string admitted = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!-_+.()[]";
			if (input.Length > maxLength)
				input = input.Substring(0, maxLength);
		
			StringBuilder output = new StringBuilder(input.Length);
			bool found = false;

			// we check each character against the list of admitted characters...
			foreach (char c in input) {
				found = false;
				foreach (char adm in admitted) {
					if (c == adm) {
						found = true;
						output.Append(c);
					}
				}
			
				// if character is considered "illegal", we replace it with a dash
				if (found == false && '-' != output[output.Length-1])
					output.Append("-");
			}

			return output.ToString();
		}

		/**
		 * Coroutine method used for parsing a string from the raw contents of a file located on the disk at the path given as input.
		 * Assigns string value to analyticsLocationLabel property. If nothing is parsed successfully the string "Unknown" is assigned.
		 * 
		 * @param locationPath directory path to a file that represents the location of the desired string data
		 */
		private IEnumerator LoadLocationFile (string locationPath) 
		{
			WWW locationFile = new WWW("file://" + locationPath);
			yield return locationFile;

			// if location file was not found or was not loaded we exit early
			if (!String.IsNullOrEmpty(locationFile.error)) {
				this.analyticsLocationLabel = UNKNOWN_LOCATION;
				yield break;
			}

			// only use the first line of the file that was loaded
			string rawLocationText = locationFile.text;
			string[] newLineDelimiter = new string[] {"\n"};
			string[] lineDelimitedFile = rawLocationText.Split(newLineDelimiter, StringSplitOptions.None);
			rawLocationText = lineDelimitedFile[0].Trim();
		
			// handle degenerate case or "unsanitized" input
			if ("" == rawLocationText) {
				this.analyticsLocationLabel = UNKNOWN_LOCATION;
			}
			else {
				this.analyticsLocationLabel = SanitizeLabel(rawLocationText);
			}
		}

		/**
		 * Initialize the Esc Analytics with your Google Analytics data.
		 * 
		 * @param escClientID the GA client ID parameter
		 * @param escAppName the GA App Name parameter
		 * @param escAppVersion the GA App Version parameter
		 * @param escUseHTTPS optionally use HTTPS for logging
		 */
		internal void Initialize ( string escClientID, string escAppName, string escAppVersion, bool escUseHTTPS )
		{
			if (!this.isEnabled) return;
			
			if (gua == null) {
				gua = GoogleUniversalAnalytics.Instance;
			}
			
			StringBuilder gaIdentifier = new StringBuilder(48);
			GameInterface.getGoogleAnalyticsIdentifier( gaIdentifier, (uint) gaIdentifier.Capacity );
			
			this.trackingID = gaIdentifier.ToString(); 
			this.clientID = escClientID;
			this.appName = escAppName; 
			this.appVersion = escAppVersion; 
			this.useHTTPS = escUseHTTPS;
			
			this.appPrefix = this.appName + ": ";
			
			gua.initialize( this.trackingID, this.clientID, this.appName, this.appVersion, this.useHTTPS );
			
			isInitialized = true;
		}

		//! Track the beginning of a Game Session 
		public void TrackGameSessionBegin ()
		{
			if (this.isEnabled) gua.addSessionControl(true);
		}

		//! Track the end of a Game Session 
		public void TrackGameSessionEnded ()
		{
			if (this.isEnabled) gua.addSessionControl(false);
		}

		//! Track Game Load 
		public void TrackGameLoad (int totalPlayers)
		{
			if (this.isEnabled) gua.sendEventHit(this.analyticsLocationLabel,this.appPrefix + GAME_LOAD, TOTAL_PLAYERS + totalPlayers.ToString());
		}

		//! Track Game Start 
		public void TrackGameStart (int totalPlayers)
		{
			if (this.isEnabled) gua.sendEventHit(this.analyticsLocationLabel,this.appPrefix + GAME_START, TOTAL_PLAYERS + totalPlayers.ToString());
		}

		//! Track Game Pause
		public void TrackGamePause (int totalPlayers)
		{
			if (this.isEnabled) gua.sendEventHit(this.analyticsLocationLabel, this.appPrefix + GAME_PAUSE, TOTAL_PLAYERS + totalPlayers.ToString());
		}

		//! Track Game Disconnected 
		public void TrackGameDisconnected ()
		{
			if (this.isEnabled) gua.sendEventHit(this.analyticsLocationLabel, this.appPrefix + GAME_DISCONNECTED);
		}

		//! Track Game End 
		public void TrackGameEnd (int totalPlayers)
		{
			if (this.isEnabled) gua.sendEventHit(this.analyticsLocationLabel, this.appPrefix + GAME_END, TOTAL_PLAYERS + totalPlayers.ToString());
		}

		//! Track Round Start  
		public void TrackRoundStart (string round)
		{
			if (this.isEnabled) gua.sendEventHit(this.analyticsLocationLabel,this.appPrefix + ROUND_START, ROUND + round);
		}

		//! Track Game End 
		public void TrackRoundEnd (string round)
		{
			if (this.isEnabled) gua.sendEventHit(this.analyticsLocationLabel,this.appPrefix + ROUND_END, ROUND + round);
		}

		//! Track Game Duration 
		public void TrackGameDuration (string timePlayed)
		{
			if (this.isEnabled) gua.sendEventHit(this.analyticsLocationLabel,this.appPrefix + TIME_PLAYED, timePlayed);
		}

		//! Track Controller Disconnected 
		public void TrackControllerDisconnected ()
		{
			if (this.isEnabled) gua.sendEventHit(this.analyticsLocationLabel, this.appPrefix + CONTROLLER_DISCONNECTED);
		}

		//! Track Custom Key Value 
		public void TrackCustomKeyValue (string key, string customValue)
		{
			if (this.isEnabled) gua.sendEventHit(this.analyticsLocationLabel,this.appPrefix + key, customValue);
		}

		//! Track Points Awarded 
		public void TrackPointsAwarded (string points)
		{
			if (this.isEnabled) gua.sendEventHit(this.analyticsLocationLabel,this.appPrefix + POINTS_AWARDED, points);
		}

		//! Track Player Score
		public void TrackPlayerScore (string points)
		{
			if (this.isEnabled) gua.sendEventHit(this.analyticsLocationLabel,this.appPrefix + POINTS_SCORED, points);
		}

		//! Track Winning Player 
		public void TrackPlayerWinner (string player)
		{
			if (this.isEnabled) gua.sendEventHit(this.analyticsLocationLabel,this.appPrefix + WINNING_PLAYER, player);
		}

		//! Track Game Exception 
		public void TrackGameException (string description, bool isFatal)
		{
			if (this.isEnabled) gua.sendExceptionHit(this.appPrefix + description, isFatal);
		}

		//! Catch exceptions on mobile device 
		void HandleLogEntry(string logEntry, string stackTrace, LogType logType)
		{
			if (!this.isEnabled) return;
			
			switch (logType)
			{
				case LogType.Exception:
				TrackGameException(logEntry, true);
				//Debug.LogError("Caught an exception being thrown: " + logEntry);
				break;
			}
		}
	}
}
