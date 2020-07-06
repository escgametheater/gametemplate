using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Esc {

	/**
	 * The EscEvent class is used to encapsulate game event messages that
	 * are received from other peers or being sent out to other peers.
	 * Each EscEvent instance comes with a name that identifies what it
	 * is and attributes stored as a dictionary of key value pairs.
	 * 
	 */
	public class EscEvent : System.Object {

		//! String name that identifies the EscEvent object  
		public string name;

		//! Dictionary that stores key value pairs that are part of the EscEvent object
		public Dictionary<string, string> attributes;

		/**
		 * Constructor of an EscEvent with a string name and dictionary of key value pairs
		 * @param name set the string name 
		 * @param keyValues set the string key pairs with a comma delimiter
		 */
		public EscEvent (string name, string keyValues)
		{
			this.name = name;
			this.attributes = new Dictionary<string, string>();
			string[] commaDelimiter = new string[] {","};
			string[] equalsDelimiter = new string[] {"="};
			string[] vars = keyValues.Split(commaDelimiter, StringSplitOptions.RemoveEmptyEntries);
			foreach ( string pair in vars ) {
				string[] keyValuePair = pair.Split(equalsDelimiter, StringSplitOptions.None);
				if( keyValuePair.Length >= 2 ) {
					this.attributes.Add( keyValuePair[0], keyValuePair[1] );
				}
			}
		}

		//! Constructor
		public EscEvent (string name = "", Dictionary<string, string> attrs = null)
		{
			this.name = name;
			this.attributes = attrs;
		}

		//! Destructor
		~EscEvent ()
		{
			this.attributes = null;
		}

		//! Modify the inherited method ToString to return a key value string format
		public override string ToString ()
		{
			string output = this.name;
			output += ":";

			if (null != this.attributes) {
				int count = 0;
				foreach ( KeyValuePair<string,string> pair in this.attributes ) {
					output += pair.Key;
					output += "=";
					output += pair.Value;
					
					count++;
					if (count < this.attributes.Count) {
						output += ",";
					}
				}
			}

			return output;
		}

		/**
		 * Converts a string to an EscEvent
		 * @param serializedEvent a string message that will be converted 
		 * @return EscEvent from a string 
		 */
		public static EscEvent FromString (string serializedEvent)
		{
			EscEvent output;
			string[] colonDelimiter = new string[] {":"};
			string[] vars = serializedEvent.Split(colonDelimiter, StringSplitOptions.None);

			if ( 2 == vars.Length ) {
				output = new EscEvent( vars[0], vars[1] );
			}
			else if ( 1 == vars.Length ) {
				output = new EscEvent( vars[0] );
			}
			else 
				output = new EscEvent();

			return output;
		}
	}

}