using UnityEngine;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.IO;
using System.Xml;
using Osc;
using Esc;

namespace Esc {

	//! Total teams in play 
	public enum Team {
		ONE_TEAM = 1,
		TWO_TEAMS,
		THREE_TEAMS,
		FOUR_TEAMS,
		FIVE_TEAMS,
		SIX_TEAMS
	}

	//! Available lighting colors 
	public enum LightingColor {
		RED = 1,
		ORANGE = 2,
		AMBER = 3,
		YELLOW = 4,
		GREEN = 5,
		CYAN = 6,
		BLUE = 7,
		LIGHT_BLUE = 8,
		MAGENTA = 9,
		LAVENDER = 10,
		PINK = 11,
		WARM_WHITE = 12,
		NEUTRAL_WHITE = 13,
		COOL_WHITE = 14,
		RED_AMBER_YELLOW = 51,
		BLUE_CYAN_GREEN = 52,
		RED_BLUE = 53,
		AMBER_BLUE = 54,
		RED_WHITE_BLUE = 55,
		RED_PINK_PURPLE = 56,
		BLACK = 98
	}

	//! Available light groupings
	public enum Grouping {
		ALL = 101, 
		FRONT,
		BACK
	}

	//! Available intensity levels 
	public enum Level {
		OFF = 201, 
		LOW = 202,
		MEDIUM = 205, 
		HIGH = 210
	}

	//! Available speed 
	public enum Speed {
		OFF = 10,
		LOW = 12,
		MEDIUM = 15, 
		HIGH = 19
	}

	//! Available duration 
	public enum Duration {
		SHORT = 5,
		MEDIUM = 10,
		LONG = 30
	}

	//! Available movment pattern 
	public enum Pattern {
		LEFTRIGHT = 21, 
		RIGHTLEFT,
		CLOCKWISE,
		COUNTERCLOCKWISE,
		RANDOM1,
		RANDOM2,
		ONE_BURST,
		PAPARAZZI
	}
	
	// Available pulse patterns
	public enum Pulse {
		ALL_FULL = 401,
		ALL_50_PERCENT,
		ALL_25_PERCENT,
		LEFT_RIGHT,
		RIGHT_LEFT,
		CENTER_OUT
	}

	//! Available timings 
	public enum Timing {
		ZERO = 0,
		ONE = 1,
		THREE = 3,
		FIVE = 5,
		TEN = 10
	}

	/**
	 * The ShowControlInterface abstracts messages that are used to customize live lighting 
	 * effects that happen during gameplay. Lighting effects are based on the number of teams 
	 * in play and variables such as color, duration and pattern can be customized. The lighting 
	 * values are based on an ESC lighting cue sheet.
	 * 
	 */
	public class ShowControlInterface : MonoBehaviour {

		//! Groupings used for the teams; Internal use only!
		private enum LightingGroups {
			Grouping01 = 341, // TEAM1_OF_6
			Grouping02, // TEAM2_OF_6
			Grouping03, // TEAM3_OF_6
			Grouping04, // TEAM4_OF_6
			Grouping05, // TEAM5_OF_6
			Grouping06, // TEAM6_OF_6
			Grouping07, // TEAM1_OF_5
			Grouping08, // TEAM2_OF_5
			Grouping09, // TEAM3_OF_5
			Grouping10, // TEAM4_OF_5
			Grouping11, // TEAM5_OF_5
			Grouping12, // TEAM1_OF_4
			Grouping13, // TEAM2_OF_4
			Grouping14, // TEAM3_OF_4
			Grouping15, // TEAM4_OF_4
			Grouping16, // TEAM1_OF_3
			Grouping17, // TEAM2_OF_3
			Grouping18, // TEAM3_OF_3
			Grouping19, // TEAM1_OF_2
			Grouping20, // TEAM2_OF_2
			Grouping21	// TEAM1_OF_1
		}
		
