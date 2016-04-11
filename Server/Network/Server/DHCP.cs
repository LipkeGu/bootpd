namespace bootpd
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using System.Text;

	public sealed class DHCP : ServerProvider, IDHCPServer_Provider, IDisposable
	{
		public static Dictionary<string, DHCPClient> Clients = new Dictionary<string, DHCPClient>();

		public static Dictionary<string, Serverentry> Servers = new Dictionary<string, Serverentry>();

		public DHCPSocket DHCPsocket;

		public BINLSocket BINLsocket;

		static int requestid;

		DHCPMsgType msgType;

		byte[] ntlmkey;

		public DHCP(IPEndPoint socket, int port, ServerMode mode = ServerMode.KnownOnly)
		{
			this.ntlmkey = new byte[24];
			requestid = Settings.RequestID;

			this.BINLsocket = new BINLSocket(new IPEndPoint(IPAddress.Any, port));
			this.BINLsocket.Type = SocketType.BINL;
			this.BINLsocket.DataReceived += this.DataReceived;
			this.BINLsocket.DataSend += this.DataSend;

			if (Settings.EnableDHCP)
			{
				this.DHCPsocket = new DHCPSocket(new IPEndPoint(IPAddress.Any, 67), true);
				this.DHCPsocket.Type = SocketType.DHCP;
				this.DHCPsocket.DataReceived += this.DataReceived;
				this.DHCPsocket.DataSend += this.DataSend;

				if (Settings.AdvertPXEServerList)
					ReadServerList();
			}
		}

		public static void ReadServerList()
		{
			Functions.ReadServerList(Settings.ServersFile, ref Servers);
		}

		public static int RequestID
		{
			get
			{
				return requestid;
			}

			set
			{
				requestid = value;
			}
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
			try
			{
				var parameterlist_offset = Functions.GetOptionOffset(ref packet, DHCPOptionEnum.ParameterRequestList);
				var parameterlistLength = packet.Data[(parameterlist_offset + 1)];
				var bootitem = (ushort)0;
				var pktlength = (ushort)1024;

				if (parameterlist_offset != 0)
					Array.Clear(packet.Data, parameterlist_offset, parameterlistLength);

				var vendoroffset = Functions.GetOptionOffset(ref packet, DHCPOptionEnum.Vendorclassidentifier);
				if (vendoroffset == 0)
					return;

				var vendor_str = Encoding.ASCII.GetString(packet.Data, vendoroffset + 2, packet.Data[vendoroffset + 1]);

				if (vendor_str.Contains("PXEClient"))
				{
					client.VendorIdent = "PXEClient";

					// Option contains additional Informations!
					if (vendor_str.Contains(":"))
					{
						var vendor_parts = vendor_str.Split(':');
						if (vendor_parts.Length > 1)
						{
							client.PXEFramework = PXEFrameworks.UNDI;
							if (vendor_parts[1] == "ARCH")
								client.Arch = (Architecture)short.Parse(vendor_parts[2]);

							if (vendor_parts[3] == "UNDI")
							{
								client.UNDI_Major = short.Parse(vendor_parts[4].Substring(0, 3).Replace("00", string.Empty));
								client.UNDI_Minor = short.Parse(vendor_parts[4].Substring(3, 3).Replace("00", string.Empty));
							}
						}
					}
				}
				else
					return;

				var response = new DHCPPacket(new byte[pktlength]);
				Array.Copy(packet.Data, 0, response.Data, 0, 242);

				response.BootpType = BootMessageType.Reply;
				response.ServerName = Settings.ServerName;
				response.NextServer = Settings.ServerIP;
				response.Type = client.Type;
				response.Offset += 243;

				if (Functions.GetOptionOffset(ref packet, DHCPOptionEnum.WDSNBP) != 0)
					client.IsWDSClient = true;
				else
					client.IsWDSClient = false;

				switch (packet.Type)
				{
					case SocketType.DHCP:
						client.MsgType = DHCPMsgType.Offer;
						break;
					case SocketType.BINL:

						client.MsgType = DHCPMsgType.Ack;
						break;
					default:
						Clients.Remove(client.ID);
						return;
				}

				// Option 53
				response.MessageType = client.MsgType;

				// Option 60
				var opt = Exts.SetDHCPOption(DHCPOptionEnum.Vendorclassidentifier, Exts.StringToByte(client.VendorIdent));

				Array.Copy(opt, 0, response.Data, response.Offset, opt.Length);
				response.Offset += opt.Length;

				// Option 54
				var dhcpident = Exts.SetDHCPOption(DHCPOptionEnum.ServerIdentifier, Settings.ServerIP.GetAddressBytes());

				Array.Copy(dhcpident, 0, response.Data, response.Offset, dhcpident.Length);
				response.Offset += dhcpident.Length;

				// Option 97
				var guidopt = Exts.SetDHCPOption(DHCPOptionEnum.GUID, Exts.GetOptionValue(packet.Data, DHCPOptionEnum.GUID));

				Array.Copy(guidopt, 0, response.Data, response.Offset, guidopt.Length);
				response.Offset += guidopt.Length;

				// Bootfile
				Functions.SelectBootFile(ref client);
				response.Bootfile = client.BootFile;

				// Option 53
				response.MessageType = client.MsgType;

				if (Settings.Servermode == ServerMode.AllowAll)
					client.ActionDone = true;

				// Option 94
				var cii = new byte[3];
				cii[0] = Convert.ToByte(client.PXEFramework);
				cii[1] = Convert.ToByte(client.UNDI_Major);
				cii[2] = Convert.ToByte(client.UNDI_Minor);

				var clientIFIdent = Exts.SetDHCPOption(DHCPOptionEnum.ClientInterfaceIdent, cii);
				Array.Copy(clientIFIdent, 0, response.Data, response.Offset, clientIFIdent.Length);
				response.Offset += clientIFIdent.Length;

				if (Settings.DHCP_DEFAULT_BOOTFILE.ToLowerInvariant().Contains("pxelinux"))
				{
					var magicstring = Exts.SetDHCPOption(DHCPOptionEnum.MAGICOption, BitConverter.GetBytes(0xf100747e));
					Array.Copy(magicstring, 0, response.Data, response.Offset, magicstring.Length);
					response.Offset += magicstring.Length;
				}

				// Option 252 - BCDStore
				if (client.BCDPath != null && client.ActionDone && client.IsWDSClient)
				{
					var bcdstore = Exts.SetDHCPOption(DHCPOptionEnum.BCDPath, Exts.StringToByte(client.BCDPath));

					Array.Copy(bcdstore, 0, response.Data, response.Offset, bcdstore.Length);
					response.Offset += bcdstore.Length;
				}

				#region "Server selection"
				if (Settings.AdvertPXEServerList && Servers.Count > 0 && client.UNDI_Major > 1)
				{
					var optionoffset = Functions.GetOptionOffset(ref packet, DHCPOptionEnum.VendorSpecificInformation);
					if (optionoffset != 0)
					{
						var data = new byte[packet.Data[optionoffset + 1]];

						Array.Copy(packet.Data, optionoffset + 2, data, 0, packet.Data[optionoffset + 1]);

						switch (data[0])
						{
							case (byte)Definitions.PXEVendorEncOptions.BootItem:
								bootitem = BitConverter.ToUInt16(data, 2);
								break;
							default:
								break;
						}
					}

					// Option 43:8
					var pxeservers = Functions.GenerateServerList(ref Servers, bootitem);
					if (pxeservers != null)
					{
						var vendoropt = Exts.SetDHCPOption(DHCPOptionEnum.VendorSpecificInformation, pxeservers, true);
						Array.Copy(vendoropt, 0, response.Data, response.Offset, vendoropt.Length);
						response.Offset += vendoropt.Length;

						response.Data[response.Offset] = Convert.ToByte(DHCPOptionEnum.End);
						response.Offset += 1;
					}

					if (bootitem != 254 && bootitem != 0)
					{
						var server = (from s in Servers where s.Value.Ident == bootitem select s.Value.Hostname).FirstOrDefault();

						// This Client will not be served by this Server...
						if (Clients.ContainsKey(client.ID) && Settings.Servermode == ServerMode.KnownOnly)
							Clients.Remove(client.ID);

						response.NextServer = Servers[server].IPAddress;
						response.Bootfile = Servers[server].Bootfile;
						response.ServerName = Servers[server].Hostname;
					}
				}
				#endregion

				// Windows Deployment Server (WDSNBP Options)
				var wdsnbp = Exts.SetDHCPOption(DHCPOptionEnum.WDSNBP, this.Handle_WDS_Options(client.AdminMessage, ref client));
				Array.Copy(wdsnbp, 0, response.Data, response.Offset, wdsnbp.Length);
				response.Offset += wdsnbp.Length;

				// End of Packet (255)
				var endopt = new byte[1];
				endopt[0] = Convert.ToByte(DHCPOptionEnum.End);

				Array.Copy(endopt, 0, response.Data, response.Offset, endopt.Length);
				response.Offset += endopt.Length;

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
								requestid += 1;
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
			catch
			{
			}


		}

		public void Dispose()
		{
			Clients.Clear();
			Servers.Clear();
		}

		public void Handle_RIS_Request(RISPacket packet, ref RISClient client, bool encrypted = false)
		{
			var challenge = "rootroot";
			var ntlmssp = new NTLMSSP("root", challenge);
			var flags = ntlmssp.Flags;
			var retval = 1;

			switch (packet.OPCode)
			{
				case RISOPCodes.REQ:
					return;
				case RISOPCodes.RQU:
					#region "OSC File Request"
					var data = this.ReadOSCFile(packet.FileName, encrypted, encrypted ? this.ntlmkey : null);

					if (data == null)
						return;

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

					this.Send(ref rquResponse, client.Endpoint);
					#endregion
					break;
				case RISOPCodes.NCQ:
					#region "Driver Query"
					var ncq_packet = Functions.Unpack_Packet(packet.Data);
					var sysfile = string.Empty;
					var service = string.Empty;

					var bus = string.Empty;
					var characs = string.Empty;

					var vendorid = new byte[2];
					Array.Copy(ncq_packet, 28, vendorid, 0, vendorid.Length);
					Array.Reverse(vendorid);

					var deviceid = new byte[2];
					Array.Copy(ncq_packet, 30, deviceid, 0, deviceid.Length);
					Array.Reverse(deviceid);

					var vid = Exts.GetDataAsString(vendorid, 0, vendorid.Length);
					var pid = Exts.GetDataAsString(deviceid, 0, deviceid.Length);

					retval = Functions.FindDrv(Settings.DriverFile, vid, pid,
					out sysfile, out service, out bus, out characs);

					if (retval == 0)
					{
						var drv = Encoding.Unicode.GetBytes(sysfile);
						var svc = Encoding.Unicode.GetBytes(service);
						var pciid = Encoding.Unicode.GetBytes("PCI\\VEN_{0}&DEV_{1}".F(vid, pid));

						var ncr_packet = new RISPacket(new byte[512]);
						ncr_packet.RequestType = "NCR";
						ncr_packet.Orign = 130;
						ncr_packet.Offset = 8;

						/* Result */
						var ncr_res = BitConverter.GetBytes((int)0x00000000);
						Array.Reverse(ncr_res);

						Array.Copy(ncr_res, 0, ncr_packet.Data, ncr_packet.Offset, ncr_res.Length);
						ncr_packet.Offset += ncr_res.Length;

						/* Type */
						var type = BitConverter.GetBytes((int)0x02000000);
						Array.Reverse(type);
						Array.Copy(type, 0, ncr_packet.Data, ncr_packet.Offset, type.Length);
						ncr_packet.Offset += type.Length;

						/* Offset of PCI ID*/
						var pciid_offset = BitConverter.GetBytes(0x24000000);
						Array.Reverse(pciid_offset);
						Array.Copy(pciid_offset, 0, ncr_packet.Data, ncr_packet.Offset, pciid_offset.Length);
						ncr_packet.Offset += pciid_offset.Length;

						/* Offset of FileName*/
						var driver_offset = BitConverter.GetBytes(0x50000000);
						Array.Reverse(driver_offset);
						Array.Copy(driver_offset, 0, ncr_packet.Data, ncr_packet.Offset, driver_offset.Length);
						ncr_packet.Offset += driver_offset.Length;

						/* Offset of Service */
						var service_offset = BitConverter.GetBytes(0x68000000);
						Array.Reverse(service_offset);
						Array.Copy(service_offset, 0, ncr_packet.Data, ncr_packet.Offset, service_offset.Length);
						ncr_packet.Offset += service_offset.Length;

						var description = Functions.ParameterlistEntry("Description", "2", "RIS Network Card");
						var characteristics = Functions.ParameterlistEntry("Characteristics", "1", characs);
						var bustype = Functions.ParameterlistEntry("BusType", "1", bus);
						var pl_length = description.Length + characteristics.Length + bustype.Length;
						var pl_size = BitConverter.GetBytes(pl_length);

						/* Length of Parameters */
						Array.Copy(pl_size, 0, ncr_packet.Data, ncr_packet.Offset, pl_size.Length);
						ncr_packet.Offset += pl_size.Length;

						/* Offset of Parameters */
						var pl_offset = BitConverter.GetBytes(0x74000000);
						Array.Reverse(pl_offset);
						Array.Copy(pl_offset, 0, ncr_packet.Data, ncr_packet.Offset, pl_offset.Length);
						ncr_packet.Offset += pl_offset.Length;

						/* PCI ID */
						Array.Copy(pciid, 0, ncr_packet.Data, ncr_packet.Offset, 41);
						ncr_packet.Offset = 80;

						/* FileName */
						Array.Copy(drv, 0, ncr_packet.Data, ncr_packet.Offset, drv.Length);
						ncr_packet.Offset += drv.Length + 2;

						/* Service */
						Array.Copy(svc, 0, ncr_packet.Data, ncr_packet.Offset, svc.Length);
						ncr_packet.Offset += svc.Length + 2;

						Array.Copy(description, 0, ncr_packet.Data, ncr_packet.Offset, description.Length);
						ncr_packet.Offset += description.Length + 1;

						Array.Copy(characteristics, 0, ncr_packet.Data, ncr_packet.Offset, characteristics.Length);
						ncr_packet.Offset += characteristics.Length + 1;

						Array.Copy(bustype, 0, ncr_packet.Data, ncr_packet.Offset, bustype.Length);
						ncr_packet.Offset += bustype.Length;

						ncr_packet.Length = ncr_packet.Offset + 2;

						Send(ref ncr_packet, client.Endpoint);
					}
					else
					{
						Console.WriteLine("Cant find Driver for: {0} - {1}", vid, pid);
					}





					#endregion
					break;
				case RISOPCodes.AUT:
					#region "NTLM Authenticate"
					var ntlmssp_packet = Functions.Unpack_Packet(packet.Data);
					if (packet.Length >= 28)
					{
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
					#endregion
					break;
				case RISOPCodes.CHL:
					break;
				case RISOPCodes.NEG:
					#region "NTLM Negotiate"
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
					#endregion
					break;
				default:
					break;
			}
		}

		internal override void DataReceived(object sender, DataReceivedEventArgs e)
		{
			switch (int.Parse(Exts.GetDataAsString(e.Data, 0, 1)))
			{
				case (int)BootMessageType.Request:
					#region "BOTP - Request"
					using (var request = new DHCPPacket(e.Data))
					{
						lock (Clients)
						{
							request.Type = e.Type;

							var optvalue = Exts.GetOptionValue(e.Data, DHCPOptionEnum.Vendorclassidentifier);
							if (optvalue.Length < 1 || optvalue[0] == byte.MaxValue || e.Data[0] != (byte)BootMessageType.Request)
								return;

							var cguid = Exts.GetOptionValue(request.Data, DHCPOptionEnum.GUID);
							if (cguid.Length == 1 || cguid.Length > 32)
								return;

							var guid = Guid.Parse(Exts.GetGuidAsString(cguid, cguid.Length, true));

							var clientMAC = Exts.GetDataAsString(request.MacAddress, 0, request.MACAddresslength);
							var clientTag = "{0}-{1}".F(guid, clientMAC);

							if (!Clients.ContainsKey(clientTag))
								Clients.Add(clientTag, new DHCPClient(guid, clientMAC, request.Type, e.RemoteEndpoint));
							else
							{
								Clients[clientTag].Type = request.Type;
								Clients[clientTag].EndPoint = e.RemoteEndpoint;
							}

							var c = Clients[clientTag];
							switch (request.MessageType)
							{
								case DHCPMsgType.Request:
									if (e.RemoteEndpoint.Address != IPAddress.None)
										this.Handle_DHCP_Request(request, ref c);
									break;
								case DHCPMsgType.Discover:
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
					}
					#endregion
					break;
				case (int)BootMessageType.RISRequest:
					#region "RIS - Request"
					var packet = new RISPacket(e.Data);
					var client = new RISClient(e.RemoteEndpoint);

					switch (packet.RequestType)
					{
						case "RQU":
						case "REQ":
							packet.OPCode = packet.RequestType == "REQ" ? RISOPCodes.REQ : RISOPCodes.RQU;
							this.Handle_RIS_Request(packet, ref client, packet.RequestType == "REQ" ? true : false);
							break;
						case "NEG":
						case "AUT":
							packet.OPCode = packet.RequestType == "NEG" ? RISOPCodes.NEG : RISOPCodes.AUT;
							this.Handle_RIS_Request(packet, ref client);
							break;
						case "NCQ":
							packet.OPCode = RISOPCodes.NCQ;
							this.Handle_RIS_Request(packet, ref client);
							break;
						default:
							Errorhandler.Report(LogTypes.Info, "Got Unknown RIS Packet ({0})".F(packet.RequestType));
							break;
					}
					#endregion
					break;
				case (int)BootMessageType.RISReply:
				default:
					break;
			}
		}

		internal override void DataSend(object sender, DataSendEventArgs e)
		{
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

		internal byte[] Handle_WDS_Options(string adminMessage, ref DHCPClient client)
		{
			var offset = 0;

			#region "Create Response Options"
			var nextaction = Functions.GenerateDHCPEncOption(Convert.ToByte(WDSNBPOptions.NextAction),
			sizeof(byte), BitConverter.GetBytes(Convert.ToByte(client.NextAction)));
			var length = nextaction.Length;

			var val = BitConverter.GetBytes(Convert.ToInt32(requestid));
			Array.Reverse(val);
			var reqid = Functions.GenerateDHCPEncOption(Convert.ToByte(WDSNBPOptions.RequestID), 4, val);
			length += reqid.Length;

			val = BitConverter.GetBytes(client.PollInterval);
			Array.Reverse(val);
			var pollintervall = Functions.GenerateDHCPEncOption(Convert.ToByte(WDSNBPOptions.PollInterval), 2, val);
			length += pollintervall.Length;

			val = BitConverter.GetBytes(client.RetryCount);
			Array.Reverse(val);
			var retrycount = Functions.GenerateDHCPEncOption(Convert.ToByte(WDSNBPOptions.PollRetryCount), 2, val);
			length += retrycount.Length;

			val = Exts.StringToByte(adminMessage);
			var message = Functions.GenerateDHCPEncOption(Convert.ToByte(WDSNBPOptions.Message), val.Length, val);
			length += message.Length;

			val = BitConverter.GetBytes(Convert.ToByte(client.BootFile != string.Empty && client.ActionDone && client.IsWDSClient ? 1 : 0));
			var actionDone = Functions.GenerateDHCPEncOption(Convert.ToByte(WDSNBPOptions.ActionDone), 1, val);
			length += actionDone.Length;

			var wdsend = BitConverter.GetBytes(Convert.ToByte(WDSNBPOptions.End));
			length += 1;

			var wdsBlock = new byte[length];
			Functions.CopyTo(ref actionDone, 0, ref wdsBlock, 0, actionDone.Length);
			offset = 3;

			offset += Functions.CopyTo(ref nextaction, 0, ref wdsBlock, offset, nextaction.Length);
			offset += Functions.CopyTo(ref pollintervall, 0, ref wdsBlock, offset, pollintervall.Length);
			offset += Functions.CopyTo(ref retrycount, 0, ref wdsBlock, offset, retrycount.Length);
			offset += Functions.CopyTo(ref reqid, 0, ref wdsBlock, offset, reqid.Length);
			offset += Functions.CopyTo(ref message, 0, ref wdsBlock, offset, message.Length);
			Functions.CopyTo(ref wdsend, 0, ref wdsBlock, offset, 1);
			#endregion

			return wdsBlock;
		}

		private byte[] ReadOSCFile(string filename, bool encrypted, byte[] key = null)
		{
			try
			{
				// unsupported for now...
				if (encrypted)
					return null;

				var file = Filesystem.ResolvePath("OSChooser/English/{0}".F(filename));
				var length = Filesystem.Size(file);
				var buffer = new byte[length];
				var bytesRead = 0;

				Files.Read(file, ref buffer, out bytesRead);

				var oscContent = Exts.Replace(buffer, "%SERVERNAME%", Settings.ServerName);
				oscContent = Exts.Replace(oscContent, "%SERVERDOMAIN%", Settings.ServerDomain);
				oscContent = Exts.Replace(oscContent, "%NTLMV2Enabled%", Settings.EnableNTLMV2 ? "1" : "0");

				if (encrypted)
					return RC4.Encrypt(key, oscContent);
				else
					return oscContent;
			}
			catch
			{
				var oscfile = string.Empty;

				oscfile += "<OSCML>";
				oscfile += "<META KEY=\"F3\" ACTION=\"REBOOT\">";
				oscfile += "<TITLE>  Client Installation Wizard</TITLE>";
				oscfile += "<FOOTER>[F3] restart computer [ENTER] Continue</FOOTER>";
				oscfile += "<BODY left=5 right=75><BR><BR>";
				oscfile += "The requested file \"{0}\" was not found on the server.".F(filename);
				oscfile += "</BODY></OSCML>";

				return Exts.StringToByte(oscfile);
			}
		}
	}
}
