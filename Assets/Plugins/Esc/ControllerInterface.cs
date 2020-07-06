using UnityEngine;
using System.Text;
using System.Collections;
using System.Runtime.InteropServices;

namespace Esc {
	
	/**
	 * The ControllerInterface abstracts plugin details that are dependent 
	 * upon the current runtime (either running inside Unity or on the iOS device).
	 */
	internal class ControllerInterface : System.Object {

		public static int accessWindow ( StringBuilder id, uint idCapacity )
		{
			if (RuntimePlatform.OSXEditor == Application.platform ||
				RuntimePlatform.OSXPlayer == Application.platform) {
				return ControllerInterface_desktop.accessWindow (id, idCapacity);
			}
			else if (RuntimePlatform.Android == Application.platform ||
				RuntimePlatform.IPhonePlayer == Application.platform) {
				return ControllerInterface_mobile.accessWindow (id, idCapacity);
			}

			return -1;
		}
		
		/**
		 * Start the Docent Controller Interface with the given host name 
		 * 
		 * @return zero upon success, otherwise an error code is returned.
		 */
		public static int startDocentInterface ( string hostname = "" )
		{
			if (hostname.Equals("")) {
				hostname = "esc-game-server.local";
			}
			
			if (RuntimePlatform.OSXEditor == Application.platform ||
				RuntimePlatform.OSXPlayer == Application.platform) {
				return ControllerInterface_desktop.startDocentInterface( hostname );
			}
			else if (RuntimePlatform.Android == Application.platform ||
				RuntimePlatform.IPhonePlayer == Application.platform) {
				return ControllerInterface_mobile.startDocentInterface( hostname );
			}

			return -1;
		}
		
		/**
		 * Start the Client Controller Interface with the given host name 
		 * 
		 * @return zero upon success, otherwise an error code is returned.
		 */
		public static int startClientInterface ( string username = "", string hostname = "" )
		{
			if (username.Equals("")) {
				username = getUniqueDeviceIdentifier();
			}
			
			if (hostname.Equals("")) {
				hostname = "esc-game-server.local";
			}

			if (RuntimePlatform.OSXEditor == Application.platform ||
				RuntimePlatform.OSXPlayer == Application.platform) {
				return ControllerInterface_desktop.startClientInterface( username, hostname );
			}
			else if (RuntimePlatform.Android == Application.platform ||
				RuntimePlatform.IPhonePlayer == Application.platform) {
				return ControllerInterface_mobile.startClientInterface( username, hostname );
			}

			return -1;
		}

		/**
		 * Stop the Client Controller Interface
		 *
		 * @return zero upon success, otherwise an error code is returned.
		 */
		public static int stopInterface ()
		{
			if (RuntimePlatform.OSXEditor == Application.platform ||
			    RuntimePlatform.OSXPlayer == Application.platform) {
				return ControllerInterface_desktop.stopInterface();
			}
			else if (RuntimePlatform.Android == Application.platform ||
				RuntimePlatform.IPhonePlayer == Application.platform) {
				return ControllerInterface_mobile.stopInterface();
			}

			return -1;
		}

		//! Returns TRUE if the Client Controller Interface is connected, else returns FALSE  
		public static bool isConnected ()
		{
			if (RuntimePlatform.OSXEditor == Application.platform ||
			    RuntimePlatform.OSXPlayer == Application.platform) {
				return ControllerInterface_desktop.isConnected();
			}
			else if (RuntimePlatform.Android == Application.platform ||
				RuntimePlatform.IPhonePlayer == Application.platform) {
				return ControllerInterface_mobile.isConnected();
			}

			return false;
		}

		//! Returns TRUE if the Client Controller Interface has more EscEvents, else returns FALSE  
		public static bool hasMoreEvents ()
		{
			if (RuntimePlatform.OSXEditor == Application.platform ||
			    RuntimePlatform.OSXPlayer == Application.platform) {
				return ControllerInterface_desktop.hasMoreEvents();
			}
			else if (RuntimePlatform.Android == Application.platform ||
				RuntimePlatform.IPhonePlayer == Application.platform) {
				return ControllerInterface_mobile.hasMoreEvents();
			}

			return false;
		}

