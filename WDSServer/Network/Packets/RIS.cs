using System;
using System.Text;

using WDSServer.Providers;
using static WDSServer.Functions;

namespace WDSServer.Network
{
	sealed public class RISPacket : PacketProvider
	{
		RISOPCodes pktType;

		public RISPacket(byte[] data = null, int length = 0)
		{
			this.data = data;
			this.offset = 0;
			this.type = SocketType.BINL;

			if (length == 0)
				this.length = this.data.Length;
			else
				this.length = length;
		}


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
			get
			{
				return Convert.ToInt32(this.data[4]);
			}
			set
			{
				var bytes = BitConverter.GetBytes(value);
				CopyTo(ref bytes, 0, ref this.data, 4, bytes.Length);
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

		#region "RQU Functions"
		public byte Orign
		{
			get
			{
				return this.data[0];
			}
			set
			{
				this.data[0] = value;
			}

		}

		public string FileName
		{
			get
			{
				var offset = 36;
				var file = Encoding.ASCII.GetString(this.data, offset, (this.data.Length - offset) - 1);
				if (file.Length == 0)
					return Settings.OSC_DEFAULT_FILE.ToLowerInvariant();
				else
					return "{0}.osc".F(file.ToLowerInvariant());
			}
		}

		/// <summary>
		/// Returns the Type of the Packet (Example: RQU, NCQ, AUT ...)
		/// </summary>
		public string RequestType
		{
			get
			{
				var type = Encoding.ASCII.GetString(this.data, 1, 3);
				return type;
			}

			set
			{
				var type = Encoding.ASCII.GetBytes(value.ToUpper());
				Array.Copy(type, 0, this.data, 1, type.Length);
			}
		}

		public RISOPCodes OPCode
		{
			get
			{

				return this.pktType;
			}

			set
			{
				this.pktType = value;
			}
		}


		#endregion
	}
}