		// Constants used to target each team for lighting functions
		public const uint TEAM_01 = 1;
		public const uint TEAM_02 = 2;
		public const uint TEAM_03 = 3;
		public const uint TEAM_04 = 4;
		public const uint TEAM_05 = 5;
		public const uint TEAM_06 = 6;
		// Constants used to target each player for lighting functions
		public const uint PLAYER_01 = 1;
		public const uint PLAYER_02 = 2;
		public const uint PLAYER_03 = 3;
		public const uint PLAYER_04 = 4;
		public const uint PLAYER_05 = 5;
		public const uint PLAYER_06 = 6;
		public const uint PLAYER_07 = 7;
		public const uint PLAYER_08 = 8;
		public const uint PLAYER_09 = 9;
		public const uint PLAYER_10 = 10;
		public const uint PLAYER_11 = 11;
		public const uint PLAYER_12 = 12;
		public const uint PLAYER_13 = 13;
		public const uint PLAYER_14 = 14;
		public const uint PLAYER_15 = 15;
		public const uint PLAYER_16 = 16;
		public const uint PLAYER_17 = 17;
		public const uint PLAYER_18 = 18;
		public const uint PLAYER_19 = 19;
		public const uint PLAYER_20 = 20;
		public const uint PLAYER_21 = 21;
		public const uint PLAYER_22 = 22;
		public const uint PLAYER_23 = 23;
		public const uint PLAYER_24 = 24;
		public const uint PLAYER_25 = 25;
		public const uint PLAYER_26 = 26;
		public const uint PLAYER_27 = 27;
		public const uint PLAYER_28 = 28;
		public const uint PLAYER_29 = 29;
		public const uint PLAYER_30 = 30;
		
		public const uint CUE_WALK_IN = 1011;					//!< Between-games look

		public const uint CUE_GAME_LAUNCH = 1031;				//!< Environment changes to match game theme

		public const uint CUE_FIND_LOCATION = 1041;				//!< Team Colors highlight player positions

		public const uint CUE_IDENTIFY_TEAMS = 1051;			//!< Identify teams

		public const uint CUE_TRAINING_MODE = 1061;				//!< Environment matches game theme

		public const uint CUE_GAME_PLAY = 1101;					//!< Lowers lighting levels

		public const uint CUE_TENSION_SLOW = 1111;				//!< Slow chase over players

		public const uint CUE_TENSION_MEDIUM = 1112;			//!< Medium chase over players

		public const uint CUE_TENSION_FAST = 1113;				//!< Fast chase over players

		public const uint CUE_TENSION_NONE = 1114;				//!< Darkness over the players

		public const uint CUE_END_ROUND = 1121;					//!< Increased lighting levels

		public const uint CUE_INTERMISSION = 1151;				//!< Intermission

		public const uint CUE_ANNOUNCE_LEADING_TEAM = 1131;		//!< Lighting Ballyhoo

		public const uint CUE_IDENTIFY_LEADING_PLAYER = 1141;	//!< Highlight leading player

		public const uint CUE_GAME_FINALE = 1801;				//!< game Finale

		//! Public integer constant for the OSC Port Number 
		protected const int DEFAULT_PORT_NUMBER = 6454;

		//! Stores the lighting server IP address
		protected const string DEFAULT_IP = "10.67.0.1";

		//! Remote endpoint port for all outbound calls
		private int port;

		//! Remote endpoint IPv4 address for all outbound calls
		private string showControlIpAddress;
		
		//! Contains cues for light control
		private Dictionary<string, string> cues;
		
		//! Thread safe Producer/Consumer Queue for incoming messages
		private Queue<OscMessage> processQueue = new Queue<OscMessage>();
		
		//! The local instance of the OSC transmitter for dispatching messages
		private OscTransmitter oscTransmitter;
		
		//! Local thread used for asynchronously dispatching messages
		private Thread thread;
		
		//! Flag used for flow control logic when using the additional thread
		private bool threadStarted = false;

		//! Max teams available
		private const int MAX_TEAMS = 6;

		//! Default of total teams set to 1
		private uint teamMode = 1;
		
		//! Determines whether or not the show control signal digest will be written out or not
		private bool writeLogOutput = false;

		// Lighting Constants 
		private const string LIGHTING_SUBMASTER = "/Vx76/Submasters/";
		private const string LIGHTING_MACROS = "/Vx76/Palettes/Macro";
		private const string LIGHTING_CUE = "Cue";
		private const string LIGHTING_HIGHLIGHTS = "22";
		private const string LIGHTING_WASH = "23";
		private const string LIGHTING_ACCENTS = "24";
		private const string LIGHTING_PULSE = "25";
		private const string LIGHTING_AMBIENT = "26";
		private const string LIGHTING_FANS = "27";
		private const string LIGHTING_STROBES = "28";
		private const string LIGHTING_ATMOSPHERE = "29";
		private const string LIGHTING_EVENT = "30";
		
