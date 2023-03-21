using Bootpd.Common.Network.Protocol.TFTP;
using System;
using System.Collections.Generic;

namespace Bootpd.Network.Packet
{
	public class TFTPPacket : BasePacket
	{
		public readonly Dictionary<string, string> Options;

		public TFTPMsgType MessageType
		{
			get
			{
				Buffer.Position = 0;
				return (TFTPMsgType)Buffer.ToUInt16();
			}
			set
			{

				Buffer.Position = 0;
				Buffer.Write(((ushort)value).LE16());
				Buffer.Position += sizeof(ushort);
			}
		}

		public ushort Block
		{
			get
			{
				Buffer.Position = 2;
				return Buffer.ToUInt16();
			}

			set
			{
				Buffer.Position = 2;
				Buffer.Write(value.LE16());
				Buffer.Position += sizeof(ushort);
			}
		}

		public TFTPErrorCode ErrorCode
		{
			get
			{
				Buffer.Position = 2;
				return (TFTPErrorCode)Buffer.ToUInt16();
			}
			set
			{
				Buffer.Position = 2;
				Buffer.Write(((ushort)value).LE16());
				Buffer.Position += sizeof(ushort);
			}
		}

		public string ErrorMessage
		{
			get
			{
				Buffer.Position = 4;
				return Buffer.ReadString((Buffer.Length - Buffer.Position));
			}
			set
			{
				Buffer.Position = 4;
				Buffer.Write(value);
				Buffer.Position += value.Length + 1;
			}
		}

		public TFTPPacket(TFTPMsgType msgType, int length) : base()
		{
			Options = new Dictionary<string, string>();
			Buffer = new System.IO.MemoryStream();
			MessageType = msgType;
		}

		public TFTPPacket(byte[] data) : base(data)
		{
			Options = new Dictionary<string, string>();
			ParsePacket();
		}

		public override void ParsePacket()
		{
			switch (MessageType)
			{
				case TFTPMsgType.UNK:
					break;
				case TFTPMsgType.RRQ:
					Buffer.Position = 2;
					var parts = Buffer.ReadString((Buffer.Length - Buffer.Position)).Split('\0');

					for (var i = 0; i < parts.Length; i++)
					{
						if (i == 0)
						{
							if (!Options.ContainsKey("file"))
								Options.Add("file", parts[i]);
							else
								Options["file"] = parts[i];
						}

						if (i == 1)
						{
							if (!Options.ContainsKey("mode"))
								Options.Add("mode", parts[i]);
							else
								Options["mode"] = parts[i];
						}

						if (parts[i] == "blksize")
						{
							if (!Options.ContainsKey(parts[i]))
								Options.Add(parts[i], parts[i + 1]);
							else
								Options[parts[i]] = parts[i + 1];
						}

						if (parts[i] == "tsize")
						{
							if (!Options.ContainsKey(parts[i]))
								Options.Add(parts[i], parts[i + 1]);
							else
								Options[parts[i]] = parts[i + 1];
						}

						if (parts[i] == "windowsize")
						{
							if (!Options.ContainsKey(parts[i]))
								Options.Add(parts[i], parts[i + 1]);
							else
								Options[parts[i]] = parts[i + 1];
						}

						if (parts[i] == "msftwindow")
						{
							if (!Options.ContainsKey(parts[i]))
								Options.Add(parts[i], parts[i + 1]);
							else
								Options[parts[i]] = parts[i + 1];
						}
					}

					Console.WriteLine("[D] Got TFTP Options:");

					foreach (var option in Options)
						Console.WriteLine("[D] {0}: {1}", option.Key, option.Value);

					break;
				case TFTPMsgType.WRQ:
					break;
				case TFTPMsgType.DAT:
					break;
				case TFTPMsgType.ACK:
					break;
				case TFTPMsgType.ERR:
					break;
				case TFTPMsgType.OCK:
					break;
				default:
					Console.WriteLine("Unknown OP!");
					break;
			}
		}

		public override void CommitOptions()
		{
			switch (MessageType)
			{
				case TFTPMsgType.UNK:
					break;
				case TFTPMsgType.RRQ:
					break;
				case TFTPMsgType.WRQ:
					break;
				case TFTPMsgType.DAT:
					break;
				case TFTPMsgType.ACK:
					break;
				case TFTPMsgType.ERR:
					break;
				case TFTPMsgType.OCK:
					Buffer.Position = 2;
					var offset = 2;
					foreach (var option in Options)
					{
						offset += Buffer.Write(option.Key, option.Key.Length + 1);
						offset += Buffer.Write(option.Value, option.Value.Length + 1);
					}

					Buffer.Position = offset;
					break;
				default:
					break;
			}

			Buffer.Capacity = (int)Buffer.Length;
			Buffer.SetLength(Buffer.Capacity);

		}
	}
}
