using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

using WDSServer.Providers;

namespace WDSServer.Network
{
	#region "Allgemeine DHCP Funktionen"
	public sealed class DHCP : ServerProvider, IDHCPServer_Provider, IDisposable
	{
		public static Dictionary<string, DHCPClient> Clients =
			new Dictionary<string, DHCPClient>();

		public static ServerMode Mode;

		public DHCPSocket DHCPsocket;

		public BINLSocket BINLsocket;

		DHCPMsgType msgType;

		byte[] ntlmkey;

		public DHCP(IPEndPoint socket, int port, ServerMode mode = ServerMode.KnownOnly)
		{
			Mode = mode;
			this.ntlmkey = new byte[24];

			this.BINLsocket = new BINLSocket(new IPEndPoint(socket.Address, port));
			this.BINLsocket.Type = SocketType.BINL;
			this.BINLsocket.DataReceived += this.DataReceived;
			this.BINLsocket.DataSend += this.DataSend;
			this.endp = socket;

			this.DHCPsocket = new DHCPSocket(this.endp, true);
			this.DHCPsocket.Type = SocketType.DHCP;
			this.DHCPsocket.DataReceived += this.DataReceived;
			this.DHCPsocket.DataSend += this.DataSend;
		}

		public override IPEndPoint LocalEndPoint
		{
			get
			{
				return this.endp;
			}

			set
			{
				this.endp = value;
			}
		}