		//! Provides a Singleton Instance of this class
		private static ShowControlInterface _instance;
		public static ShowControlInterface Instance {
			get {
				if (!_instance) {
					_instance = GameObject.FindObjectOfType(typeof(ShowControlInterface)) as ShowControlInterface;
					if (!_instance) {
						GameObject container = new GameObject();
						container.name = "ShowControlInterfaceObject";
						_instance = container.AddComponent(typeof(ShowControlInterface)) as ShowControlInterface;
					}
				}
				
				return _instance;
			}
		}

		//! Queries the host machine for a show control interface config file. If available, parses it asynchronously.
		void Awake () 
		{
			QueryLocation();
		}

		//! Causes processing thread to be terminated
		void OnDestroy () 
		{
			this.threadStarted = false;
			if (null  != this.thread && this.thread.IsAlive) this.thread.Join();
		}
	
		/**
		 * Attempts to store a label for the location by loading and parsing a specific file from the disk.
		 * The file loaded is: ~/lighting
		 */
		private void QueryLocation () 
		{
			string appPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			appPath = appPath.Replace("/Documents", "");
			string locationPath = appPath + "/lighting";
		
			StartCoroutine(LoadLightingFile(locationPath));
		}

		/**
		 * Takes any string input and attempts to validate its format as an IPv4 address
		 * 
		 * @param inputAddress any string input that might be used as an address
		 * @return a valid IPv4 address or an empty string if address cannot be parsed
		 */
		private string ParseAddress (string inputAddress)
		{
			try {
				// Create an instance of IPAddress for the specified address string (in  
				// dotted-quad, or colon-hexadecimal notation).
				IPAddress address = IPAddress.Parse(inputAddress);
				return address.ToString();
			}
			catch (Exception) {
				Debug.LogWarning("ShowControlInterface : failed to parse IPv4 address: " + inputAddress);
			}
			
			return "";
		}

		/**
		 * Coroutine method used for parsing a string from the raw contents of a file located on the disk at the path given as input.
		 * Assigns default values for lighting address and port number if file cannot be found or has invalid contents.
		 * 
		 * @param locationPath directory path to a file that represents the location of the desired string data
		 */
		private IEnumerator LoadLightingFile (string locationPath) 
		{
			WWW locationFile = new WWW("file://" + locationPath);
			yield return locationFile;

			// if location file was not found or was not loaded we exit early
			if (!String.IsNullOrEmpty(locationFile.error)) {
				this.port = DEFAULT_PORT_NUMBER;
				this.showControlIpAddress = DEFAULT_IP;
				this.oscTransmitter = new OscTransmitter(this.showControlIpAddress, this.port);
				
				SpawnThread();
				
				yield break;
			}

			// only use the first line of the file that was loaded
			string rawLocationText = locationFile.text;
			string[] newLineDelimiter = new string[] {"\n"};
			string[] lineDelimitedFile = rawLocationText.Split(newLineDelimiter, StringSplitOptions.None);
			rawLocationText = lineDelimitedFile[0].Trim();
			string[] portDelimiter = new string[] {":"};
			string[] endpointStr = rawLocationText.Split(portDelimiter, StringSplitOptions.None);
			string ipv4Address = "";
			int portNum = 0;
			if (2 == endpointStr.Length) 
			{
				ipv4Address = ParseAddress(endpointStr[0].Trim());
				if ("" != ipv4Address) {
					try {
						portNum = Convert.ToInt32(endpointStr[1].Trim());
					}
					catch (Exception) {
						Debug.LogWarning("ShowControlInterface : failed to parse port number: " + endpointStr[1].Trim());
					}
				}
			}
		
			// handle degenerate case or invalid input
			if ("" == ipv4Address || portNum <= 0 || portNum >= 65535) {
				this.port = DEFAULT_PORT_NUMBER;
				this.showControlIpAddress = DEFAULT_IP;
			}
			else {
				this.port = portNum;
				this.showControlIpAddress = ipv4Address;
			}
			
			// create osc transmitter
			this.oscTransmitter = new OscTransmitter(this.showControlIpAddress, this.port);
			
			SpawnThread();
		}

