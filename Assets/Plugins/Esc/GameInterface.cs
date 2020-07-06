using UnityEngine;
using System.Text;
using System.Collections;
using System.Runtime.InteropServices;

namespace Esc {

	internal class GameInterface : System.Object {

		public static void goFullScreen () 
		{
			if (!Application.isEditor) {
				setFullscreen();
			} 
		}

		public static void goFullScreenDevMode () 
		{
			if (!Application.isEditor) {
				setFullscreenDeveloperMode();
			} 
		}

		public static void leaveFullScreen () 
		{
			if (!Application.isEditor) {
				exitFullscreen();
			} 
		}

		public static void setHidden () 
		{
			if (!Application.isEditor) {
				setWindowHidden ();
			} 
		}

		[DllImport ("ESC-Unity-Plugin", CharSet = CharSet.Ansi)]
		public static extern int launchGameForUrl( string url );

		[DllImport ("ESC-Unity-Plugin")]
		public static extern int setWindowHidden ();

		[DllImport ("ESC-Unity-Plugin")]
		public static extern int setFullscreen ();

		[DllImport ("ESC-Unity-Plugin")]
		public static extern int setFullscreenDeveloperMode ();

		[DllImport ("ESC-Unity-Plugin")]
		public static extern int exitFullscreen ();

		[DllImport ("ESC-Unity-Plugin")]
		public static extern int writeLog ( string message );

		[DllImport ("ESC-Unity-Plugin")]
		public static extern int accessWindow ( StringBuilder id, uint idCapacity );
		
		[DllImport ("ESC-Unity-Plugin")]
		public static extern int getGameIdentifier( StringBuilder gameId, uint gameIdCapacity );
		
		[DllImport ("ESC-Unity-Plugin")]
		public static extern int startLauncherInterface ();

 		[DllImport ("ESC-Unity-Plugin")]
		public static extern int startServerInterface ();

		[DllImport ("ESC-Unity-Plugin", CharSet = CharSet.Ansi)]
		public static extern int startServerInterfaceWithHost ( string hostname );
		
		[DllImport ("ESC-Unity-Plugin")]
		public static extern int stopInterface ();
		
		[DllImport ("ESC-Unity-Plugin")]
		public static extern bool isConnected ();
		
		[DllImport ("ESC-Unity-Plugin")]
		public static extern int startGame ();
		
		[DllImport ("ESC-Unity-Plugin")]
		public static extern int cancelGame ();
		
		[DllImport ("ESC-Unity-Plugin")]
		public static extern int endGame ();
		
		[DllImport ("ESC-Unity-Plugin")]
		public static extern int startRound ();
		
		[DllImport ("ESC-Unity-Plugin")]
		public static extern int cancelRound ();
		
		[DllImport ("ESC-Unity-Plugin")]
		public static extern int endRound ();
		
		[DllImport ("ESC-Unity-Plugin")]
		public static extern int pauseGame ( bool toggle );
		
		[DllImport ("ESC-Unity-Plugin")]
		public static extern bool hasMoreEvents ();

		[DllImport ("ESC-Unity-Plugin", CharSet = CharSet.Ansi)]
		public static extern int getNextEvent ( StringBuilder username, uint usernameCapacity, StringBuilder message, uint messageCapacity );
		
		[DllImport ("ESC-Unity-Plugin")]
		public static extern bool hasMorePresenceEvents ();

		[DllImport ("ESC-Unity-Plugin", CharSet = CharSet.Ansi)]
		public static extern int getNextPresenceEvent ( StringBuilder username, int usernameCapacity, out int presence );
		
		[DllImport ("ESC-Unity-Plugin")]
		public static extern bool hasMoreStatusChanges ();

		[DllImport ("ESC-Unity-Plugin")]
		public static extern int getNextStatusChange ( out int status );
		
		[DllImport ("ESC-Unity-Plugin", CharSet = CharSet.Ansi)]
		public static extern void broadcastEvent ( string message );

		[DllImport ("ESC-Unity-Plugin", CharSet = CharSet.Ansi)]
		public static extern int dispatchEvent ( string username, string message );

		[DllImport ("ESC-Unity-Plugin", CharSet = CharSet.Ansi)]
		public static extern int getLauncherInfoPlist ( string filename, StringBuilder contents, uint contentCapacity );
		
		[DllImport ("ESC-Unity-Plugin")]
		public static extern int getUniqueDeviceIdenifier ( StringBuilder id, uint idCapacity );

		[DllImport ("ESC-Unity-Plugin")]
		public static extern int getGoogleAnalyticsIdentifier ( StringBuilder id, uint idCapacity );
		
	}

}