using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Osc;

namespace Esc {

	/**
	 * The Client class is used to represent controllers that are connected 
	 * to the game server. It can be used to send and receive game events
	 * as well as transmit state information to the server.
	 * 
	 * @see Esc.Peer
	 */
	public class Client : Peer {

		private bool registered = false;
		private bool ready = false;

		//! Constructor initializes base Peer class with a username
		public Client ( string name ) : base( name )
		{
		}

		//! Destructor
		~Client ()
		{
		}

		/**
		 * The Register method creates an instance of the OscTransmitter used for 
		 * dispatching state changes. This method is invoked from the ServerConnection
		 * object when a Client has submitted its own local IP address to the server.
		 * Also assigns local state variable that indicates that the client is ready for 
		 * an initialization message.
		 *
		 * @param ipAddress local network address (using IP4)
		 */
		internal void Register( string ipAddress )
		{
			SetRemoteEndpoint( ipAddress );
			this.registered = true;
		}

		//! Returns TRUE if the client has completed its registration process with the server, FALSE otherwise
		public bool IsRegistered ()
		{
			return this.registered;
		}
		
		//! Returns TRUE if the client has completed its initialization process with the game, FALSE otherwise
		public bool IsReady ()
		{
			return this.ready;
		}

		//! Used internally to signal that a client must re-register.
		internal void UnRegister()
		{
			this.registered = false;
		}
		
		//! Updates the local client 'ready' state which should only be invoked an instance has successfully processed the init event
		internal void SetReadiness ( bool ready )
		{
			this.ready = ready;
		}

		/**
		 * Creates an OSC formatted message using the current key/value pairs within the
		 * local Peer.stateVars dictionary. Each key is represented as a component of the
		 * string address (separated by forward slashes). Each value is appended to the 
		 * OSC packet using the appropriate bit packing method. The index parameter is used
		 * to help the remote ServerConnection object match the OSC message with its locally 
		 * stored list of Client instances.
		 *
		 * @param index the integer sent to the client upon registration
		 * @return TRUE if stateTransmitter was properly initialized, FALSE otherwise
		 */
		internal bool PropagateStateVars( int index )
		{
			bool propagated = false;
			if (this.stateVarsInvalidated && null != this.stateTransmitter && this.stateTransmitter.IsConnected() && this.stateVars.Count > 0)
			{
				string address = "";
				OscMessage message = new OscMessage("");
				message.Append(index);
				foreach( KeyValuePair<string,object> pair in this.stateVars )
				{
				    address += pair.Key + "/";
					message.Append(pair.Value);
				}
				message.Address = address;
				this.stateTransmitter.Send(message);
				//UnityMainThreadDispatcher.Instance ().Enqueue (ExsecuteOnMainThread_TimeCheck ());
				propagated = true;
			}
			
			this.stateVarsInvalidated = false;
			
			return propagated;
		}
				// Added By Kevin!
//				public IEnumerator ExsecuteOnMainThread_TimeCheck(){
//					//	LoadSave.AddData (" : UDP OUT CLIENT :  ");
//						yield return null;
//				}

	}
}