		/**
		 * Spawns thread to listen and send messages using the port number
		 * denoted by the constructor.
		 */
		public void SpawnThread () 
		{
			// spawn thread
			this.threadStarted = true;
			this.thread = new Thread(new ThreadStart(ThreadUpdate));
			this.thread.Start();
		}

		/**
		 * Accepts team value and team lighting mode and returns the overhead lighting 
		 * enum associated with that team for futher processing. 
		 * 
		 * @param teamGroup index for the desired team; valid input: [1-6]
		 * @param teamLightingMode the current team lighting mode; valid input: [1-6]
		 * Return corresponding overhead lighting based on number of teams
		 */
		private LightingGroups SelectGroupingForTeam (uint teamGroup, uint teamLightingMode)
		{
			LightingGroups lightingMaster = LightingGroups.Grouping21; //Default

			switch (teamLightingMode) {
				case 0: 
				case 1: 
					lightingMaster = LightingGroups.Grouping21;
					break;

				case 2: 
					switch (teamGroup) {
						 case 1: lightingMaster = LightingGroups.Grouping19; break;
						 case 2: lightingMaster = LightingGroups.Grouping20; break;
						 default: lightingMaster = LightingGroups.Grouping19; break;
					}
					break;
				
				case 3: 
					switch (teamGroup) {
						case 1: lightingMaster = LightingGroups.Grouping16; break;
						case 2: lightingMaster = LightingGroups.Grouping17; break;
						case 3: lightingMaster = LightingGroups.Grouping18; break;
						default: lightingMaster = LightingGroups.Grouping16; break;
					}
					break;
				
				case 4: 
					switch (teamGroup) {
						case 1: lightingMaster = LightingGroups.Grouping12; break;
						case 2: lightingMaster = LightingGroups.Grouping13; break;
						case 3: lightingMaster = LightingGroups.Grouping14; break;
						case 4: lightingMaster = LightingGroups.Grouping15; break;
						default: lightingMaster = LightingGroups.Grouping12; break;
					}
					break;
				
				case 5: 
					switch (teamGroup) {
						case 1: lightingMaster = LightingGroups.Grouping07; break;
						case 2: lightingMaster = LightingGroups.Grouping08; break;
						case 3: lightingMaster = LightingGroups.Grouping09; break;
						case 4: lightingMaster = LightingGroups.Grouping10; break;
						case 5: lightingMaster = LightingGroups.Grouping11; break;
						default: lightingMaster = LightingGroups.Grouping07; break;
					}
					break;
					
				case 6: 
					switch (teamGroup) {
						case 1: lightingMaster = LightingGroups.Grouping01; break;
						case 2: lightingMaster = LightingGroups.Grouping02; break;
						case 3: lightingMaster = LightingGroups.Grouping03; break;
						case 4: lightingMaster = LightingGroups.Grouping04; break;
						case 5: lightingMaster = LightingGroups.Grouping05; break;
						case 6: lightingMaster = LightingGroups.Grouping06; break;				
						default: lightingMaster = LightingGroups.Grouping01; break;
					}
					break;
					
			}
			
			return lightingMaster;
		}
		
		/** 
		 * Set total available teams on the lighting console
		 *
		 * @param team the set of teams requested for the game
		 */
		public void SelectTeamLightingMode (Team team)
		{
			uint teamValue = (uint) team;
			if (teamValue < 1 || teamValue > MAX_TEAMS){
				Debug.LogWarning("" + teamValue + " is an invalid team size. Team value must be between [1 and 6]");
				return;
			}
			
			this.teamMode = teamValue;
			string address = LIGHTING_MACROS;
			float oscValue = 20.0f + (float) this.teamMode;

			OscMessage message = new OscMessage("");
			message.Address = address;
			message.Append(oscValue);

			// thread-safe dispatch...
			Monitor.Enter(processQueue);
			try {
				processQueue.Enqueue(message);
			}
			finally {
				Monitor.Exit(processQueue);
			}
		}
		
