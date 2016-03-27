namespace WDSServer.Network
{
	using System;
	using System.Net;
	using System.Text;
	using WDSServer.Providers;

	public sealed class TFTPPacket : PacketProvider
	{
		IPEndPoint src;

		public TFTPPacket(int size, TFTPOPCodes opcode, IPEndPoint endpoint)
		{
			this.data = new byte[size];
			this.type = SocketType.TFTP;
			this.length = this.data.Length;
			this.src = endpoint;

			if (this.OPCode == 0)
				this.OPCode = opcode;
		}

		#region "generic methods"
		public override byte[] Add
		{
			set
			{
				Functions.CopyTo(ref value, 0, ref this.data, this.data.Length, value.Length);
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

		public IPEndPoint Source
		{
			get
			{
				return this.src;
			}

			set
			{
				this.src = value;
			}
		}

		public override int Length
		{
			get
			{
				return this.length;
			}

			set
			{
				this.length = value;
			}
		}

		public TFTPOPCodes OPCode
		{
			get
			{
				return (TFTPOPCodes)Convert.ToSByte((this.data[0] * 256) + this.data[1]);
			}

			set
			{
				var bytes = Convert.ToByte((sbyte)value);

				this.data[0] = (byte)((bytes & 0xFF00) / 256);
				this.data[1] = (byte)(bytes & 0x00FF);
				this.offset = 2;
			}
		}

		public int Block
		{
			get
			{
				return (this.data[2] * 256) + this.data[3];
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
				var count = this.data.Length - 5;
				var messaage = Encoding.ASCII.GetString(this.Data, 4, count);

				return messaage;
			}

			set
			{
				var bytes = Exts.StringToByte(value);
				Functions.CopyTo(ref bytes, 0, ref this.data, 4, bytes.Length);

				this.offset += bytes.Length + 1;
			}
		}

		public TFTPErrorCode ErrorCode
		{
			get
			{
				return (TFTPErrorCode)Convert.ToSByte(this.data[3]);
			}

			set
			{
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
		#endregion
	}
}
