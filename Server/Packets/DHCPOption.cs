using System;
using System.Net;
using System.Text;

namespace Server.Network
{
	public class DHCPOption
	{
		public byte Option { get; private set; }
		public byte Length { get; private set; }
		public byte[] Data { get; private set; }

		public DHCPOption(byte option, byte[] data)
		{
			Option = option;
			Data = data;
			Length = Convert.ToByte(Data.Length);
		}

		public DHCPOption(byte option, IPAddress data)
		{
			Option = option;
			Data = data.GetAddressBytes();
			Length = Convert.ToByte(Data.Length);
		}

		public DHCPOption(byte option, ulong data)
		{
			Option = option;
			Data = BitConverter.GetBytes(data);
			Length = Convert.ToByte(Data.Length);
		}

		public DHCPOption(byte option, long data)
		{
			Option = option;
			Data = BitConverter.GetBytes(data);
			Length = Convert.ToByte(Data.Length);
		}


		public DHCPOption(byte option, int data)
		{
			Option = option;
			Data = BitConverter.GetBytes(data);
			Length = Convert.ToByte(Data.Length);
		}

		public DHCPOption(byte option, byte data)
		{
			Option = option;
			Data = BitConverter.GetBytes(Convert.ToByte(data));
			Length = sizeof(byte);
		}

		public DHCPOption(byte option)
		{
			if (option != 255)
				return;

			Option = option;
			Data = new byte[0];
			Length = 0;
		}

		public DHCPOption(byte option, DHCPMsgType data)
		{
			Option = option;
			Data = BitConverter.GetBytes(Convert.ToByte(data));
			Length = sizeof(byte);
		}

		public DHCPOption(byte option, ushort data)
		{
			Option = option;
			Data = BitConverter.GetBytes(data);
			Length = Convert.ToByte(Data.Length);
		}

		public DHCPOption(byte option, string data)
		{
			Option = option;
			Data = Encoding.ASCII.GetBytes(data);
			Length = Convert.ToByte(Data.Length);
		}
	}
}