		/**
		 * Highlight individual player for a given duration
		 *
		 * @param player must be set to [1,30]
		 * @param duration of lighting effect
		 */
		public void HighlightPlayer (uint player, Duration duration)
		{
			if (player < 1 || player > 30) {
				Debug.LogWarning("player value must be between [1 and 30]. Ignoring player highlight.");
				return;
			}
			
			int durationValue = (int) duration;
			float oscValue = (player + 300) + (durationValue / 100f);

			// Create Osc Message 
			string address = LIGHTING_SUBMASTER;
			OscMessage message = new OscMessage("");
			address += LIGHTING_HIGHLIGHTS + "/";
			address += LIGHTING_CUE;
			
			message.Address = address;
			message.Append(oscValue);
				
			// thread-safe dispatch...
			Monitor.Enter(processQueue);
			try {
				processQueue.Enqueue(message);
			}
			finally {
				Monitor.Exit(processQueue);
			}
		}
		
		
		/**
		 * Highlight individual team for a given color and timing values
		 *
		 * @param player must be set to [1,30]
		 * @param duration of lighting effect
		 */
		public void SetLightingForTeam (uint team, LightingColor color, Timing timing)
		{
			if (team < 1 || team > this.teamMode) {
				Debug.LogWarning("team value must be between [1 and " + this.teamMode.ToString() + "]. Ignoring team highlight.");
				return;
			}

			int colorValue = (int) color;
			int timingValue = (int) timing;
			float oscValue = colorValue + (timingValue / 100f);

			// Create Osc Message 
			string address = LIGHTING_SUBMASTER;
			OscMessage message = new OscMessage("");
			address += team.ToString() + "/";
			address += LIGHTING_CUE;
			
			message.Address = address;
			message.Append(oscValue);
				
			// thread-safe dispatch...
			Monitor.Enter(processQueue);
			try {
				processQueue.Enqueue(message);
			}
			finally {
				Monitor.Exit(processQueue);
			}
		}
		
		/**
		 * Temporarily highlight a team over a duration (uses lighting color from SetLightingForTeam)
		 *
		 * @param team must be set to [1,Total number of teams]
		 * @param duration of lighting effect
		 */
		public void HighlightTeam (uint team, Duration duration)
		{
			if (team < 1 || team > this.teamMode) {
				Debug.LogWarning("team value must be between [1 and " + this.teamMode.ToString() + "]. Ignoring team highlight.");
				return;
			}
			
			LightingGroups grouping = SelectGroupingForTeam(team, this.teamMode);
			
			int durationValue = (int) duration;
			int teamGroupingValue = (int) grouping;
			float oscValue = teamGroupingValue + (durationValue / 100f);

			// Create Osc Message 
			string address = LIGHTING_SUBMASTER;
			OscMessage message = new OscMessage("");
			address += LIGHTING_HIGHLIGHTS + "/";
			address += LIGHTING_CUE;
			
			message.Address = address;
			message.Append(oscValue);
				
			// thread-safe dispatch...
			Monitor.Enter(processQueue);
			try {
				processQueue.Enqueue(message);
			}
			finally {
				Monitor.Exit(processQueue);
			}
		}
		
		/**
		 * Specify lighting wash
		 *
		 * @param color wash color for the lighting effect
		 * @param timing for the lighting effect
		 */
		public void SetLightingWash (LightingColor color, Timing timing)
		{
			int colorValue = (int) color;
			int timingValue = (int) timing;
			float oscValue = colorValue + (timingValue / 100f);

			string address = LIGHTING_SUBMASTER;

			OscMessage message = new OscMessage("");
			address += LIGHTING_WASH + "/";
			address += LIGHTING_CUE;

			message.Address = address;
			message.Append(oscValue);

			// thread-safe dispatch...
			Monitor.Enter(processQueue);
			try {
				processQueue.Enqueue(message);
			}
			finally {
				Monitor.Exit(processQueue);
			}
		}

		/**
		 * Specify lighting accent color and timing
		 *
		 * @param color wash color for the lighting effect
		 * @param timing for the lighting effect
		 */
		public void SetLightingAccents (LightingColor color, Timing timing)
		{
			int colorValue = (int) color;
			int timingValue = (int) timing;
			float oscValue = colorValue + (timingValue / 100f); 

			// Create Osc Message 
			string address = LIGHTING_SUBMASTER;
			OscMessage message = new OscMessage("");
			address += LIGHTING_ACCENTS + "/";
			address += LIGHTING_CUE;
			
			message.Address = address;
			message.Append(oscValue);
				
			// thread-safe dispatch...
			Monitor.Enter(processQueue);
			try {
				processQueue.Enqueue(message);
			}
			finally {
				Monitor.Exit(processQueue);
			}
		}

