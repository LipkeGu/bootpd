namespace Server.Network
{
	using Extensions;
	using System;
	using System.Text;
	using static Functions;

	public sealed class RISPacket : PacketProvider, IDisposable
	{
		Encoding encoding;

		public RISPacket(Encoding encoding, byte[] data = null, int length = 0, bool isNTLMSSPPacket = false,
			NTLMMessageType ntlmMsgType = NTLMMessageType.NTLMChallenge)
		{
			Data = data;
			Offset = 0;
			Type = SocketType.BINL;
			this.encoding = encoding;

			Length = length == 0 ? Data.Length : Length = length;

			#region "NTLMSSP"
			if (data.Length > 16)
			{
				IsNTLMPacket = Exts.BytesToString(Data, Encoding.ASCII, 8, 7) == "NTLMSSP" || isNTLMSSPPacket ? true : false;

				if (!IsNTLMPacket && isNTLMSSPPacket)
				{
					var hdr = Exts.StringToByte("NTLMSSP\0", Encoding.ASCII);
					Array.Copy(hdr, 0, Data, 8, hdr.Length);

					if (IsNTLMPacket)
						MessageType = ntlmMsgType;

					IsNTLMPacket = Exts.BytesToString(Data, Encoding.ASCII, 8, 7) == "NTLMSSP" ? true : false;
				}
			}
			#endregion;
			var x = RequestType;
		}

		public override SocketType Type
		{
			get; set;
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
			get
			{
				return BitConverter.ToInt32(Data, 4);
			}

			set
			{
				var bytes = BitConverter.GetBytes(value);
				CopyTo(bytes, 0, Data, 4);
			}
		}

		public int ServiceName_Offset
		{
			set
			{
				if (OPCode == RISOPCodes.NCR)
				{
					var bytes = BitConverter.GetBytes(value);
					Array.Reverse(bytes);

					CopyTo(bytes, 0, Data, 18, bytes.Length);
				}
			}
		}

		public int Drivername_Offset
		{
			set
			{
				if (OPCode == RISOPCodes.NCR)
				{
					var bytes = BitConverter.GetBytes(value);
					Array.Reverse(bytes);

					CopyTo(bytes, 0, Data, 14, bytes.Length);
				}
			}
		}

		public int ParameterList_Offset
		{
			set
			{
				if (OPCode == RISOPCodes.NCR)
				{
					var bytes = BitConverter.GetBytes(value);
					Array.Reverse(bytes);

					CopyTo(bytes, 0, Data, 20, bytes.Length);
				}
			}
		}

		#region "NTLMSSP"
		public bool HaveLMResponse
		{
			get
			{
				return BitConverter.ToUInt16(Data, 20) != ushort.MinValue &&
					OPCode == RISOPCodes.AUT && IsNTLMPacket ? true : false;
			}
		}

		public bool HaveNTLMResponse
		{
			get
			{
				return BitConverter.ToUInt16(Data, 28) != ushort.MinValue &&
					OPCode == RISOPCodes.AUT && IsNTLMPacket ? true : false;
			}
		}

		public byte[] LMResponse
		{
			get
			{
				if (HaveLMResponse && IsNTLMPacket && OPCode == RISOPCodes.AUT)
				{
					var bytes = new byte[BitConverter.ToUInt16(Data, 20)];
					Array.Copy(Data, (BitConverter.ToUInt16(Data, 24) + 8), bytes, 0, bytes.Length);

					return bytes;
				}
				else
					return new byte[0];
			}
		}

		public NTLMMessageType MessageType
		{
			get
			{
				return (NTLMMessageType)BitConverter.ToUInt32(Data, 16);
			}

			set
			{
				var bytes = BitConverter.GetBytes(Convert.ToUInt32(value));
				Array.Copy(bytes, 0, Data, 16, bytes.Length);
			}
		}

		public bool IsNTLMPacket { get; }

		public NTLMFlags Flags
		{
			get
			{
				return (NTLMFlags)BitConverter.ToInt32(Data, 20);
			}

			set
			{
				var bytes = BitConverter.GetBytes(Convert.ToInt32(value));
				Array.Copy(bytes, 0, Data, 20, bytes.Length);
			}
		}

		public byte[] NTLMResponse
		{
			get
			{
				if (HaveNTLMResponse && IsNTLMPacket && OPCode == RISOPCodes.AUT)
				{
					var bytes = new byte[BitConverter.ToUInt16(Data, 28)];
					Array.Copy(Data, (BitConverter.ToUInt16(Data, 32) + 8), bytes, 0, bytes.Length);

					return bytes;
				}
				else
					return new byte[0];
			}
		}

		public string TargetName
		{
			get
			{
				var x = string.Empty;
				var val_offset = 0;
				var val_length = ushort.MinValue;

				if (IsNTLMPacket)
				{
					switch (OPCode)
					{
						case RISOPCodes.NEG:
							break;
						case RISOPCodes.CHL:
							break;
						case RISOPCodes.AUT:
							val_offset = BitConverter.ToInt32(Data, 40) + 8;
							val_length = BitConverter.ToUInt16(Data, 36);
							break;
						default:
							break;
					}

					x = Exts.BytesToString(Data, TestEncoding(Data, (val_offset + 1)), val_offset, val_length);
				}

				return x;
			}
		}

		public string UserName
		{
			get
			{
				var x = string.Empty;
				var val_offset = 0;
				var val_length = ushort.MinValue;

				if (IsNTLMPacket)
				{
					switch (OPCode)
					{
						case RISOPCodes.NEG:
							break;
						case RISOPCodes.CHL:
							break;
						case RISOPCodes.AUT:
							val_offset = BitConverter.ToInt32(Data, 48) + 8;
							val_length = BitConverter.ToUInt16(Data, 44);
							break;
						default:
							break;
					}

					x = Exts.BytesToString(Data, TestEncoding(Data, (val_offset + 1)), val_offset, val_length);
				}

				return x;
			}
		}

		public string WorkStation
		{
			get
			{
				var x = string.Empty;
				var val_offset = 0;
				var val_length = ushort.MinValue;

				if (!IsNTLMPacket)
					return x;

				switch (OPCode)
				{
					case RISOPCodes.NEG:
						break;
					case RISOPCodes.CHL:
						break;
					case RISOPCodes.AUT:
						val_offset = BitConverter.ToInt32(Data, 56) + 8;
						val_length = BitConverter.ToUInt16(Data, 52);
						break;
					default:
						break;
				}

				x = Exts.BytesToString(Data, TestEncoding(Data, (val_offset + 1)), val_offset, val_length);
				if (x == "none")
					x = x.Replace("none", string.Empty);

				return x;
			}
		}
		#endregion

		public NTSTATUS AuthResponse
		{
			set
			{
				var x = BitConverter.GetBytes(Convert.ToUInt32(value));
				Array.Copy(x, 0, Data, 8, x.Length);
				Length = x.Length;
			}
		}

		public byte Orign
		{
			get
			{
				return Data[0];
			}

			set
			{
				Data[0] = value;
			}
		}

		public string FileName
		{
			get
			{
				if (OPCode == RISOPCodes.RQU)
				{
					var offset = 36;
					var file = Exts.BytesToString(Data, Encoding.ASCII, offset, (Data.Length - offset) - 1);

					return file.Length == 0 ? Settings.OSC_DEFAULT_FILE.ToLowerInvariant() : "{0}.osc".F(file.ToLowerInvariant());
				}
				else
					return string.Empty;
			}
		}

		public string RequestType
		{
			get
			{
				var type = Exts.BytesToString(Data, Encoding.ASCII, 1, 3);
				SetOPCode(type);
				return type;
			}

			set
			{
				SetOPCode(value.ToUpper());

				var type = Exts.StringToByte(value.ToUpper(), Encoding.ASCII);
				CopyTo(type, 0, Data, 1);
			}
		}

		public RISOPCodes OPCode { get; set; }

		private void SetOPCode(string type)
		{
			switch (type)
			{
				case "RQU":
					OPCode = RISOPCodes.RQU;
					break;
				case "REQ":
					OPCode = RISOPCodes.REQ;
					break;
				case "NEG":
					OPCode = RISOPCodes.NEG;
					break;
				case "AUT":
					OPCode = RISOPCodes.AUT;
					break;
				case "NCQ":
					OPCode = RISOPCodes.NCQ;
					break;
				case "NCR":
					OPCode = RISOPCodes.NCR;
					break;
				case "OFF":
					OPCode = RISOPCodes.OFF;
					break;
				case "CHL":
					OPCode = RISOPCodes.CHL;
					break;
				case "RES":
					OPCode = RISOPCodes.RES;
					break;
				case "RSP":
					OPCode = RISOPCodes.RSP;
					break;
				case "RSU":
					OPCode = RISOPCodes.RSU;
					break;
			}
		}

		public void Dispose()
		{
			Array.Clear(Data, 0, Data.Length);
		}
	}
}
