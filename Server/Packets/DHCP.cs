namespace Server.Network
{
	using Server.Extensions;
	using System;
	using System.Collections.Generic;
	using System.Net;
	using System.Text;
	using static Functions;

	public sealed class DHCPPacket : PacketProvider, IDisposable
	{
		#region "Generic Functions"
		public Dictionary<byte, DHCPOption> DHCPOptions;
		bool sNamefieldOverloaded = false;
		bool filefieldOverloaded = false;

		public DHCPPacket(byte[] data, SocketType type, bool parse = false)
		{
			Data = data;
			Offset = 0;
			Type = type;
			Length = data.Length;
			DHCPOptions = new Dictionary<byte, DHCPOption>();

			if (parse)
				ParseDHCPPacket();
		}

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

		#endregion

		#region "DHCP Packet Functions"

		public void CommitOptions()
		{
			var length = 0;

			// Calculate the Length of the TargetBuffer Options!
			foreach (var option in DHCPOptions.Values)
			{
				length += option.Option != 255 ? 2 + option.Length : 1;
			}

			var offset = 240;

			foreach (var option in DHCPOptions.Values)
			{
				offset += CopyTo(option.Option, Data, offset);

				if (option.Option == byte.MaxValue)
					break;

				offset += CopyTo(option.Length, Data, offset);

				if (option.Length != 1)
					offset += CopyTo(option.Data, 0, Data, offset);
				else
					offset += CopyTo(Convert.ToByte(option.Data[0]), Data, offset);

			}

			var end = Data.Length;

			for (var i = end; i > 240; i--)
				if (Data[i - 1] == byte.MaxValue)
				{
					end = i;
					break;
				}

			var newData = Data;

			Array.Resize(ref newData, end);

			Data = newData;
			Length = newData.Length;
			Offset = newData.Length;
		}

		private void ParseDHCPPacket()
		{
			for (var i = 240; i < Data.Length;)
			{
				var opt = Data[i];
				if (opt != byte.MaxValue)
				{
					var len = Data[i + 1];
					var data = new byte[len];

					CopyTo(Data, (i + 2), data, 0, len);
					AddOption(new DHCPOption(opt, data));

					i += 2 + len;
				}
				else
				{
					AddOption(new DHCPOption(opt));
					break;
				}
			}

			if (HasOption(52))
			{
				var ovld = Convert.ToByte(GetOption(52).Data);

				switch (ovld)
				{
					case 1:
						filefieldOverloaded = true;
						break;
					case 2:
						sNamefieldOverloaded = true;
						break;
					case 3:
						sNamefieldOverloaded = filefieldOverloaded = true;
						break;
					default:
						break;
				}
			}

		}

		public BootMessageType BootpType
		{
			get
			{
				return (BootMessageType)Convert.ToByte(Data[0]);
			}

			set
			{
				Data[0] = Convert.ToByte(value);
			}
		}

		public byte HardwareType
		{
			get
			{
				return Convert.ToByte(Data[1]);
			}

			set
			{
				Data[1] = Convert.ToByte(value);
			}
		}

		public byte MACAddresslength
		{
			get
			{
				return Convert.ToByte(Data[2]);
			}

			set
			{
				Data[2] = Convert.ToByte(value);
			}
		}

		public byte Hops
		{
			get
			{
				return Convert.ToByte(Data[3]);
			}

			set
			{
				Data[3] = Convert.ToByte(value);
			}
		}

		public uint TransactionID
		{
			get
			{
				return BitConverter.ToUInt32(Data, 4);
			}

			set
			{
				var bytes = BitConverter.GetBytes(value);
				CopyTo(bytes, 0, Data, 4, bytes.Length);
			}
		}

		public ushort Seconds
		{
			get
			{
				return BitConverter.ToUInt16(Data, 8);
			}

			set
			{
				var bytes = BitConverter.GetBytes(value);
				CopyTo(bytes, 0, Data, 8);
			}
		}

		public ushort BootpFlags
		{
			get
			{
				return BitConverter.ToUInt16(Data, 10);
			}

			set
			{
				var bytes = BitConverter.GetBytes(value);
				CopyTo(bytes, 0, Data, 10);
			}
		}

		public IPAddress ClientIP
		{
			get
			{
				var ipstring = BitConverter.ToString(Data, 12, 4).Replace("-", string.Empty);
				return IPAddress.Parse(ipstring);
			}

			set
			{
				var bytes = value.GetAddressBytes();
				CopyTo(bytes, 0, Data, 12);
			}
		}

		public IPAddress YourIP
		{
			get
			{
				var ipstring = BitConverter.ToString(Data, 16, 4).Replace("-", string.Empty);
				return IPAddress.Parse(ipstring);
			}

			set
			{
				var bytes = value.GetAddressBytes();
				CopyTo(bytes, 0, Data, 16);
			}
		}

		public IPAddress NextServer
		{
			get
			{
				var ipstring = BitConverter.ToString(Data, 20, 4).Replace("-", string.Empty);
				return IPAddress.Parse(ipstring);
			}

			set
			{
				var bytes = value.GetAddressBytes();
				CopyTo(bytes, 0, Data, 20);
			}
		}

		public IPAddress RelayServer
		{
			get
			{
				var ipstring = BitConverter.ToString(Data, 24, 4).Replace("-", string.Empty);
				return IPAddress.Parse(ipstring);
			}

			set
			{
				var bytes = value.GetAddressBytes();
				CopyTo(bytes, 0, Data, 24);
			}
		}

		/// <summary>
		/// Sets or gets the CLient HArdware address (Also sets or gets the Address from Option 61)
		/// </summary>
		public byte[] MacAddress
		{
			get
			{
				if (DHCPOptions.ContainsKey(61))
					return DHCPOptions[61].Data;
				else
				{
					var mac = new byte[MACAddresslength];
					CopyTo(Data, 28, mac, 0, mac.Length);
					return mac;
				}
			}
			set
			{
				var mac = new byte[16];

				CopyTo(value, 0, mac, 0, value.Length);
				CopyTo(mac, 0, Data, 28);

				AddOption(new DHCPOption(61, mac));
			}
		}

		public string ServerName
		{
			get
			{
				return sNamefieldOverloaded ? BitConverter.ToString(Data, 44, 64) :
					Encoding.ASCII.GetString(GetOption(66).Data);
			}

			set
			{
				if (!sNamefieldOverloaded)
				{
					Array.Clear(Data, 44, 64);

					var bytes = new byte[64];
					bytes = Exts.StringToByte(value, Encoding.ASCII);
					CopyTo(bytes, 0, Data, 44);
				}
				else
					AddOption(new DHCPOption(66, value));
			}
		}

		public string Bootfile
		{
			get
			{
				return BitConverter.ToString(Data, 108, 128);
			}

			set
			{
				var bytes = new byte[128];

				Array.Clear(Data, 108, bytes.Length);
				bytes = Exts.StringToByte(value, Encoding.ASCII);
				CopyTo(bytes, 0, Data, 108);
			}
		}

		public uint Cookie
		{
			get
			{
				return BitConverter.ToUInt32(Data, 236);
			}

			set
			{
				var bytes = BitConverter.GetBytes(value);
				CopyTo(bytes, 0, Data, 236);
			}
		}

		public void Dispose()
		{
			Array.Clear(Data, 0, Data.Length);
		}


		public void AddOption(DHCPOption dhcpoption)
		{
			if (!DHCPOptions.ContainsKey(dhcpoption.Option))
				DHCPOptions.Add(dhcpoption.Option, dhcpoption);
			else
				DHCPOptions[dhcpoption.Option] = dhcpoption;
		}

		public bool HasOption(byte opt)
			=> DHCPOptions.ContainsKey(opt);

		public List<DHCPOption> GetEncOptions(byte opt)
		{
			var dict = new List<DHCPOption>();

			var optionData = GetOption(opt).Data;

			for (var i = 0; i < optionData.Length;)
			{
				var o = optionData[i];

				if (o != 255)
				{
					var len = optionData[i + 1];
					var data = new byte[len];

					CopyTo(optionData, (i + 2), data, 0, len);
					dict.Add(new DHCPOption(o, data));

					i += 2 + len;
				}
				else
				{
					dict.Add(new DHCPOption(o));
					break;
				}
			}

			return dict;
		}

		public DHCPOption GetOption(byte opt)
			=> DHCPOptions[opt];
		#endregion
	}
}
