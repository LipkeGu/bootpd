using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using static Bootpd.Functions;
namespace Bootpd.Common.Network.Protocol.DHCP
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
			Length = Convert.ToByte(data.Length);
		}

		public DHCPOption(byte option, List<DHCPOption> list)
		{
			var length = 0;

			foreach (var item in list)
				length += item.Option != 255 ? 2 + item.Length : 1;

			var offset = 0;
			var block = new byte[length];

			foreach (var item in list)
			{
				offset += CopyTo(item.Option, block, offset);

				if (item.Option == 255)
					break;

				offset += CopyTo(item.Length, block, offset);
				offset += CopyTo(item.Data, 0, block, offset);
			}


			Option = option;
			Data = block;
			Length = Convert.ToByte(length);
		}

		public DHCPOption(byte option, Guid data)
		{
			Option = option;
			Data = new byte[17];

			Array.Copy(data.ToByteArray(), 0, Data, 1, Data.Length - 1);
			Length = Convert.ToByte(Data.Length);
		}
	}
}
