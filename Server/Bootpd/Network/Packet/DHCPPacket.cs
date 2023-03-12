using Bootpd.Common.Network.Protocol.DHCP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using static bootpd.Functions;

namespace Bootpd.Network.Packet
{
	public class DHCPPacket : BasePacket
	{
		Dictionary<byte, DHCPOption> DHCPOptions;

		public DHCPMsgType MessageType { get; private set; }
		bool sNamefieldOverloaded = false;
		bool filefieldOverloaded = false;

		public BootpMsgType BootpMsgType
		{
			get
			{
				Buffer.Position = 0;
				return (BootpMsgType)Buffer.ReadByte();
			}

			set
			{
				Buffer.Position = 0;
				Buffer.WriteByte(Convert.ToByte(value));
			}
		}

		public BootpHWType BootpHWType
		{
			get
			{
				Buffer.Position = 1;
				return (BootpHWType)Buffer.ReadByte();
			}

			set
			{
				Buffer.Position = 1;
				Buffer.WriteByte(Convert.ToByte(value));
			}
		}

		public byte BootpHWLen
		{
			get
			{
				Buffer.Position = 2;
				return (byte)Buffer.ReadByte();
			}

			set
			{
				Buffer.Position = 2;
				Buffer.WriteByte(value);
			}
		}

		public byte BootpHops
		{
			get
			{
				Buffer.Position = 3;
				return (byte)Buffer.ReadByte();
			}

			set
			{
				Buffer.Position = 3;
				Buffer.WriteByte(value);
			}
		}

		public uint TransactionId
		{
			get
			{
				Buffer.Position = 4;
				return Buffer.ToUint32();
			}

			set
			{
				var buffer = value.GetBytes();
				Buffer.Position = 4;
				Buffer.Write(buffer);
			}
		}

		public ushort Seconds
		{
			get
			{
				Buffer.Position = 8;
				return Buffer.ToUint16().LE16();
			}

			set
			{
				var buffer = value.LE16().GetBytes();
				Buffer.Position = 8;
				Buffer.Write(buffer);
			}
		}

		public BootpFlags Flags
		{
			get
			{
				Buffer.Position = 10;
				return (BootpFlags)Buffer.ToUint16();
			}

			set
			{
				var buffer = ((ushort)value).GetBytes();
				Buffer.Position = 10;
				Buffer.Write(buffer);
			}
		}

		public IPAddress ClientIP
		{
			get
			{
				Buffer.Position = 12;
				return Buffer.ToIPAddress();
			}

			set
			{
				Buffer.Position = 12;
				Buffer.Write(value);
			}
		}

		public IPAddress YourIP
		{
			get
			{
				Buffer.Position = 16;
				return Buffer.ToIPAddress();
			}

			set
			{
				Buffer.Position = 16;
				Buffer.Write(value);
			}
		}

		public IPAddress ServerIP
		{
			get
			{
				Buffer.Position = 20;
				return Buffer.ToIPAddress();
			}

			set
			{
				Buffer.Position = 20;
				Buffer.Write(value);
			}
		}

		public IPAddress RelayIP
		{
			get
			{
				Buffer.Position = 24;
				return Buffer.ToIPAddress();
			}

			set
			{
				Buffer.Position = 24;
				Buffer.Write(value);
			}
		}

		public byte[] HWAddr
		{
			get
			{
				Buffer.Position = 28;
				return Buffer.Read(BootpHWLen);
			}

			set
			{
				Buffer.Position = 28;
				Buffer.Write(value);
			}
		}

		public string ServerName
		{
			get
			{
				Buffer.Position = 44;
				return Buffer.ReadString(BootpHWLen);
			}

			set
			{
				Buffer.Position = 44;
				Buffer.Write(value, 64);
			}
		}

		public string Bootfile
		{
			get
			{
				Buffer.Position = 108;
				return Buffer.ReadString(BootpHWLen);
			}

			set
			{
				Buffer.Position = 108;
				Buffer.Write(value, 128);
			}
		}

		public byte[] MagicCookie
		{
			get
			{

				Buffer.Position = 236;
				return Buffer.Read(4);
			}

			set
			{
				Buffer.Position = 236;
				Buffer.Write(value);
			}
		}

		public DHCPPacket(byte[] data) : base(data)
		{
			DHCPOptions = new Dictionary<byte, DHCPOption>();
			if (HasOption(53))
				MessageType = (DHCPMsgType)Convert.ToByte(GetOption(53).Data);

			ParseDHCPPacket();
		}

		public DHCPPacket(DHCPMsgType msgType, int size = 0)
		{
			Buffer = (size != 0) ? new MemoryStream(size) : new MemoryStream();
			DHCPOptions = new Dictionary<byte, DHCPOption>();
			MessageType = msgType;
			BootpMsgType = BootpMsgType.Request;
			BootpHWType = BootpHWType.Ethernet;
			BootpHWLen = 6;
			BootpHops = 0;
			TransactionId = (uint)new Random().Next(10000, 4300000);
			Seconds = 4;
			Flags = BootpFlags.Broadcast;
			ClientIP = IPAddress.Any;
			YourIP = IPAddress.Any;
			ServerIP = IPAddress.Any;
			RelayIP = IPAddress.Any;
			HWAddr = "b8:27:eb:97:b6:39".MacAsBytes();
			ServerName = string.Empty;
			Bootfile = string.Empty;
			MagicCookie = new byte[] { 99, 130, 83, 99 };
			AddOption(new DHCPOption(53, (byte)MessageType));
			Console.WriteLine(Seconds);
		}

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

		public void AddOption(DHCPOption dhcpoption)
		{
			if (!DHCPOptions.ContainsKey(dhcpoption.Option))
				DHCPOptions.Add(dhcpoption.Option, dhcpoption);
			else
				DHCPOptions[dhcpoption.Option] = dhcpoption;
		}

		public DHCPOption GetOption(byte opt)
			=> DHCPOptions[opt];

		public bool HasOption(byte opt)
			=> DHCPOptions.ContainsKey(opt);

		private void ParseDHCPPacket()
		{
			for (var i = 240; i < Buffer.Length;)
			{
				Buffer.Position = i;
				var opt = (byte)Buffer.ReadByte();

				if (opt != 255)
				{
					Buffer.Position = i + 1;
					var len = Buffer.ReadByte();
					var data = new byte[len];



					Buffer.Position = i + 2;
					Buffer.Read(data, 0, len);

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

		public override void CommitOptions()
		{
			AddOption(new DHCPOption(255));
			var length = 0;

			// Calculate the Length of the TargetBuffer Options!
			foreach (var option in DHCPOptions.Values)
			{
				length += option.Option != 255 ? 2 + option.Length : 1;
			}

			var offset = 240;
			Buffer.Position = offset;

			foreach (var option in DHCPOptions.Values)
			{
				offset += Buffer._WriteByte(option.Option);
				Buffer.Position = offset;

				if (option.Option == 255)
					break;

				offset += Buffer._WriteByte(option.Length);
				Buffer.Position = offset;

				if (option.Length != 1)
				{
					Buffer.Write(option.Data);
					offset += option.Length;
				}
				else
				{
					offset += Buffer._WriteByte(Convert.ToByte(option.Data[0]));
				}
				Buffer.Position = offset;
			}

			Buffer.SetLength(offset);
			Buffer.Capacity = offset;
			Length = Buffer.Capacity;
		}
	}
}