		public DHCPMsgType MsgType
		{
			get
			{
				return this.msgType;
			}

			set
			{
				this.msgType = value;
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

		public void Handle_DHCP_Request(DHCPPacket packet, ref DHCPClient client)
		{
			var response = new DHCPPacket(new byte[1024]);
			Array.Copy(packet.Data, 0, response.Data, 0, 242);

			response.BootpType = BootMessageType.Reply;
			response.ServerName = Settings.ServerName;
			response.NextServer = this.LocalEndPoint.Address;
			response.Type = client.Type;
			response.Offset += 243;

			switch (packet.Type)
			{
				case SocketType.DHCP:
					client.MsgType = DHCPMsgType.Offer;
					break;
				case SocketType.BINL:
					if (Functions.GetOptionOffset(ref packet, DHCPOptionEnum.WDSNBP) != 0)
						client.IsWDSClient = true;
					else
						client.IsWDSClient = false;

					client.MsgType = DHCPMsgType.Ack;
					break;
				default:
					Clients.Remove(client.ID);
					return;
			}

			Functions.SelectBootFile(ref client, client.IsWDSClient);

			// Bootfile
			response.Bootfile = client.BootFile;

			// Option 53
			response.MessageType = client.MsgType;

			// Option 60
			var opt = Exts.SetDHCPOption(DHCPOptionEnum.Vendorclassidentifier, Encoding.ASCII.GetBytes("PXEClient".ToCharArray()));

			Array.Copy(opt, 0, response.Data, response.Offset, opt.Length);
			response.Offset += opt.Length;

			// Option 54
			var dhcpident = Exts.SetDHCPOption(DHCPOptionEnum.ServerIdentifier, Exts.GetServerIP().GetAddressBytes());

			Array.Copy(dhcpident, 0, response.Data, response.Offset, dhcpident.Length);
			response.Offset += dhcpident.Length;

			// Option 97
			var guidopt = Exts.SetDHCPOption(DHCPOptionEnum.GUID, Exts.GetOptionValue(packet.Data, DHCPOptionEnum.GUID));

			Array.Copy(guidopt, 0, response.Data, response.Offset, guidopt.Length);
			response.Offset += guidopt.Length;

			// Option 252 - BCDStore
			if (client.BCDPath != null)
			{
				var bcdstore = Exts.SetDHCPOption(DHCPOptionEnum.BCDPath, Encoding.ASCII.GetBytes(client.BCDPath.ToCharArray()));

				Array.Copy(bcdstore, 0, response.Data, response.Offset, bcdstore.Length);
				response.Offset += bcdstore.Length;
			}

			if (Mode == ServerMode.AllowAll)
				client.ActionDone = true;

			this.Handle_WDS_Options(ref response, client.AdminMessage, ref client);

			// End of Packet (255)
			var endopt = new byte[1];
			endopt[0] = (byte)DHCPOptionEnum.End;

			Array.Copy(endopt, 0, response.Data, response.Offset + 1, endopt.Length);
			response.Offset += endopt.Length;

			if (client == null)
				throw new Exception("Stop no client!");

			switch (packet.Type)
			{
				case SocketType.DHCP:
					this.Send(ref response, client.EndPoint);
					break;
				case SocketType.BINL:
					if (client.IsWDSClient)
						if (client.ActionDone)
						{
							this.Send(ref response, client.EndPoint);
							Clients.Remove(client.ID);
							client = null;
						}
						else
							break;
					else
						this.Send(ref response, client.EndPoint);
					break;
				default:
					break;
			}
		}

		public void Dispose()
		{
			Clients.Clear();
		}

		public void Handle_RIS_Request(RISPacket packet, ref RISClient client, bool encrypted = false)
		{
			var challenge = "rootroot";
			var ntlmssp = new NTLMSSP("root", challenge);
			var flags = ntlmssp.Flags;

			Errorhandler.Report(LogTypes.Info, "Message Type: {0}".F(packet.OPCode));
			switch (packet.OPCode)
			{
				case RISOPCodes.REQ:
					return;
				case RISOPCodes.RQU:
					var data = this.ReadOSCFile(packet.FileName, encrypted, encrypted ? this.ntlmkey : null);
					var rquResponse = new RISPacket(new byte[(data.Length + 40)]);

					if (!encrypted)
						rquResponse.RequestType = "RSU";
					else
						rquResponse.RequestType = "RSP";

					rquResponse.Orign = 130;

					Array.Copy(packet.Data, 8, rquResponse.Data, 8, 28);
					rquResponse.Offset = 36;

					Array.Copy(data, 0, rquResponse.Data, rquResponse.Offset, data.Length);

					rquResponse.Offset += data.Length;
					rquResponse.Length = data.Length + 36;
					#endregion

					this.Send(ref rquResponse, client.Endpoint);
					break;
				case RISOPCodes.NCQ:
					break;
				case RISOPCodes.AUT:
					var ntlmssp_packet = Functions.Unpack_Packet(packet.Data);
					if (packet.Length >= 28)
					{
						Console.WriteLine("Domain Info: ");
						Console.WriteLine("Length: {0}", ntlmssp_packet[28]);
						Console.WriteLine("Allocated: {0}", ntlmssp_packet[30]);
						Console.WriteLine("Value: {0}", Encoding.ASCII.GetString(ntlmssp_packet, ntlmssp_packet[32], ntlmssp_packet[28]));

						Console.WriteLine(string.Empty);
						Console.WriteLine("User Info: ");
						Console.WriteLine("Length: {0}", ntlmssp_packet[36]);
						Console.WriteLine("Allocated: {0}", ntlmssp_packet[38]);
						Console.WriteLine("Value: {0}", Encoding.ASCII.GetString(ntlmssp_packet, ntlmssp_packet[40], ntlmssp_packet[36]));

						Console.WriteLine(string.Empty);
						Console.WriteLine("Session Key: ");
						Console.WriteLine("Length: {0}", ntlmssp_packet[20]);
						Console.WriteLine("Allocated: {0}", ntlmssp_packet[22]);
						Console.WriteLine("Value: {0}", Exts.GetDataAsString(ntlmssp_packet, ntlmssp_packet[24], ntlmssp_packet[20]));

						Console.WriteLine(string.Empty);
						Console.WriteLine("Workstation info: ");
						Console.WriteLine("Length: {0}", ntlmssp_packet[44]);
						Console.WriteLine("Allocated: {0}", ntlmssp_packet[46]);
						Console.WriteLine("Value: {0}", Encoding.ASCII.GetString(ntlmssp_packet, ntlmssp_packet[48], ntlmssp_packet[44]));

						Array.Copy(ntlmssp_packet, ntlmssp_packet[48], this.ntlmkey, 0, ntlmssp_packet[44]);

						var resPacket = new RISPacket(new byte[10]);
						resPacket.RequestType = "RES";
						resPacket.Orign = 130;

						resPacket.Offset += 4;
						var res = BitConverter.GetBytes(0x00000000);

						resPacket.Length = 4;

						Array.Copy(res, 0, resPacket.Data, resPacket.Offset, res.Length);
						resPacket.Offset += res.Length;
						this.Send(ref resPacket, client.Endpoint);
					}

					break;
				case RISOPCodes.CHL:
					break;
				case RISOPCodes.NEG:
					var msg = NTLMSSP.CreateMessage(NTLMSSP.NTLMMessageType.Challenge, flags, challenge);
					var negResponse = new RISPacket(new byte[(8 + msg.Length)]);

					negResponse.OPCode = RISOPCodes.CHL;
					negResponse.RequestType = "CHL";
					negResponse.Orign = 130;

					negResponse.Offset = 8;
					Array.Copy(msg, 0, negResponse.Data, 8, msg.Length);

					negResponse.Offset += msg.Length;
					negResponse.Length = negResponse.Offset;

					this.Send(ref negResponse, client.Endpoint);
					break;
				default:
					break;
			}
		}

		internal override void DataReceived(object sender, DataReceivedEventArgs e)
		{
			var type = int.Parse(Exts.GetDataAsString(e.Data, 0, 1));

			switch (type)
			{
				case (int)BootMessageType.Request:
					using (var request = new DHCPPacket(e.Data))
					{
						request.Type = e.Type;

						var optvalue = Exts.GetOptionValue(e.Data, DHCPOptionEnum.Vendorclassidentifier);
						if (optvalue.Length < 1 || optvalue[0] == byte.MaxValue || e.Data[0] != (byte)BootMessageType.Request)
							return;

						var cguid = Exts.GetOptionValue(request.Data, DHCPOptionEnum.GUID);
						var guid = Guid.Empty;

						try
						{
							guid = Guid.Parse(Exts.GetGuidAsString(cguid, 0, cguid.Length));
						}
						catch (Exception)
						{
							return;
						}

						var clientMAC = Exts.GetDataAsString(request.MacAddress, 0, request.MACAddresslength);
						var clientTag = "{0}-{1}".F(guid, clientMAC);

						if (!Clients.ContainsKey(clientTag))
							Clients.Add(clientTag, new DHCPClient(guid, clientMAC, request.Type, e.RemoteEndpoint));
						else
						{
							Clients[clientTag].Type = request.Type;
							Clients[clientTag].EndPoint = e.RemoteEndpoint;
						}

						Clients[clientTag].RequestID = Settings.RequestID;

						var c = Clients[clientTag];
						switch (request.MessageType)
						{
							case DHCPMsgType.Request:
								if (e.RemoteEndpoint.Address == IPAddress.None)
									return;

								this.Handle_DHCP_Request(request, ref c);
								break;
							case DHCPMsgType.Discover:
								Clients[clientTag].RequestID = 1;

								this.Handle_DHCP_Request(request, ref c);
								break;
							case DHCPMsgType.Release:
								if (Clients.ContainsKey(clientTag))
									Clients.Remove(clientTag);
								break;
							default:
								return;
						}
					}

					break;
				case (int)BootMessageType.RISRequest:
					var packet = new RISPacket(e.Data);
					var client = new RISClient(e.RemoteEndpoint);

					switch (packet.RequestType)
					{
						case "RQU":
						case "REQ":
							Errorhandler.Report(LogTypes.Info, "====== RIS (OSChooser) ======");
							packet.OPCode = packet.RequestType == "REQ" ? RISOPCodes.REQ : RISOPCodes.RQU;

							this.Handle_RIS_Request(packet, ref client, packet.RequestType == "REQ" ? true : false);
							break;
						case "NEG":
						case "AUT":
							Errorhandler.Report(LogTypes.Info, "======= RIS (NTLMSSP) =======");
							packet.OPCode = packet.RequestType == "NEG" ? RISOPCodes.NEG : RISOPCodes.AUT;

							this.Handle_RIS_Request(packet, ref client);
							break;
						default:
							Errorhandler.Report(LogTypes.Info, "Got Unknown RIS Packet ({0})".F(packet.RequestType));
							break;
					}

					Errorhandler.Report(LogTypes.Info, "=============================");

					break;
				case (int)BootMessageType.RISReply:
				default:
					break;
			}

			GC.Collect();
		}

		internal override void DataSend(object sender, DataSendEventArgs e)
		{
			var flag = string.Empty;

			switch (this.Type)
			{
				case SocketType.DHCP:
					flag = "DHCP";
					break;
				case SocketType.BINL:
					flag = "BINL";
					break;
				default:
					return;
			}

			Errorhandler.Report(LogTypes.Info, "[{0}] Sent {1} bytes to: {2}".F(flag, e.BytesSend, e.RemoteEndpoint));
		}

		internal void Send(ref RISPacket packet, IPEndPoint endpoint)
		{
			this.BINLsocket.Send(endpoint, packet.Data, packet.Offset);
		}

		internal void Send(ref DHCPPacket packet, IPEndPoint endpoint)
		{
			switch (packet.Type)
			{
				case SocketType.DHCP:
					this.DHCPsocket.Send(endpoint, packet.Data, packet.Offset);
					break;
				case SocketType.BINL:
					this.BINLsocket.Send(endpoint, packet.Data, packet.Offset);
					break;
				default:
					break;
			}
		}

		internal void Handle_WDS_Options(ref DHCPPacket packet, string adminMessage, ref DHCPClient client)
		{
			var bytes = Encoding.ASCII.GetBytes(adminMessage.ToCharArray());
			var block = new byte[26 + bytes.Length];
			block[0] = (byte)DHCPOptionEnum.WDSNBP;

			var offset = 1;
			block[offset] = (byte)(block.Length - 3);
			offset += 1;

			// NextAction;
			block[offset] = (byte)WDSNBPOptions.NextAction;
			offset += 1;

			block[offset] = 1;
			offset += 1;

			block[offset] = (byte)NextActionOptionValues.Approval;
			offset += 1;

			// TODO: Align this!

			// RequestID
			block[offset] = (byte)WDSNBPOptions.RequestID;
			offset += 1;

			block[offset] = 4;
			offset += 4;

			block[offset] = Convert.ToByte(client.RequestID);
			offset += 1;

			// Pollintervall
			block[offset] = (byte)WDSNBPOptions.PollInterval;
			offset += 1;

			block[offset] = 2;
			offset += 2;

			block[offset] = Convert.ToByte(client.PollIntervall);
			offset += 1;

			// PollRetryCount
			block[offset] = (byte)WDSNBPOptions.PollRetryCount;
			offset += 1;

			block[offset] = 2;
			offset += 2;

			block[offset] = Convert.ToByte(client.RetryCount);
			offset += 1;

			// Message
			block[offset] = (byte)WDSNBPOptions.Message;
			offset += 1;

			block[offset] = Convert.ToByte(bytes.Length);
			offset += 1;

			Array.Copy(bytes, 0, block, offset, bytes.Length);
			offset += bytes.Length + 1;

			// ActionDone
			block[offset] = (byte)WDSNBPOptions.ActionDone;
			offset += 1;

			if (client.BootFile != string.Empty && client.ActionDone && client.IsWDSClient)
				block[offset] = 1;
			else
				block[offset] = 0;
			offset += 1;

			block[offset] = 0;
			offset += 1;

			block[offset] = 255;
			offset += 1;

			var wdsBlock = new byte[(2 + offset + bytes.Length)];
			Array.Copy(block, 0, wdsBlock, 0, offset);
			Array.Copy(wdsBlock, 0, packet.Data, packet.Offset, wdsBlock.Length);
			packet.Offset += wdsBlock.Length;
		}

		private byte[] ReadOSCFile(string filename, bool encrypted, byte[] key = null)
		{
			if (encrypted)
				return new byte[0];

			var oscfile = "OSChooser/English/{0}".F(filename);

			var buffer = new byte[Filesystem.Size(oscfile)];
			var bytesRead = 0;

			Files.Read(oscfile, ref buffer, out bytesRead);

			var oscContent = Exts.Replace(buffer, "%SERVERNAME%", Settings.ServerName);
			oscContent = Exts.Replace(oscContent, "%SERVERDOMAIN%", Settings.ServerDomain);
			oscContent = Exts.Replace(oscContent, "%NTLMV2Enabled%", "0");

			if (encrypted)
			{
				var rsp = RC4.Encrypt(key, oscContent);
				return rsp;
			}
			else
				return oscContent;
		}
	}
}
