using System;
using System.Runtime.InteropServices;
using System.Collections;

public class iosBtyPluginHandler
{

		[DllImport("__Internal")]
		private static extern IntPtr BtyState ();

		[DllImport("__Internal")]
		public static extern int BtyLevel ();

		public static string btyState {
				get {
						return Marshal.PtrToStringAuto (BtyState ());
				}

		}
}
