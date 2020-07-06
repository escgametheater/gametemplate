using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Esc;
using Osc;

namespace Esc
{

		/**
	 * The Peer class is the base class for the Server and Client classes.
	 * It is designed to organize messaging based on the sender or receiver.
	 * Each Peer controls its own internal state transmitter. The state
	 * transmitter is invoked by the ServerConnection to propagate state
	 * updates to the clients. Accordingly, the ClientConnection invokes 
	 * the state transmitter to send state updates to the server.
	 * 
	 * Each Peer instance also contains a list of events that have yet to
	 * be processed. 
	 * 
	 * @see Esc.Client
	 * @see Esc.Server
	 */
		public class Peer : System.Object
		{

				protected OscTransmitter stateTransmitter;
				protected Dictionary<string,object> stateVars;
				protected List<EscEvent> events;
				protected string username;
				protected string remoteIp;
				protected int remotePort;
				protected int status;
				protected bool stateVarsInvalidated;

				//! Constructor with sets the username with name
				public Peer (string name)
				{
						SetUsername (name);

						this.events = new List<EscEvent> ();
						this.stateVars = new Dictionary<string,object> ();
			
						this.stateVarsInvalidated = false;
				}

				//! Destructor
				~Peer ()
				{
						ShutdownTransmitter ();
				}

				/**
		 * Sets the state for a key value pair
		 * @param key the string key 
		 * @param value the object value
		 */
				public void SetStateVar (string key, object value)
				{
						if (this.stateVars.ContainsKey (key)) {
								this.stateVars [key] = value;
						} else {
								this.stateVars.Add (key, value);
						}
					//	LoadSave.savedData.Add (Time.time.ToString () + " : UDP IN : " + key); 
						this.stateVarsInvalidated = true;
				}

				/**
		 * Update the state for a set of key value pairs
		 * @param key[] an array of keys  
		 * @param args an array list of the value for the keys
		 */
				internal void UpdateStateVars (string[] keys, ArrayList args)
				{
						for (int i = 0; i < keys.Length && i < args.Count; ++i) {
								if (this.stateVars.ContainsKey (keys [i])) {
										this.stateVars [keys [i]] = args [i];
								} else {
										this.stateVars.Add (keys [i], args [i]);
								}
						}
				}

				/**
		 * Get the value for a specified key
		 * @param key the string for the key value pair  
		 * @return the value of the object for specified key
		 */
				public object GetStateVar (string key)
				{
						object result = null;
			
						if (this.stateVars.ContainsKey (key)) {
								result = this.stateVars [key];
						}
			
						return result;
				}

				//! Returns TRUE if the peer has more events, else returns FALSE
				public bool HasMoreEvents ()
				{
						return 0 != this.events.Count;
				}

				//! Returns TRUE if the peer has more EscEvents, else returns FALSE
				public EscEvent GetNextEvent ()
				{
						EscEvent evt = this.events [0];
						this.events.RemoveAt (0);
						return evt;
				}

				/**
		 * Appends EscEvent to the list of events
		 * @param evt the EscEvent that is added to the list  
		 */
				internal void AppendEvent (EscEvent evt)
				{
						this.events.Add (evt);
				}

				/**
		 * Appends an EscEvent with a username and message
		 * @param from the string of the user sending the message
		 * @param message the string message included with the EscEvent
		 */
				internal void AppendEvent (string from, string message)
				{
						// Added by kevin to null check
						if (System.String.IsNullOrEmpty (from) || System.String.IsNullOrEmpty (message))
								return;
						this.events.Add (new EscEvent (from, message));
				}

				/**
		 * Sets the remote end point for the Peer to communicate with
		 * @param ipAddress the string remote end point 
		 */
				internal void SetRemoteEndpoint (string ipAddress)
				{
						this.remoteIp = ipAddress;
						this.remotePort = StateListener.OSC_PORT_NUMBER;
			
						ShutdownTransmitter ();
						this.stateTransmitter = new OscTransmitter (this.remoteIp, this.remotePort);
				}

				//! Returns TRUE if the peer state has changed, else returns FALSE
				internal bool StateVarsAreStale ()
				{
						return stateVarsInvalidated;
				}

				// Added by Kevin! Returns true if state has changed. Else returns false
				public bool StateHasChanged ()
				{
						return stateVarsInvalidated;
				}

				//! Returns the string value of the remote ip address
				public string RemoteIp ()
				{
						return this.remoteIp;
				}

				//! Returns the int value of the remote port
				public int RemotePort ()
				{
						return this.remotePort;
				}

				protected void SetUsername (string name)
				{
						this.username = name;
				}

				//! Returns the string value of the Peer username
				public string Username ()
				{
						return this.username;
				}

				/**
		 * Sets the status of the Peer
		 * @param status the integer value of the peer status 
		 */		public void SetStatus (int status)
				{
						this.status = status;
				}

				//! Returns the int value of the Peer status
				public int Status ()
				{
						return this.status;
				}

				private bool ShutdownTransmitter ()
				{
						bool shutdown = (null != this.stateTransmitter);
			
						if (shutdown) {
								this.stateTransmitter.Close ();
								this.stateTransmitter = null;
						}
			
						return shutdown;
				}
		}

}