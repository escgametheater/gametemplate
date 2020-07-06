using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace Osc
{
	public class OscTransmitter
	{
		protected bool connected;
		protected UdpClient udpClient;
		protected string remoteHost;
		protected int remotePort;

		public OscTransmitter(string remoteHost, int remotePort)
		{
			this.connected = false;
			this.remoteHost = remoteHost;
			this.remotePort = remotePort;
			Connect();
		}

		public void Connect()
		{
			if(this.udpClient != null) Close();
			this.udpClient = new UdpClient(this.remoteHost, this.remotePort);
			this.connected = true;
		}

		public void Close()
		{
			this.udpClient.Close();
			this.udpClient = null;
			this.connected = false;
		}
		
		public bool IsConnected() 
		{
			return this.connected;
		}

		public int Send(OscPacket packet)
		{
			int byteNum = 0;
			byte[] data = packet.BinaryData;
			try 
			{
				byteNum = this.udpClient.Send(data, data.Length);

			}
			catch (Exception e)
			{
				Console.Error.WriteLine(e.Message);
				Console.Error.WriteLine(e.StackTrace);
			}

			return byteNum;
		}
	}
}
