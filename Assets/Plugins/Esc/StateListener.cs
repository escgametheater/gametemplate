using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Osc;

namespace Esc
{

	/**
	 * The StateListener is used to receive state information coming  
	 * from other peers. It is used by the ClientConnection and 
	 * ServerConnection objects to open a socket and listen on it.
	 * 
	 * @see Esc.ServerConnection
	 * @see Esc.ClientConnection
	 */
	internal class StateListener : System.Object
	{

		//! Public integer constant for the OSC Port Number
		public const int OSC_PORT_NUMBER = 3222;

		//! Create an OSC delegate to update a peer object
		public delegate void OscDelegateMessage (string[] keys,ArrayList args);

		public event OscDelegateMessage HandleMessage;

		// thread safe Producer/Consumer Queue for incoming messages
		private List<OscMessage> processQueue = new List<OscMessage> ();

		// Private members
		private bool connected = false;
		private int port;
		private OscReceiver receiver;
		private Thread thread;

		//! Constructor that takes an integer Port number
		public StateListener (int port = OSC_PORT_NUMBER)
		{
			this.port = port;
		}

		/**
		 * Performs thread-safe access to internal queue of messages
		 * in order to process them in sequence. For each message,
		 * the HandleMessage callback is invoked.
		 */
		public void Update ()
		{
			//processMessages has to be called on the main thread
			//so we used a shared proccessQueue full of OSC Messages
			lock (processQueue) {
				foreach (OscMessage message in processQueue) {
					ProcessMessage (message);
				}
				processQueue.Clear ();
			}
		}

		/**
		 * Spawns thread to listen for messages using the port number
		 * denoted by the constructor.
		 *
		 * @see Esc.StateListener.Listen
		 */
		public void Connect ()
		{
			try {
				receiver = new OscReceiver (port);
				thread = new Thread (new ThreadStart (Listen));
				thread.Start ();
				connected = true;

			} catch (Exception e) {
				Console.WriteLine ("failed to connect to port " + port);
				Console.WriteLine (e.Message);
			}
		}

		//! Disconnect the state listener
		public void Disconnect ()
		{
			if (receiver != null)
				receiver.Close ();

			receiver = null;
			connected = false;
		}

		/**
		 * Listen is spawned by a thread in the Connect() method. It remains 
		 * in a loop receiving data as the connection is alive.
		 *
		 * @see Esc.StateListener.Connect
		 */
		private void Listen ()
		{
			while (connected) {
				try {
					OscPacket packet = receiver.Receive ();
					if (null != packet) {
						//	UnityMainThreadDispatcher.Instance ().Enqueue (ExsecuteOnMainThread_TimeCheck ());
						lock (processQueue) {
							if (packet.IsBundle ()) {
								ArrayList messages = packet.Values;
								for (int i = 0; i < messages.Count; i++) {
									processQueue.Add ((OscMessage)messages [i]);
								}
							} else {
								processQueue.Add ((OscMessage)packet);
							}
						}
					}
				} catch (Exception e) { 
					Debug.LogError (e.Message);
				}
			}
		}
		// Added By Kevin!
		//				public IEnumerator ExsecuteOnMainThread_TimeCheck ()
		//				{
		//						LoadSave.AddData (" : UDP IN :  ");
		//						yield return null;
		//				}

		/**
		 * Invoked by the Update() method. Consumes the OSC message, creating
		 * a set of key/value pairs to be passed on to the callback method.
		 */
		private void ProcessMessage (OscMessage message)
		{
			ArrayList args = message.Values;

			string[] slashDelimiter = new string[] { "/" };
			string[] keys = message.Address.Split (slashDelimiter, StringSplitOptions.RemoveEmptyEntries);

			if (null != HandleMessage) {
				HandleMessage (keys, args);
			}
		}

		//! Returns TRUE if the state listener is connected, else returns FALSE
		public bool IsConnected ()
		{
			return connected;
		}
	}

}