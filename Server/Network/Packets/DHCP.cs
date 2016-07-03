namespace bootpd
{
	using System;
	using System.Net;
	using System.Text;
	public sealed class DHCPPacket : PacketProvider, IDisposable
	{
		#region "Generic Functions"

		public DHCPPacket(byte[] data)
		{
			this.data = data;
			this.offset = 0;
			this.type = SocketType.DHCP;
			this.length = this.data.Length;
		}

		public override byte[] Add
		{
			set
			{
				Functions.CopyTo(ref value, 0, ref this.data, this.data.Length, value.Length);
				this.offset += value.Length;
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

		public override int Length
		{
			get
			{
				return this.Offset;
			}

			set
			{
				this.Offset = value;
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

		#endregion

		#region "DHCP Packet Functions"
		public BootMessageType BootpType
		{
			get
			{
				return (BootMessageType)Convert.ToSByte(this.data[0]);
			}

			set
			{
				this.data[0] = Convert.ToByte(value);
			}
		}

		public sbyte HardwareType
		{
			get
			{
				return Convert.ToSByte(this.data[1]);
			}

			set
			{
				this.data[1] = Convert.ToByte(value);
			}
		}

		public sbyte MACAddresslength
		{
			get
			{
				return Convert.ToSByte(this.data[2]);
			}

			set
			{
				this.data[2] = Convert.ToByte(value);
			}
		}

		public sbyte Hops
		{
			get
			{
				return Convert.ToSByte(this.data[3]);
			}

			set
			{
				this.data[3] = Convert.ToByte(value);
			}
		}

		public uint TransactionID
		{
			get
			{
				return BitConverter.ToUInt32(this.data, 4);
			}

			set
			{
				var bytes = BitConverter.GetBytes(value);
				Functions.CopyTo(ref bytes, 0, ref this.data, 4, bytes.Length);
			}
		}

		public ushort Seconds
		{
			get
			{
				return BitConverter.ToUInt16(this.data, 8);
			}

			set
			{
				var bytes = BitConverter.GetBytes(value);
				Functions.CopyTo(ref bytes, 0, ref this.data, 8, bytes.Length);
			}
		}

		public DHCPMsgType MessageType
		{
			get
			{
				return (DHCPMsgType)Convert.ToInt32(this.data[242]);
			}

			set
			{
				this.data[240] = Convert.ToByte(DHCPOptionEnum.DHCPMessageType);
				this.data[241] = 1;
				this.data[242] = Convert.ToByte(value);
			}
		}

		public ushort BootpFlags
		{
			get
			{
				return BitConverter.ToUInt16(this.data, 10);
			}

			set
			{
				var bytes = BitConverter.GetBytes(value);
				Functions.CopyTo(ref bytes, 0, ref this.data, 10, bytes.Length);
			}
		}

		public IPAddress ClientIP
		{
			get
			{
				var ipstring = BitConverter.ToString(this.data, 12, 4).Replace("-", string.Empty);
				return IPAddress.Parse(ipstring);
			}

			set
			{
				var bytes = value.GetAddressBytes();
				Functions.CopyTo(ref bytes, 0, ref this.data, 12, bytes.Length);
			}
		}

		public IPAddress YourIP
		{
			get
			{
				var ipstring = BitConverter.ToString(this.data, 16, 4).Replace("-", string.Empty);
				return IPAddress.Parse(ipstring);
			}

			set
			{
				var bytes = value.GetAddressBytes();
				Functions.CopyTo(ref bytes, 0, ref this.data, 16, bytes.Length);
			}
		}

		public IPAddress NextServer
		{
			get
			{
				var ipstring = BitConverter.ToString(this.data, 20, 4).Replace("-", string.Empty);
				return IPAddress.Parse(ipstring);
			}

			set
			{
				var bytes = value.GetAddressBytes();
				Functions.CopyTo(ref bytes, 0, ref this.data, 20, bytes.Length);
			}
		}

		public IPAddress RelayServer
		{
			get
			{
				var ipstring = BitConverter.ToString(this.data, 24, 4).Replace("-", string.Empty);
				return IPAddress.Parse(ipstring);
			}

			set
			{
				var bytes = value.GetAddressBytes();
				Functions.CopyTo(ref bytes, 0, ref this.data, 24, bytes.Length);
			}
		}

		public byte[] MacAddress
		{
			get
			{
				var mac = new byte[this.data[2]];
				Functions.CopyTo(ref this.data, 28, ref mac, 0, mac.Length);
				return mac;
			}

			set
			{
				var mac = new byte[16];

				Functions.CopyTo(ref value, 0, ref mac, 0, value.Length);
				Functions.CopyTo(ref mac, 0, ref this.data, 28, mac.Length);
			}
		}

		public string ServerName
		{
			get
			{
				return BitConverter.ToString(this.data, 44, 64);
			}

			set
			{
				Array.Clear(this.data, 44, 64);

				var bytes = new byte[64];
				bytes = Exts.StringToByte(value, Encoding.ASCII);
				Functions.CopyTo(ref bytes, 0, ref this.data, 44, bytes.Length);
			}
		}

		public string Bootfile
		{
			get
			{
				return BitConverter.ToString(this.data, 108, 128);
			}

			set
			{
				var bytes = new byte[128];

				Array.Clear(this.data, 108, bytes.Length);
				bytes = Exts.StringToByte(value, Encoding.ASCII);
				Functions.CopyTo(ref bytes, 0, ref this.data, 108, bytes.Length);
			}
		}

		public uint Cookie
		{
			get
			{
				return BitConverter.ToUInt32(this.data, 236);
			}

			set
			{
				var bytes = BitConverter.GetBytes(value);
				Functions.CopyTo(ref bytes, 0, ref this.data, 236, bytes.Length);
			}
		}

		public void Dispose()
		{
		}
		#endregion
	}
}
