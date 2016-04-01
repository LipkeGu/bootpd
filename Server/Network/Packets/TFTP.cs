namespace bootpd
{
	using System;
	using System.Net;
	using System.Text;

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
				var bytes = new byte[sizeof(short)];
				Functions.CopyTo(ref this.data, 0, ref bytes, 0, bytes.Length);
				Array.Reverse(bytes);

				return (TFTPOPCodes)BitConverter.ToInt16(bytes, 0);
			}

			set
			{
				var bytes = BitConverter.GetBytes(Convert.ToInt16(value));
				Array.Reverse(bytes);

				Functions.CopyTo(ref bytes, 0, ref this.data, 0, bytes.Length);
				this.offset = 2;
			}
		}

		public short Block
		{
			get
			{
				var bytes = new byte[sizeof(short)];
				Functions.CopyTo(ref this.data, 2, ref bytes, 0, bytes.Length);
				Array.Reverse(bytes);

				return BitConverter.ToInt16(bytes, 0);
			}

			set
			{
				var bytes = BitConverter.GetBytes(Convert.ToInt16(value));
				Array.Reverse(bytes);

				Functions.CopyTo(ref bytes, 0, ref this.data, 2, bytes.Length);
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
				return (TFTPErrorCode)BitConverter.ToInt16(this.data, 2);
			}

			set
			{
				var errcode = BitConverter.GetBytes(Convert.ToInt16(value));
				Array.Reverse(errcode);

				Functions.CopyTo(ref errcode, 0, ref this.data, 2, errcode.Length);
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
