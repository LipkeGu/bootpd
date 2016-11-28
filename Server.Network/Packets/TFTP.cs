namespace Server.Network
{
	using System;
	using System.Net;
	using System.Text;
	using Extensions;
	using static Extensions.Functions;

	public sealed class TFTPPacket : PacketProvider
	{
		IPEndPoint src;

		public TFTPPacket(int size, TFTPOPCodes opcode, IPEndPoint endpoint)
		{
			this.data = new byte[size];
			this.type = SocketType.TFTP;
			this.length = this.data.Length;
			this.src = endpoint;

			if (this.OPCode == TFTPOPCodes.UNK)
				this.OPCode = opcode;
		}

		#region "generic methods"
		public override byte[] Add
		{
			set
			{
				this.offset += CopyTo(ref value, 0, ref this.data, this.data.Length, value.Length);
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
				var bytes = new byte[sizeof(ushort)];
				CopyTo(ref this.data, 0, ref bytes, 0, bytes.Length);
				Array.Reverse(bytes);

				return (TFTPOPCodes)BitConverter.ToUInt16(bytes, 0);
			}

			set
			{
				var bytes = BitConverter.GetBytes(Convert.ToUInt16(value));
				Array.Reverse(bytes);

				this.offset += CopyTo(ref bytes, 0, ref this.data, 0, bytes.Length);
			}
		}

		public ushort Block
		{
			get
			{
				var bytes = new byte[sizeof(ushort)];
				CopyTo(ref this.data, 2, ref bytes, 0, bytes.Length);
				Array.Reverse(bytes);

				return BitConverter.ToUInt16(bytes, 0);
			}

			set
			{
				var bytes = BitConverter.GetBytes(Convert.ToUInt16(value));
				Array.Reverse(bytes);

				this.offset += CopyTo(ref bytes, 0, ref this.data, 2, bytes.Length);
			}
		}

		public string ErrorMessage
		{
			get
			{
				var count = this.data.Length - 5;
				var messaage = Exts.BytesToString(this.Data, Encoding.ASCII, 4, count);

				return messaage;
			}

			set
			{
				var bytes = Exts.StringToByte(value, Encoding.ASCII);
				this.offset += CopyTo(ref bytes, 0, ref this.data, 4, bytes.Length) + 1;
			}
		}

		public TFTPErrorCode ErrorCode
		{
			get
			{
				return (TFTPErrorCode)BitConverter.ToUInt16(this.data, 2);
			}

			set
			{
				var errcode = BitConverter.GetBytes(Convert.ToUInt16(value));
				Array.Reverse(errcode);

				CopyTo(ref errcode, 0, ref this.data, 2, errcode.Length);
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

		public ushort MSFTWindow
		{
			get
			{
				return this.OPCode == TFTPOPCodes.ACK && this.data.Length == 5 ? 
					Convert.ToUInt16(this.data[this.data.Length - 1]) : Convert.ToUInt16(1);
			}
		}
		#endregion
	}
}
