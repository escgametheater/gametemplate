using System;
using System.Collections;

namespace Osc
{
	public class OscBundle : OscPacket
	{
		protected const string BUNDLE = "#bundle";
		private long timestamp = 0;
		
		public OscBundle(long ts)
		{
			this.address = BUNDLE;
			this.timestamp = ts;
		}

		public OscBundle()
		{
			this.address = BUNDLE;
			this.timestamp = 0;
		}

		override protected void pack()
		{
			ArrayList data = new ArrayList();

			addBytes(data, packString(this.Address));
			padNull(data);
			addBytes(data, packLong(0)); // TODO
			
			foreach(object value in this.Values)
			{
				if(value is OscPacket)
				{
					byte[] bs = ((OscPacket)value).BinaryData;
					addBytes(data, packInt(bs.Length));
					addBytes(data, bs);
				}
				else 
				{
					// TODO
				}
			}
			
			this.binaryData = (byte[])data.ToArray(typeof(byte));
		}

		public static new OscBundle Unpack(byte[] bytes, ref int start, int end)
		{

			string address = unpackString(bytes, ref start);
			//Console.WriteLine("bundle: " + address);
			if(!address.Equals(BUNDLE)) return null; // TODO

			long timestamp = unpackLong(bytes, ref start);
			OscBundle bundle = new OscBundle(timestamp);
			
			while(start < end)
			{
				int length = unpackInt(bytes, ref start);
				int sub_end = start + length;
				//Console.WriteLine(bytes.Length +" "+ start+" "+length+" "+sub_end);
				bundle.Append(OscPacket.Unpack(bytes, ref start, sub_end));

			}

			return bundle;
		}

		public long getTimeStamp() {
			return timestamp;
		}

		override public void Append(object value)
		{
			if( value is OscPacket) 
			{
				values.Add(value);
			}
			else 
			{
				// TODO: exception
			}
		}

		override public bool IsBundle() { return true; }
	}
}

