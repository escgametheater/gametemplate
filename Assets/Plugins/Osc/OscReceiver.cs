using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace Osc
{
	public class OscReceiver
	{
		protected UdpClient udpClient;
		protected int localPort;

		public OscReceiver(int localPort)
		{
			this.localPort = localPort;
			Connect();
		}

		public void Connect()
		{
			if(this.udpClient != null) Close();
			this.udpClient = new UdpClient(this.localPort);
		}

		public void Close()
		{
			if (this.udpClient!=null) this.udpClient.Close();
			this.udpClient = null;
		}

		public OscPacket Receive()
		{
            try
            {
                IPEndPoint ip = null;
                byte[] bytes = this.udpClient.Receive(ref ip);
                if (bytes != null && bytes.Length > 0)
                    return OscPacket.Unpack(bytes);
							

            } catch (Exception e) { 
                Console.WriteLine(e.Message);
                return null;
            }

			return null;
		}
	}
}