		/**
		 * Specify lighting wash pulse
		 *
		 * @param pulse lighting pulse pattern
		 */
		public void SetLightingPulse (Pulse pulse)
		{
			int pulseValue = (int) pulse;
			float oscValue = (float) pulseValue; 

			string address = LIGHTING_SUBMASTER;

			OscMessage message = new OscMessage("");
			address += LIGHTING_PULSE + "/";
			address += LIGHTING_CUE;
			
			message.Address = address;
			message.Append(oscValue);
			
			// thread-safe dispatch...
			Monitor.Enter(processQueue);
			try {
				processQueue.Enqueue(message);
			}
			finally {
				Monitor.Exit(processQueue);
			}
		}
		
		/**
		 * Specify lighting ambient color and duration
		 *
		 * @param color wash color for the lighting effect
		 * @param timing for the lighting effect
		 */
		public void SetAmbientLighting (LightingColor color, Timing timing)
		{
			int colorValue = (int) color;
			int timingValue = (int) timing;
			float oscValue = colorValue + (timingValue / 100f);

			string address = LIGHTING_SUBMASTER;

			OscMessage message = new OscMessage("");
			address += LIGHTING_AMBIENT + "/";
			address += LIGHTING_CUE;
			
			message.Address = address;
			message.Append(oscValue);
				
			// thread-safe dispatch...
			Monitor.Enter(processQueue);
			try {
				processQueue.Enqueue(message);
			}
			finally {
				Monitor.Exit(processQueue);
			}
		}

		/**
		 * Specify lighting fan group and speed 
		 *
		 * @param groupValue the set of fans to run
		 * @param speedValue the speed at which to run the fans
		 */
		public void SetFans (Grouping groupValue, Speed speedValue)
		{
			int fanGroup = (int) groupValue;
			int fanSpeed = (int) speedValue; 	
 			float oscValue = fanGroup + (fanSpeed / 100f); 

			string address = LIGHTING_SUBMASTER;

			OscMessage message = new OscMessage("");
			address += LIGHTING_FANS + "/";
			address += LIGHTING_CUE;
			
			message.Address = address;
			message.Append(oscValue);
			
			// thread-safe dispatch...
			Monitor.Enter(processQueue);
			try {
				processQueue.Enqueue(message);
			}
			finally {
				Monitor.Exit(processQueue);
			}
		}

		/**
		 * Specify lighting strobes with a moving pattern 
		 *
		 * @param groupValue the set of strobe lights to flash
		 * @param patternValue the pattern used for flashing the lights
		 */
		public void SetStrobes (Grouping groupValue, Pattern patternValue)
		{
			int strobeGroup = (int) groupValue; 
			int strobePattern = (int) patternValue; 
 			float oscValue = strobeGroup + (strobePattern / 100f); 

			string address = LIGHTING_SUBMASTER;

			OscMessage message = new OscMessage("");
			address += LIGHTING_STROBES + "/";
			address += LIGHTING_CUE;
			
			message.Address = address;
			message.Append(oscValue);
				
			// thread-safe dispatch...
			Monitor.Enter(processQueue);
			try {
				processQueue.Enqueue(message);
			}
			finally {
				Monitor.Exit(processQueue);
			}
		}
		
		/**
		 * Specify lighting atmosphere 
		 *
		 * @param levelValue the amount of "atmosphere" effect applied
		 */
		private void SetAtmosphere (Level levelValue)
		{
			int atmosphereLevel_int = (int) levelValue;
			float atmosphereLevel = (float) atmosphereLevel_int;

			string address = LIGHTING_SUBMASTER;

			OscMessage message = new OscMessage("");
			address += LIGHTING_ATMOSPHERE + "/";
			address += LIGHTING_CUE;

			message.Address = address;
			message.Append(atmosphereLevel);
				
			// thread-safe dispatch...
			Monitor.Enter(processQueue);
			try {
				processQueue.Enqueue(message);
			}
			finally {
				Monitor.Exit(processQueue);
			}
		}
		
