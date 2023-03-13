namespace Server.Network
{
	using Bootpd;
	using System;
	using System.Net;
	using System.Text;
	using static Bootpd.Functions;

	public sealed class TFTPPacket : PacketProvider
	{
		public TFTPPacket(int size, TFTPOPCodes opcode, IPEndPoint endpoint)
		{
			Data = new byte[size];
			Type = SocketType.TFTP;
			Length = Data.Length;
			Source = endpoint;

			if (OPCode == TFTPOPCodes.UNK)
				OPCode = opcode;
		}

		#region "generic methods"

		public override byte[] Data
		{
			get; set;
		}

		public override int Offset
		{
			get; set;
		}

		public override int Length
		{
			get; set;
		}

		public override SocketType Type
		{
			get; set;
		}

		public IPEndPoint Source { get; set; }

		public TFTPOPCodes OPCode
		{
			get
			{
				var bytes = new byte[sizeof(ushort)];
				CopyTo(Data, 0, bytes, 0, bytes.Length);
				Array.Reverse(bytes);

				return (TFTPOPCodes)BitConverter.ToUInt16(bytes, 0);
			}

			set
			{
				var bytes = BitConverter.GetBytes(Convert.ToUInt16(value));
				Array.Reverse(bytes);

				Offset += CopyTo(bytes, 0, Data, 0);
			}
		}

		public ushort Block
		{
			get
			{
				var bytes = new byte[sizeof(ushort)];
				CopyTo(Data, 2, bytes, 0, bytes.Length);
				Array.Reverse(bytes);

				return BitConverter.ToUInt16(bytes, 0);
			}

			set
			{
				var bytes = BitConverter.GetBytes(Convert.ToUInt16(value));
				Array.Reverse(bytes);

				Offset += CopyTo(bytes, 0, Data, 2);
			}
		}

		public string ErrorMessage
		{
			get
			{
				var count = Data.Length - 5;
				var messaage = Exts.BytesToString(Data, Encoding.ASCII, 4, count);

				return messaage;
			}

			set
			{
				var bytes = Exts.StringToByte(value, Encoding.ASCII);
				Offset += CopyTo(bytes, 0, Data, 4) + 1;
			}
		}

		public TFTPErrorCode ErrorCode
		{
			get
			{
				return (TFTPErrorCode)BitConverter.ToUInt16(Data, 2);
			}

			set
			{
				var errcode = BitConverter.GetBytes(Convert.ToUInt16(value));
				Array.Reverse(errcode);

				CopyTo(errcode, 0, Data, 2);
			}
		}

		public ushort MSFTWindow
		{
			get
			{
				return OPCode == TFTPOPCodes.ACK && Data.Length == 5 ?
					Convert.ToUInt16(Data[Data.Length - 1]) : Convert.ToUInt16(1);
			}
		}
		#endregion
	}
}
