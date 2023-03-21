using Bootpd.Common.Network.Protocol.DHCP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace Bootpd.Network.Packet
{
	public class DHCPPacket : BasePacket
	{
		Dictionary<byte, DHCPOption> Options;

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
				return Buffer.ToUInt16().LE16();
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
				return (BootpFlags)Buffer.ToUInt16();
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
				if (!sNamefieldOverloaded)
				{
					Buffer.Position = 44;
					return Buffer.ReadString(64);
				}
				else
				{
					return HasOption(66) ? GetOption(66).Data.GetString() : string.Empty;
				}
			}

			set
			{
				if (!sNamefieldOverloaded)
				{
					Buffer.Position = 44;
					Buffer.Write(value, 64);
				}
				else
				{
					Buffer.Position = 44;
					Buffer.WriteByte(255);
				}
			}
		}

		public string Bootfile
		{
			get
			{
				if (!filefieldOverloaded)
				{
					Buffer.Position = sNamefieldOverloaded ? (108 - 63) : 108;
					return Buffer.ReadString(128);
				}
				else
				{
					if (HasOption(67))
						return GetOption(67).Data.GetString();
					else
						return string.Empty;
				}
			}

			set
			{
				if (!filefieldOverloaded)
				{
					Buffer.Position = sNamefieldOverloaded ? (108 - 63) : 108;
					Buffer.Write(value.Trim());
					Buffer.Position += (128 - value.Trim().Length);
				}
				else
				{
					Buffer.Position = 108;
					Buffer.WriteByte(255);
				}
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
			Options = new Dictionary<byte, DHCPOption>();
			ParsePacket();
		}

		public DHCPPacket(int size = 0)
		{
			Buffer = (size != 0) ? new MemoryStream(size) : new MemoryStream();
			Options = new Dictionary<byte, DHCPOption>();
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
			ServerName = Environment.MachineName;
			Bootfile = string.Empty;
			MagicCookie = new byte[] { 99, 130, 83, 99 };
		}

		public List<DHCPOption> GetEncOptions(byte opt)
		{
			var dict = new List<DHCPOption>();

			var optionData = GetOption(opt).Data;

			for (var i = 0; i < optionData.Length;)
			{
				var o = optionData[i];

				if (o != byte.MaxValue)
				{
					var len = optionData[i + 1];
					var data = new byte[len];

					Functions.CopyTo(optionData, (i + 2), data, 0, len);
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

		public DHCPPacket CreateResponse(IPAddress serverIP)
		{
			DHCPPacket packet = null;
			var msgType = (DHCPMsgType)Convert.ToByte(GetOption(53).Data[0]);
			switch (BootpMsgType)
			{
				case BootpMsgType.Request:
					packet = new DHCPPacket
					{
						ServerName = Environment.MachineName,
						BootpHWType = BootpHWType,
						BootpHWLen = BootpHWLen,
						BootpHops = BootpHops,
						TransactionId = TransactionId,
						Seconds = Seconds,
						Flags = Flags,
						ClientIP = ClientIP,
						YourIP = YourIP,
						ServerIP = serverIP,
						RelayIP = RelayIP,
						MagicCookie = MagicCookie,
						BootpMsgType = BootpMsgType.Reply
					};


					Buffer.CopyTo(packet.Buffer, 28, 28, 6);

					packet.AddOption(new DHCPOption(54, packet.ServerIP));
					packet.AddOption(GetOption(97));
					packet.AddOption(new DHCPOption(60, "PXEClient"));

					switch (msgType)
					{
						case DHCPMsgType.Discover:
							packet.AddOption(new DHCPOption(53, DHCPMsgType.Offer));
							break;
						case DHCPMsgType.Request:
						case DHCPMsgType.Inform:
							packet.AddOption(new DHCPOption(53, DHCPMsgType.Ack));
							break;
						default:
							break;
					}
					break;
				case BootpMsgType.Reply:
					switch (msgType)
					{
						case DHCPMsgType.Offer:
							break;
						case DHCPMsgType.Ack:
							break;
						default:
							break;
					}
					break;
				default:
					break;
			}

			return packet;
		}

		public void AddOption(DHCPOption dhcpoption)
		{
			if (dhcpoption == null)
				return;

			if (!Options.ContainsKey(dhcpoption.Option))
				Options.Add(dhcpoption.Option, dhcpoption);
			else
				Options[dhcpoption.Option] = dhcpoption;
		}

		public DHCPOption GetOption(byte opt)
			=> HasOption(opt) ? Options[opt] : null;

		public bool HasOption(byte opt)
			=> Options.ContainsKey(opt);

		public override void ParsePacket()
		{
			Options.Clear();
			var cookieoffset = FindMagicCookie() + 4;

			for (var i = cookieoffset; i < Buffer.Length;)
			{
				Buffer.Position = i;

				var opt = (byte)Buffer.ReadByte();

				if (opt != byte.MaxValue)
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
				var ovld = Convert.ToByte(GetOption(52).Data[0]);

				switch (ovld)
				{
					case 1:
						filefieldOverloaded = true;
						Bootfile = "";
						break;
					case 2:
						sNamefieldOverloaded = true;
						ServerName = "";
						break;
					case 3:
						sNamefieldOverloaded = filefieldOverloaded = true;
						Bootfile = "";
						ServerName = "";
						break;
					default:
						break;
				}
			}

			Options.OrderBy(key => key.Key);

			if (HasOption(77) && GetOption(77).Data.GetString().Contains("PXE"))
			{
				Console.WriteLine("[W] Option 77: Non RFC compilant option data! (iPXE)");
			}
		}

		private long FindMagicCookie()
		{
			Buffer.Position = 236;

			for (var i = 0U; i < Buffer.Length - 4;)
			{
				Buffer.Position = i;

				if (Buffer.ToUint32() == 1666417251U)
				{
					Buffer.Position = i;

					return Buffer.Position;
				}

				i += sizeof(uint);
			}

			Buffer.Position = 236;
			return Buffer.Position;
		}

		public override void CommitOptions()
		{
			if (HasOption(52))
			{
				var ovld = Convert.ToByte(GetOption(52).Data[0]);

				switch (ovld)
				{
					case 1:
						filefieldOverloaded = true;
						Bootfile = "";
						break;
					case 2:
						sNamefieldOverloaded = true;
						ServerName = "";
						break;
					case 3:
						sNamefieldOverloaded = filefieldOverloaded = true;
						Bootfile = "";
						ServerName = "";
						break;
					default:
						break;
				}
			}

			Options.OrderBy(key => key.Key);
			AddOption(new DHCPOption(byte.MaxValue));
			var length = 0;

			foreach (var option in Options.Values)
				length += option.Option != byte.MaxValue ? 2 + option.Length : 1;

			// Find the Magic and increment it by 4!
			var offset = FindMagicCookie() + 4;
			Buffer.Position = offset;

			foreach (var option in Options.Values)
			{
				offset += Buffer._WriteByte(option.Option);
				Buffer.Position = offset;

				if (option.Option == byte.MaxValue)
					break;

				offset += Buffer._WriteByte(option.Length);
				Buffer.Position = offset;

				if (option.Length != 1)
				{
					Buffer.Write(option.Data);
					offset += option.Length;
				}
				else
					offset += Buffer._WriteByte(Convert.ToByte(option.Data[0]));

				Buffer.Position = offset;
			}

			Buffer.SetLength(offset);
			Buffer.Capacity = (int)offset;
			Length = Buffer.Capacity;
		}
	}
}
