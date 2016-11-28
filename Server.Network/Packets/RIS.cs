namespace Server.Network
{
	using System;
	using System.Text;
	using Crypto;
	using Extensions;
	using static Extensions.Functions;

	public sealed class RISPacket : PacketProvider, IDisposable
	{
		RISOPCodes pktType;
		bool isNTLMSSPPacket;
		Encoding encoding;

		public RISPacket(Encoding encoding, byte[] data = null, int length = 0, bool isNTLMSSPPacket = false,
			NTLMMessageType ntlmMsgType = NTLMMessageType.NTLMChallenge)
		{
			this.data = data;
			this.offset = 0;
			this.type = SocketType.BINL;
			this.encoding = encoding;

			this.length = length == 0 ? this.data.Length : this.length = length;

			#region "NTLMSSP"
			if (data.Length > 16)
			{
				this.isNTLMSSPPacket = Exts.BytesToString(this.data, Encoding.ASCII, 8, 7) == "NTLMSSP" || isNTLMSSPPacket ? true : false;

				if (!this.isNTLMSSPPacket && isNTLMSSPPacket)
				{
					var hdr = Exts.StringToByte("NTLMSSP\0", Encoding.ASCII);
					Array.Copy(hdr, 0, this.data, 8, hdr.Length);

					if (this.isNTLMSSPPacket)
						this.MessageType = ntlmMsgType;

					this.isNTLMSSPPacket = Exts.BytesToString(this.data, Encoding.ASCII, 8, 7) == "NTLMSSP" ? true : false;
				}
			}
			#endregion;
			var x = this.RequestType;
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
				return BitConverter.ToInt32(this.data, 4);
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

		public int ServiceName_Offset
		{
			set
			{
				if (this.pktType == RISOPCodes.NCR)
				{
					var bytes = BitConverter.GetBytes(value);
					Array.Reverse(bytes);

					CopyTo(ref bytes, 0, ref this.data, 18, bytes.Length);
				}
			}
		}

		public int Drivername_Offset
		{
			set
			{
				if (this.pktType == RISOPCodes.NCR)
				{
					var bytes = BitConverter.GetBytes(value);
					Array.Reverse(bytes);

					CopyTo(ref bytes, 0, ref this.data, 14, bytes.Length);
				}
			}
		}

		public int ParameterList_Offset
		{
			set
			{
				if (this.pktType == RISOPCodes.NCR)
				{
					var bytes = BitConverter.GetBytes(value);
					Array.Reverse(bytes);

					CopyTo(ref bytes, 0, ref this.data, 20, bytes.Length);
				}
			}
		}

		#region "NTLMSSP"
		public bool HaveLMResponse
		{
			get
			{
				return BitConverter.ToUInt16(this.data, 20) != ushort.MinValue &&
					this.OPCode == RISOPCodes.AUT && this.isNTLMSSPPacket ? true : false;
			}
		}

		public bool HaveNTLMResponse
		{
			get
			{
				return BitConverter.ToUInt16(this.data, 28) != ushort.MinValue &&
					this.OPCode == RISOPCodes.AUT && this.isNTLMSSPPacket ? true : false;
			}
		}

		public byte[] LMResponse
		{
			get
			{
				if (HaveLMResponse && this.isNTLMSSPPacket && this.OPCode == RISOPCodes.AUT)
				{
					var bytes = new byte[BitConverter.ToUInt16(this.data, 20)];
					Array.Copy(this.data, (BitConverter.ToUInt16(this.data, 24) + 8), bytes, 0, bytes.Length);

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
				return (NTLMMessageType)BitConverter.ToUInt32(this.data, 16);
			}

			set
			{
				var bytes = BitConverter.GetBytes(Convert.ToUInt32(value));
				Array.Copy(bytes, 0, this.data, 16, bytes.Length);
			}
		}

		public bool IsNTLMPacket
		{
			get
			{
				return this.isNTLMSSPPacket;
			}
		}

		public NTLMFlags Flags
		{
			get
			{
				return (NTLMFlags)BitConverter.ToInt32(this.data, 20);
			}

			set
			{
				var bytes = BitConverter.GetBytes(Convert.ToInt32(value));
				Array.Copy(bytes, 0, this.data, 20, bytes.Length);
			}
		}

		public byte[] NTLMResponse
		{
			get
			{
				if (HaveNTLMResponse && this.isNTLMSSPPacket && this.OPCode == RISOPCodes.AUT)
				{
					var bytes = new byte[BitConverter.ToUInt16(this.data, 28)];
					Array.Copy(this.data, (BitConverter.ToUInt16(this.data, 32) + 8), bytes, 0, bytes.Length);

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

				if (this.isNTLMSSPPacket)
				{
					switch (this.OPCode)
					{
						case RISOPCodes.NEG:
							break;
						case RISOPCodes.CHL:
							break;
						case RISOPCodes.AUT:
							val_offset = BitConverter.ToInt32(this.data, 40) + 8;
							val_length = BitConverter.ToUInt16(this.data, 36);
							break;
						default:
							break;
					}

					x = Exts.BytesToString(this.data, TestEncoding(ref this.data, (val_offset + 1)), val_offset, val_length);
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

				if (this.isNTLMSSPPacket)
				{
					switch (this.OPCode)
					{
						case RISOPCodes.NEG:
							break;
						case RISOPCodes.CHL:
							break;
						case RISOPCodes.AUT:
							val_offset = BitConverter.ToInt32(this.data, 48) + 8;
							val_length = BitConverter.ToUInt16(this.data, 44);
							break;
						default:
							break;
					}

					x = Exts.BytesToString(this.data, TestEncoding(ref this.data, (val_offset + 1)), val_offset, val_length);
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

				if (!this.isNTLMSSPPacket)
					return x;

				switch (this.OPCode)
				{
					case RISOPCodes.NEG:
						break;
					case RISOPCodes.CHL:
						break;
					case RISOPCodes.AUT:
						val_offset = BitConverter.ToInt32(this.data, 56) + 8;
						val_length = BitConverter.ToUInt16(this.data, 52);
						break;
					default:
						break;
				}

				x = Exts.BytesToString(this.data, TestEncoding(ref this.data, (val_offset + 1)), val_offset, val_length);
				if (x == "none")
					x = x.Replace("none", string.Empty);

				return x;
			}
		}
		#endregion

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

		public NTSTATUS AuthResponse
		{
			set
			{
				var x = BitConverter.GetBytes(Convert.ToUInt32(value));
				Array.Copy(x, 0, this.data, 8, x.Length);
				this.Length = x.Length;
			}
		}

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
				if (this.OPCode == RISOPCodes.RQU)
				{
					var offset = 36;
					var file = Exts.BytesToString(this.data, Encoding.ASCII, offset, (this.data.Length - offset) - 1);

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
				var type = Exts.BytesToString(this.data, Encoding.ASCII, 1, 3);
				SetOPCode(type);
				return type;
			}

			set
			{
				SetOPCode(value.ToUpper());

				var type = Exts.StringToByte(value.ToUpper(), Encoding.ASCII);
				CopyTo(ref type, 0, ref this.data, 1, type.Length);
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

		private void SetOPCode(string type)
		{
			switch (type)
			{
				case "RQU":
					this.OPCode = RISOPCodes.RQU;
					break;
				case "REQ":
					this.OPCode = RISOPCodes.REQ;
					break;
				case "NEG":
					this.OPCode = RISOPCodes.NEG;
					break;
				case "AUT":
					this.OPCode = RISOPCodes.AUT;
					break;
				case "NCQ":
					this.OPCode = RISOPCodes.NCQ;
					break;
				case "NCR":
					this.OPCode = RISOPCodes.NCR;
					break;
				case "OFF":
					this.OPCode = RISOPCodes.OFF;
					break;
				case "CHL":
					this.OPCode = RISOPCodes.CHL;
					break;
				case "RES":
					this.OPCode = RISOPCodes.RES;
					break;
				case "RSP":
					this.OPCode = RISOPCodes.RSP;
					break;
				case "RSU":
					this.OPCode = RISOPCodes.RSU;
					break;
			}
		}

		public void Dispose()
		{
			Array.Clear(this.data, 0, this.data.Length);
		}
	}
}
