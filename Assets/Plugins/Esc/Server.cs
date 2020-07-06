using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Osc;

namespace Esc
{

	/**
	 * The Server class is used to represent the game engine network
	 * endpoint. It can be used to send and receive game events to clients
	 * as well as broadcasting state information to the connected clients.
	 * 
	 * @see Esc.Peer
	 */
	public class Server : Peer
	{

		//! Class constructor. It requires a string to instantiate.
		public Server (string name) : base (name)
		{
		}

		//! Class destructor
		~Server ()
		{
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
		internal bool PropagateStateVars ()
		{
			bool propagated = false;

			if (this.stateTransmitter.IsConnected () && this.stateVars.Count > 0) {
				string address = "";
				OscMessage message = new OscMessage ("");
				foreach (KeyValuePair<string,object> pair in this.stateVars) {
					address += pair.Key + "/";
					message.Append (pair.Value);
				}
				message.Address = address;
				this.stateTransmitter.Send (message);
				//	UnityMainThreadDispatcher.Instance ().Enqueue (ExsecuteOnMainThread_TimeCheck ());
				propagated = true;
			}

			this.stateVarsInvalidated = false;

			return propagated;
		}
		// Added By Kevin!
		//				public IEnumerator ExsecuteOnMainThread_TimeCheck ()
		//				{
		//						LoadSave.AddData (" : UDP OUT SERVER :  ");
		//						yield return null;
		//				}
	}

}