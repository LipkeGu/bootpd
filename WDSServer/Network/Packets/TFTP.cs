﻿using System;
using System.Text;
using WDSServer.Providers;
using static WDSServer.Functions;

namespace WDSServer.Network
{
	sealed public class TFTPPacket : PacketProvider
	{
		public TFTPPacket(int size, TFTPOPCodes opcode)
		{

			this.data = new byte[size];
			this.type = SocketType.TFTP;

			if (this.OPCode == 0)
				this.OPCode = (sbyte)opcode;
		}

		#region "generic methods"
		public override byte[] Add
		{
			set
			{
				CopyTo(ref value, 0, ref this.data, this.data.Length, value.Length);
			}
		}

		public override byte[] Data
		{
			get
			{
				return this.data;
			}

			set
			{
				this.data = value;
			}
		}

		public override int Length
		{
			get { return this.data.Length; }
			set { }
		}
		public sbyte OPCode
		{
			get
			{
				return (sbyte)((this.data[0] * 256) + this.data[1]);
			}

			set
			{
				this.data[0] = (byte)((value & 0xFF00) / 256);
				this.data[1] = (byte)(value & 0x00FF);
				this.offset = 2;
			}
		}

		public int Block
		{
			get
			{
				return ((this.data[2] * 256) + this.data[3]);
			}

			set
			{
				this.data[2] = (byte)((value & 0xFF00) / 256);
				this.data[3] = (byte)(value & 0x00FF);
				this.offset += 2;
			}
		}

		public string ErrorMessage
		{
			get
			{
				if (!IsTFTPOPCode((sbyte)TFTPOPCodes.ERR, this.Data))
					throw new InvalidOperationException("This Packet does not contains Error Messages");

				var count = (this.data.Length - 5);
				var Messaage = Encoding.ASCII.GetString(this.Data, 4, count);

				return Messaage;
			}
			set
			{
				if (!IsTFTPOPCode((sbyte)TFTPOPCodes.ERR, this.Data))
					throw new InvalidOperationException("This Packet does not contains Error Messages");

				var bytes = Encoding.ASCII.GetBytes(value.ToCharArray());
				CopyTo(ref bytes, 0, ref this.data, 4, bytes.Length);

				this.offset += (bytes.Length + 1);
			}
		}

		public TFTPErrorCode ErrorCode
		{
			get
			{
				if (!IsTFTPOPCode((sbyte)TFTPOPCodes.ERR, this.Data))
					throw new InvalidOperationException("This Packet does not contains Error Code");

				return (TFTPErrorCode)Convert.ToSByte(this.data[3]);
			}
			set
			{
				if (!IsTFTPOPCode((sbyte)TFTPOPCodes.ERR, this.Data))
					throw new InvalidOperationException("This Packet does not contains Error Code");
				var errcode = Convert.ToByte((sbyte)value);

				this.data[2] = (byte)((errcode & 0xFF00) / 256);
				this.data[3] = (byte)(errcode & 0x00FF);
				this.offset = 4;
			}
		}

		public override SocketType Type
		{
			get
			{
				return this.type;
			}
			set
			{
				this.type = value;
			}
		}

		public override int Offset
		{
			get
			{
				return this.offset;
			}
			set
			{
				this.offset = value;
			}
		}

		~TFTPPacket()
		{
			Array.Clear(this.data, 0, this.data.Length);
		}
		#endregion
	}
}