		/**
		 * Invokes a generic show control event with the given cue number and variant.
		 * 
		 * @param cueEvent number must be between 1 and 9999
		 * @param cueVariant must be between 1 and 99
		 */
		public void InvokeLightingCue (uint cueEvent, uint cueVariant)
		{
			if (cueEvent > 9999) {
				Debug.LogWarning("Cue event must be between 0 and 9999");
				return;
			}
			else if (cueVariant > 99) {
				Debug.LogWarning("Cue variant must be between 0 and 99");
				return;
			}

			float lightingCue = (float) cueEvent + (cueVariant / 100f);

			string address = LIGHTING_SUBMASTER;

			OscMessage message = new OscMessage("");
			address += LIGHTING_EVENT + "/";
			address += LIGHTING_CUE;
			
			message.Address = address;
			message.Append(lightingCue);

			// thread-safe dispatch...
			Monitor.Enter(processQueue);
			try {
				processQueue.Enqueue(message);
			}
			finally {
				Monitor.Exit(processQueue);
			}
		}
		
		/**
		 * Invokes a show control message using the specified string address and cue number.
		 * The input parameters will do nothing if the message format and payload is not preprogrammed 
		 * on the lighting console.
		 */
		private void InvokeGenericCue (string cueAddress, float cueNumber)
		{
			OscMessage message = new OscMessage(cueAddress, cueNumber);
			
			// thread-safe dispatch...
			Monitor.Enter(processQueue);
			try {
				processQueue.Enqueue(message);
			}
			finally {
				Monitor.Exit(processQueue);
			}
		}

		//! Start lighting system setup 
		public void SystemSetup ()
		{
			string address = LIGHTING_MACROS;
			float oscValue = 1.0f;

			OscMessage message = new OscMessage("");
			message.Address = address;
			message.Append(oscValue);

			// thread-safe dispatch...
			Monitor.Enter(processQueue);
			try {
				processQueue.Enqueue(message);
			}
			finally {
				Monitor.Exit(processQueue);
			}
		}
		
		//! Closes existing connection with show control unit, re-opens connection with specified endpoint.
		public void ResetEndpoint (string ipAddress, int port) 
		{
			this.showControlIpAddress = ipAddress;
			this.port = port;
			
			if (this.oscTransmitter.IsConnected()) {
				this.oscTransmitter.Close();
			}
			
			this.oscTransmitter = new OscTransmitter(this.showControlIpAddress, this.port);
		}

		//! Toggles the flag for writing output to console or log file
		public void enableLogOutput (bool toggle = true)
		{
			this.writeLogOutput = toggle;
		}

		//! Update messages in queue
		private void ThreadUpdate () 
		{
			while (this.threadStarted) {				
				// see if there are any more messages, if so send them ...
				if (Monitor.TryEnter(processQueue)) {
				    try {
						if (processQueue.Count > 0) {
							OscMessage message = processQueue.Dequeue();
							ProcessMessage(message);
						}
				    }
				    finally {
				        Monitor.Exit(processQueue);
				    }
				}

				// Must apply delay for rate limiting messages (but 5 ms may not be enough...)
				Thread.Sleep(5);
			}
		}

		//! Process input message by sending it using the existing transmitter
		private void ProcessMessage (OscMessage message)
		{			
			// send actual message...
			this.oscTransmitter.Send(message);
		//				UnityMainThreadDispatcher.Instance ().Enqueue (ExsecuteOnMainThread_TimeCheck ());
			if (this.writeLogOutput) {
#if UNITY_EDITOR
				Debug.Log("ShowControlInterface sending: " + message.Address + " (" + (message.Values.Count > 0 ? message.Values[0].ToString() : "") + ")" );
#else
				GameInterface.writeLog("ShowControlInterface sending: " + message.Address + " (" + (message.Values.Count > 0 ? message.Values[0].ToString() : "") + ")" );
#endif
			}
		}
//				// Added By Kevin!
//				public IEnumerator ExsecuteOnMainThread_TimeCheck ()
//				{
//						LoadSave.AddData (" : UDP OUT SHOWCONTROL :  ");
//						yield return null;
//				}
		
	}
}