		/**
		 * Returns the next event username and message string by reference using the buffers
		 * supplied as arguments.
		 *
		 * @param StringBuilder username the name of the user
		 * @param uint usernameCapacity the unsigned integer string capacity  
		 * @param StringBuilder message the message from the user
		 * @param uint messageCapacity the unsigned integer string capacity  
		 * @return zero upon success, otherwise an error code is returned.
		 */
		public static int getNextEvent ( StringBuilder username, uint usernameCapacity, StringBuilder message, uint messageCapacity )
		{
			if (RuntimePlatform.OSXEditor == Application.platform ||
			    RuntimePlatform.OSXPlayer == Application.platform) {
				return ControllerInterface_desktop.getNextEvent( username, usernameCapacity, message, messageCapacity );
			}
			else if (RuntimePlatform.Android == Application.platform ||
				RuntimePlatform.IPhonePlayer == Application.platform) {
				return ControllerInterface_mobile.getNextEvent( username, usernameCapacity, message, messageCapacity );
			}

			return -1;
		}

		//! Returns TRUE if there are more presence events, else returns FALSE
		public static bool hasMorePresenceEvents ()
		{
			if (RuntimePlatform.OSXEditor == Application.platform ||
			    RuntimePlatform.OSXPlayer == Application.platform) {
				return ControllerInterface_desktop.hasMorePresenceEvents();
			}
			else if (RuntimePlatform.Android == Application.platform ||
				RuntimePlatform.IPhonePlayer == Application.platform) {
				return ControllerInterface_mobile.hasMorePresenceEvents();
			}

			return false;
		}

		/**
		 * Get the next Presence event
		 * 
		 * @param StringBuilder username the name of the user
		 * @param uint usernameCapacity the unsigned integer string capacity  
		 * @param StringBuilder message the message from the user
		 * @param int presence pass the presence integer value by reference 
		 * @return zero upon success, otherwise an error code is returned.
		 */
		public static int getNextPresenceEvent ( StringBuilder username, uint usernameCapacity, out int presence )
		{
			if (RuntimePlatform.OSXEditor == Application.platform ||
			    RuntimePlatform.OSXPlayer == Application.platform) {
				return ControllerInterface_desktop.getNextPresenceEvent( username, usernameCapacity, out presence );
			}
			else if (RuntimePlatform.Android == Application.platform ||
				RuntimePlatform.IPhonePlayer == Application.platform) {
				return ControllerInterface_mobile.getNextPresenceEvent( username, usernameCapacity, out presence );
			}

			presence = -1;
			return -1;
		}

		//! Returns TRUE if there are more status changes, else returns FALSE
		public static bool hasMoreStatusChanges ()
		{
			if (RuntimePlatform.OSXEditor == Application.platform ||
			    RuntimePlatform.OSXPlayer == Application.platform) {
				return ControllerInterface_desktop.hasMoreStatusChanges();
			}
			else if (RuntimePlatform.Android == Application.platform ||
				RuntimePlatform.IPhonePlayer == Application.platform) {
				return ControllerInterface_mobile.hasMoreStatusChanges();
			}

			return false;
		}

		/**
		 * Get the next status change 
		 *
		 * @param int status pass the status integer value by reference 
		 * @return zero upon success, otherwise an error code is returned.
		 */
		public static int getNextStatusChange ( out int status )
		{
			if (RuntimePlatform.OSXEditor == Application.platform ||
			    RuntimePlatform.OSXPlayer == Application.platform) {
				return ControllerInterface_desktop.getNextStatusChange( out status );
			}
			else if (RuntimePlatform.Android == Application.platform ||
				RuntimePlatform.IPhonePlayer == Application.platform) {
				return ControllerInterface_mobile.getNextStatusChange( out status );
			}

			status = -1;
			return -1;
		}

		/**
		 * Dispatch an event with a username and message
		 * 
		 * @param string username dispatch to the peer with string username 
		 * @param string message  ddispatch to the peer with string message 
		 * @return zero upon success, otherwise an error code is returned.
		 */
		public static int dispatchEvent ( string username, string message )
		{
			if (RuntimePlatform.OSXEditor == Application.platform ||
			    RuntimePlatform.OSXPlayer == Application.platform) {
				return ControllerInterface_desktop.dispatchEvent( username, message );
			}
			else if (RuntimePlatform.Android == Application.platform ||
				RuntimePlatform.IPhonePlayer == Application.platform) {
				return ControllerInterface_mobile.dispatchEvent( username, message );
			}

			return -1;
		}
		
		/**
		 * Collect the unique machine identifier
		 * 
		 * @param StringBuilder id buffer to load unique identifier into
		 * @param uint idCapacity size of the id buffer
		 * @return zero upon success, otherwise an error code is returned.
		 */
		public static string getUniqueDeviceIdentifier ()
		{
			if (RuntimePlatform.OSXEditor == Application.platform ||
			    RuntimePlatform.OSXPlayer == Application.platform) {
				StringBuilder udid = new StringBuilder(48);
				int status = ControllerInterface_desktop.getUniqueDeviceIdentifier( udid, (uint) udid.Capacity );
				return udid.ToString();
			}
			else if (RuntimePlatform.Android == Application.platform ||
				RuntimePlatform.IPhonePlayer == Application.platform) {
				return SystemInfo.deviceUniqueIdentifier;
			}

			return "";
		}

		public static bool isDeviceCharging ()
		{
			if (RuntimePlatform.Android == Application.platform) {
				bool status = false;
				/*
				AndroidJavaClass javaClass = new AndroidJavaClass("com.esc.plugin.EscAndroidBridge");
				if (javaClass != null) {
					AndroidJavaObject javaObject = new AndroidJavaObject("com.esc.plugin.EscAndroidBridge");
					if (javaObject != null) 
						status = javaObject.Call<bool>("isDeviceCharging");
				}
				*/
				return status;
			}
			else if(RuntimePlatform.IPhonePlayer == Application.platform) {
				return ControllerInterface_mobile.isDeviceCharging();
			}

			return false;
		}

		public static bool isDeviceWifiConnected ()
		{
			if (RuntimePlatform.Android == Application.platform) {
				bool status = false;
				/*
				AndroidJavaClass javaClass = new AndroidJavaClass("com.esc.plugin.EscAndroidBridge");
				if (javaClass != null) {
					AndroidJavaObject javaObject = new AndroidJavaObject("com.esc.plugin.EscAndroidBridge");
					if (javaObject != null) 
						status = javaObject.Call<bool>("isDeviceWifiConnected");
				}
				*/
				return status;
			}
			else if(RuntimePlatform.IPhonePlayer == Application.platform) {
				return ControllerInterface_mobile.isDeviceWifiConnected();
			}

			return false;
		}

		public static bool dimScreen ()
		{
			if (RuntimePlatform.Android == Application.platform) {
				bool status = false; 
				/*
				AndroidJavaClass javaClass = new AndroidJavaClass("com.esc.plugin.EscAndroidBridge");
				if (javaClass != null) {
					AndroidJavaObject javaObject = new AndroidJavaObject("com.esc.plugin.EscAndroidBridge");
					if (javaObject != null) 
						status = javaObject.Call<bool>("dimScreen");
				}
				*/
				return status;
			}
			else if(RuntimePlatform.IPhonePlayer == Application.platform) {
				return ControllerInterface_mobile.dimScreen();
			}

			return false;
		}

		public static bool brightenScreen ()
		{
			if (RuntimePlatform.Android == Application.platform) {
				bool status = false;
				/*
				AndroidJavaClass javaClass = new AndroidJavaClass("com.esc.plugin.EscAndroidBridge");
				if (javaClass != null) {
					AndroidJavaObject javaObject = new AndroidJavaObject("com.esc.plugin.EscAndroidBridge");
					if (javaObject != null) 
						status = javaObject.Call<bool>("brightenScreen");
				}
				*/
				return status;
			}
			else if(RuntimePlatform.IPhonePlayer == Application.platform) {
				return ControllerInterface_mobile.brightenScreen();
			}

			return false;
		}
	}
}