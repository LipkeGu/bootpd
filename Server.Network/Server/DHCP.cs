namespace Server.Network
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using System.Text;
	using Crypto;
	using Extensions;
	using static Extensions.Functions;

	public sealed class DHCP : ServerProvider, IDHCPServer_Provider, IDisposable
	{
		public static Dictionary<string, DHCPClient> Clients = new Dictionary<string, DHCPClient>();

		public static Dictionary<string, Serverentry<ushort>> Servers = new Dictionary<string, Serverentry<ushort>>();

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
				/*
				if (Settings.AdvertPXEServerList)
					ReadServerList(ref Program);
				*/
			}
		}

		public static void ReadServerList(ref SQLDatabase db)
		{
			Functions.ReadServerList(ref db, ref Servers);
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
			var parameterlist_offset = Functions.GetOptionOffset(ref packet, DHCPOptionEnum.ParameterRequestList);
			var parameterlistLength = packet.Data[(parameterlist_offset + 1)];
			var bootitem = ushort.MinValue;
			var pktlength = (ushort)1024;

			if (parameterlist_offset != 0)
				Array.Clear(packet.Data, parameterlist_offset, parameterlistLength);

			var vendoroffset = Functions.GetOptionOffset(ref packet, DHCPOptionEnum.Vendorclassidentifier);
			if (vendoroffset == 0)
				return;

			var vendor_str = Encoding.ASCII.GetString(packet.Data, vendoroffset + 2, packet.Data[vendoroffset + 1]);
			if (vendor_str.Contains(VendorIdents.PXEClient.ToString()))
				client.VendorIdent = VendorIdents.PXEClient;
			else if (vendor_str.Contains(VendorIdents.PXEServer.ToString()))
				client.VendorIdent = VendorIdents.PXEServer;
			else if (vendor_str.Contains(VendorIdents.BSDP.ToString()))
				client.VendorIdent = VendorIdents.BSDP;
			else
				return;

			switch (client.VendorIdent)
			{
				case VendorIdents.PXEClient:
					if (vendor_str.Contains(":"))
					{
						var vendor_parts = vendor_str.Split(':');
						if (vendor_parts.Length > 1)
						{
							client.PXEFramework = PXEFrameworks.UNDI;
							if (vendor_parts[1].ToUpper() == "ARCH" && !client.IsWDSClient)
								client.Arch = (Architecture)ushort.Parse(vendor_parts[2]);

							if (vendor_parts[3].ToUpper() == PXEFrameworks.UNDI.ToString())
							{
								client.UNDI_Major = ushort.Parse(vendor_parts[4].Substring(0, 3).Replace("00", string.Empty));
								client.UNDI_Minor = ushort.Parse(vendor_parts[4].Substring(3, 3).Replace("00", string.Empty));
							}
						}
					}
					break;
				case VendorIdents.PXEServer:
					/* Server <-> Server Communication */
					break;
				case VendorIdents.BSDP:
					/* Apple Boot server discovery Protocol */
					break;
				default:
					return;
			}

			var response = new DHCPPacket(new byte[pktlength]);
			Array.Copy(packet.Data, 0, response.Data, 0, 242);

			response.BootpType = BootMessageType.Reply;
			response.ServerName = Settings.ServerName;
			response.NextServer = Settings.ServerIP;
			response.Type = client.Type;
			response.Offset += 243;

			client.IsWDSClient = Functions.GetOptionOffset(ref packet, DHCPOptionEnum.WDSNBP) != 0 ? true : false;
			
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
			var opt = Exts.SetDHCPOption((int)DHCPOptionEnum.Vendorclassidentifier, Exts.StringToByte(client.VendorIdent.ToString(), Encoding.ASCII));

			Array.Copy(opt, 0, response.Data, response.Offset, opt.Length);
			response.Offset += opt.Length;

			// Option 54
			var dhcpident = Exts.SetDHCPOption((int)DHCPOptionEnum.ServerIdentifier, Settings.ServerIP.GetAddressBytes());

			Array.Copy(dhcpident, 0, response.Data, response.Offset, dhcpident.Length);
			response.Offset += dhcpident.Length;

			// Option 97
			var guidopt = Exts.SetDHCPOption((int)DHCPOptionEnum.GUID, Exts.GetOptionValue(packet.Data, (int)DHCPOptionEnum.GUID));

			Array.Copy(guidopt, 0, response.Data, response.Offset, guidopt.Length);
			response.Offset += guidopt.Length;

			if (client.IsWDSClient)
			{
				var wds_arch = new byte[1];
				Array.Copy(packet.Data, 289, wds_arch, 0, wds_arch.Length);
				client.Arch = (Architecture)wds_arch[0];
			}

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

			var clientIFIdent = Exts.SetDHCPOption((int)DHCPOptionEnum.ClientInterfaceIdent, cii);
			Array.Copy(clientIFIdent, 0, response.Data, response.Offset, clientIFIdent.Length);
			response.Offset += clientIFIdent.Length;

			if (Settings.DHCP_DEFAULT_BOOTFILE.ToLowerInvariant().Contains("pxelinux"))
			{
				var magicstring = Exts.SetDHCPOption((int)DHCPOptionEnum.MAGICOption, BitConverter.GetBytes(0xf100747e));
				Array.Copy(magicstring, 0, response.Data, response.Offset, magicstring.Length);
				response.Offset += magicstring.Length;
			}

			// Option 252 - BCDStore
			if (!string.IsNullOrEmpty(client.BCDPath) && client.ActionDone && client.IsWDSClient)
			{
				var bcdstore = Exts.SetDHCPOption((int)DHCPOptionEnum.BCDPath, Exts.StringToByte(client.BCDPath, Encoding.ASCII));

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
						case (byte)PXEVendorEncOptions.BootItem:
							bootitem = BitConverter.ToUInt16(data, 2);
							break;
						default:
							break;
					}
				}

				// Option 43:8
				var pxeservers = Functions.GenerateServerList(ref Servers, bootitem);
				if (pxeservers != null && client.PXEFramework == PXEFrameworks.UNDI)
				{
					var vendoropt = Exts.SetDHCPOption((int)DHCPOptionEnum.VendorSpecificInformation, pxeservers, true);
					Array.Copy(vendoropt, 0, response.Data, response.Offset, vendoropt.Length);
					response.Offset += vendoropt.Length;

					response.Data[response.Offset] = Convert.ToByte(DHCPOptionEnum.End);
					response.Offset += 1;
				}

				var server = (from s in Servers where s.Value.Ident == bootitem select s.Value.Hostname).FirstOrDefault();
				response.NextServer = Servers[server].IPAddress;
				response.Bootfile = Servers[server].Bootfile;
				response.ServerName = Servers[server].Hostname;
			}
			#endregion

			// Windows Deployment Server (WDSNBP Options)
			var wdsnbp = Exts.SetDHCPOption((int)DHCPOptionEnum.WDSNBP, this.Handle_WDS_Options(ref client));
			Array.Copy(wdsnbp, 0, response.Data, response.Offset, wdsnbp.Length);
			response.Offset += wdsnbp.Length;

			// End of Packet (255)
			response.Data[response.Offset] = Convert.ToByte(DHCPOptionEnum.End);
			response.Offset += 1;

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

		public void Dispose()
		{
			Clients.Clear();
			Servers.Clear();
		}

		public void Handle_RIS_Request(ref RISPacket packet, ref RISClient client, bool encrypted = false)
		{
			var retval = 1;

			switch (packet.OPCode)
			{
				case RISOPCodes.REQ:
					Console.WriteLine("REQ Packet!");
					return;
				case RISOPCodes.RQU:
					#region "OSC File Request"
					var d = packet.Data;

					Files.Write("RIS_{0}_packet_{1}.bin".F(packet.OPCode, packet.FileName), ref d);
					var data = this.ReadOSCFile(packet.FileName, encrypted, Encoding.ASCII, encrypted ? this.ntlmkey : null);

					if (data == null)
						return;

					var rquResponse = new RISPacket(Encoding.ASCII, new byte[(data.Length + 40)]);
					rquResponse.RequestType = !encrypted ? "RSU" : "RSP";
					rquResponse.Orign = 130;

					Array.Copy(packet.Data, 8, rquResponse.Data, 8, 28);
					rquResponse.Offset = 36;

					Array.Copy(data, 0, rquResponse.Data, rquResponse.Offset, data.Length);

					rquResponse.Offset += data.Length;
					rquResponse.Length = data.Length + 36;

					this.Send(ref rquResponse, client.Endpoint,true);
					#endregion
					break;
				case RISOPCodes.NCQ:
					#region "Network Card Query"
					var ncq_packet = Functions.Unpack_Packet(ref packet);

					var vendorid = new byte[2];
					Array.Copy(ncq_packet, 28, vendorid, 0, vendorid.Length);
					Array.Reverse(vendorid);

					var deviceid = new byte[2];
					Array.Copy(ncq_packet, 30, deviceid, 0, deviceid.Length);
					Array.Reverse(deviceid);

					var vid = Exts.GetDataAsString(vendorid, 0, vendorid.Length);
					var pid = Exts.GetDataAsString(deviceid, 0, deviceid.Length);

					var sysfile = string.Empty;
					var service = string.Empty;

					var bus = string.Empty;
					var characs = string.Empty;

					retval = FindDrv(Settings.DriverFile, vid, pid, out sysfile, out service, out bus, out characs);

					if (retval == 0)
					{
						var drv = Exts.StringToByte(sysfile, Encoding.Unicode);
						var svc = Exts.StringToByte(service, Encoding.Unicode);
						var pciid = Exts.StringToByte("PCI\\VEN_{0}&DEV_{1}".F(vid, pid), Encoding.Unicode);

						var ncr_packet = new RISPacket(Encoding.ASCII, new byte[512]);
						ncr_packet.RequestType = "NCR";
						ncr_packet.Orign = 130;
						ncr_packet.Offset = 8;

						/* Result */
						var ncr_res = BitConverter.GetBytes(0x00000000);
						Array.Reverse(ncr_res);

						Array.Copy(ncr_res, 0, ncr_packet.Data, ncr_packet.Offset, ncr_res.Length);
						ncr_packet.Offset += ncr_res.Length;

						/* Type */
						var type = BitConverter.GetBytes(0x02000000);
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

						/* Filename */
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
						Errorhandler.Report(LogTypes.Error, "Could not find Driver for: {0} - {1}".F(vid, pid));

					#endregion
					break;
				case RISOPCodes.OFF:
					if (packet.Length == 0)
						return;

					var off_packet = Functions.Unpack_Packet(ref packet);
					break;
				default:
					Console.WriteLine("Unknown Packet!");
					break;
			}
		}

		internal override void DataReceived(object sender, DataReceivedEventArgs e)
		{
			var x = int.Parse(Exts.GetDataAsString(e.Data,0,1));
			switch (x)
			{
				case (int)BootMessageType.Request:
					#region "BOOTP - Request"
					using (var request = new DHCPPacket(e.Data))
					{
						lock (Clients)
						{
							request.Type = e.Type;

							var optvalue = Exts.GetOptionValue(request.Data, (int)DHCPOptionEnum.Vendorclassidentifier);
							if (optvalue.Length < 1 || optvalue[0] == byte.MaxValue || request.BootpType != BootMessageType.Request)
								return;

							var cguid = Exts.GetOptionValue(request.Data, (int)DHCPOptionEnum.GUID);
							if (cguid.Length == 1 || cguid.Length > 32)
								return;

							var guid_string = Exts.GetGuidAsString(cguid, cguid.Length, true);
							if (string.IsNullOrEmpty(guid_string))
								return;

							var xa = guid_string.Split('-').Length;
							if (xa < 4)
								return;

							var guid = Guid.Parse(guid_string);

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
							/*
							if (Server.Database.Count("Computer", "UUID", c.Guid.ToString()) != 0)
							{
								var dbQuery = Server.Database.SQLQuery("SELECT * FROM Computer WHERE UUID LIKE '{0}' LIMIT 1".F(c.Guid));
								for (var i = 0U; i < dbQuery.Count; i++)
								{
									c.NextAction = (NextActionOptionValues)int.Parse(dbQuery[i]["NextAction"]);
									c.ActionDone = dbQuery[i]["ActionDone"] == "1" ? true : false;
								}
							}
							*/

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
					var packet = new RISPacket(Encoding.ASCII, e.Data);
					var client = new RISClient(e.RemoteEndpoint);

					if (!packet.IsNTLMPacket)
						this.Handle_RIS_Request(ref packet, ref client);
					else
						this.Handle_NTLMSSP_Request(ref packet, ref client);
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

		internal void Send(ref RISPacket packet, IPEndPoint endpoint, bool dump = false)
		{
			this.BINLsocket.Send(endpoint, packet.Data, packet.Offset);

			if (dump)
			{
				var d = packet.Data;
				Files.Write("packet_RIS_{0}.bin".F(packet.OPCode), ref d);
			}
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

		internal byte[] Handle_WDS_Options(ref DHCPClient client)
		{
			var offset = 0;

			#region "Create Response Options"
			var val = BitConverter.GetBytes(Convert.ToByte(client.NextAction));
			var nextaction = Functions.GenerateDHCPEncOption((int)WDSNBPOptions.NextAction, sizeof(byte), val);
			var length = nextaction.Length;

			val = BitConverter.GetBytes(requestid);
			Array.Reverse(val);
			var reqid = Functions.GenerateDHCPEncOption((int)WDSNBPOptions.RequestID, val.Length, val);
			length += reqid.Length;

			val = BitConverter.GetBytes(client.PollInterval);
			Array.Reverse(val);
			var pollintervall = Functions.GenerateDHCPEncOption((int)WDSNBPOptions.PollInterval, val.Length, val);
			length += pollintervall.Length;

			val = BitConverter.GetBytes(client.RetryCount);
			Array.Reverse(val);
			var retrycount = Functions.GenerateDHCPEncOption((int)WDSNBPOptions.PollRetryCount, val.Length, val);
			length += retrycount.Length;

			val = Exts.StringToByte(client.AdminMessage, Encoding.ASCII);
			var message = Functions.GenerateDHCPEncOption((int)WDSNBPOptions.Message, val.Length, val);
			length += message.Length;

			val = BitConverter.GetBytes(Convert.ToByte(!string.IsNullOrEmpty(client.BootFile) && client.ActionDone && client.IsWDSClient ? 1 : 0));
			var actionDone = Functions.GenerateDHCPEncOption(Convert.ToByte(WDSNBPOptions.ActionDone), 1, val);
			length += actionDone.Length;

			var wdsend = BitConverter.GetBytes(Convert.ToByte(WDSNBPOptions.End));
			length += 1;

			var wdsBlock = new byte[length];
			CopyTo(ref actionDone, 0, ref wdsBlock, 0, actionDone.Length);
			offset = 3;

			offset += CopyTo(ref nextaction, 0, ref wdsBlock, offset, nextaction.Length);
			offset += CopyTo(ref pollintervall, 0, ref wdsBlock, offset, pollintervall.Length);
			offset += CopyTo(ref retrycount, 0, ref wdsBlock, offset, retrycount.Length);
			offset += CopyTo(ref reqid, 0, ref wdsBlock, offset, reqid.Length);
			offset += CopyTo(ref message, 0, ref wdsBlock, offset, message.Length);
			offset += CopyTo(ref wdsend, 0, ref wdsBlock, offset, 1);
			#endregion

			return wdsBlock;
		}

		internal void Handle_NTLMSSP_Request(ref RISPacket packet, ref RISClient client)
		{
			var message = new byte[1024];

			switch (packet.MessageType)
			{
				case NTLMMessageType.NTLMNegotiate:
					var d = packet.Data;
					Files.Write("ntlmssp_{0}_packet.bin".F(packet.OPCode), ref d, 8);

					if (packet.Length < 16)
						return;

					Functions.ParseNegotiatedFlags(packet.Flags, ref client);

					#region "Create Challenge Message"
					var offset = 0;
					var tnsbOffset = 0;

					var context = BitConverter.GetBytes(Convert.ToUInt64(ulong.MinValue));
					var negotiateFlags = BitConverter.GetBytes(Convert.ToInt32(client.ServerFlags));

					if (BitConverter.IsLittleEndian)
						Array.Reverse(negotiateFlags);
					
					var servChallenge = BitConverter.GetBytes(DateTime.Now.ToFileTime());

					var signature = Exts.StringToByte("NTLMSSP\0", Encoding.ASCII);
					var mtype = BitConverter.GetBytes(Convert.ToUInt32(NTLMMessageType.NTLMChallenge));
					var domain = Exts.StringToByte(Settings.ServerDomain, Encoding.Unicode);

					offset += CopyTo(signature, 0, message, 0, signature.Length);
					offset += CopyTo(mtype, 0, message, offset, mtype.Length);

					if (client.NTLMSSP_REQUEST_TARGET)
					{
						#region "Target Name Security Buffer"
						var tn = string.Empty;

						switch (client.TargetType)
						{
							case NTLMTargets.Domain:
								tn = Settings.ServerDomain;
								break;
							case NTLMTargets.Server:
								tn = Settings.ServerName;
								break;
							case NTLMTargets.Share:
							case NTLMTargets.Local:
							default:
								Errorhandler.Report(LogTypes.Error, "Unsupportet Target: {0}".F(client.TargetType));
								return;
						}

						var targetName = Exts.StringToByte(tn, Encoding.Unicode);
						var targetNameLen = BitConverter.GetBytes(Convert.ToUInt16(targetName.Length));

						tnsbOffset += signature.Length;
						tnsbOffset += mtype.Length;
						tnsbOffset += targetNameLen.Length;
						tnsbOffset += targetNameLen.Length;
						tnsbOffset += targetName.Length;
						tnsbOffset += negotiateFlags.Length;
						tnsbOffset += servChallenge.Length;
						tnsbOffset += context.Length;

						var targetNameBufferOffset = BitConverter.GetBytes(Convert.ToUInt32(tnsbOffset));
						offset += CopyTo(targetNameLen, 0, message, offset, targetNameLen.Length);
						offset += CopyTo(targetNameLen, 0, message, offset, targetNameLen.Length);
						offset += CopyTo(targetNameBufferOffset, 0, message, offset, targetNameBufferOffset.Length);
						#endregion
					}

					offset += CopyTo(negotiateFlags, 0, message, offset, negotiateFlags.Length);
					offset += CopyTo(servChallenge, 0, message, offset, servChallenge.Length);
					offset += CopyTo(context, 0, message, offset, context.Length);

					#region "Target Information Security Buffer"

					// Include the Target Informations here...
					var TargetInformation = NTLMSSP.TargetInfoBlock(Settings.ServerDomain,Settings.ServerName, 
						Settings.ServerName + Settings.UserDNSDomain, Settings.UserDNSDomain);

					var targetInformationLen = BitConverter.GetBytes(Convert.ToUInt16(TargetInformation.Length));

					offset += CopyTo(targetInformationLen, 0, message, offset, targetInformationLen.Length);
					offset += CopyTo(targetInformationLen, 0, message, offset, targetInformationLen.Length);

					var targetInformationOffset = BitConverter.GetBytes(Convert.ToUInt32(offset + domain.Length + 4));
					offset += CopyTo(targetInformationOffset, 0, message, offset, targetInformationOffset.Length);
					#endregion

					offset += CopyTo(domain, 0, message, offset, domain.Length);
					offset += CopyTo(TargetInformation, 0, message, offset, TargetInformation.Length);
					#endregion

					var negResponse = new RISPacket(client.NTLMSSP_NEGOTIATED_ENCODING, new byte[(8 + offset)]);
					negResponse.RequestType = "CHL";
					negResponse.Orign = 130;

					negResponse.Offset = 8;
					Array.Copy(message, 0, negResponse.Data, negResponse.Offset, offset);
					negResponse.Offset += offset;
					negResponse.Length = negResponse.Offset;

					this.Send(ref negResponse, client.Endpoint, true);
					break;
				case NTLMMessageType.NTLMAuthenticate:
					#region "NTLM Authenticate"
					System.IO.File.WriteAllBytes("ntlmssp_aut_package.bin", packet.Data);

					if (packet.Length < 28)
						return;
					var result = NTSTATUS.SSPI_LOGON_DENIED;

					if ((packet.HaveLMResponse || packet.HaveNTLMResponse)/* && Server.Database.Count("Usernames", "Name", packet.UserName) != 0
						*/ && !string.IsNullOrEmpty(packet.TargetName))
					{
						/*
						var pwQuery = Server.Database.SQLQuery("SELECT * From Usernames WHERE Name LIKE '{0}' LIMIT 1".F(packet.UserName));
						Errorhandler.Report(LogTypes.Info, "UserName: {0}".F(packet.UserName));
						*/

						if (!string.IsNullOrEmpty(packet.WorkStation))
							Console.WriteLine("WorkStation: {0}", packet.WorkStation);

						result = NTSTATUS.ERROR_SUCCESS;
					}
					else
						result = NTSTATUS.SSPI_LOGON_DENIED;
					var x = new byte[12];

					var autResponse = new RISPacket(Encoding.ASCII, x);
					autResponse.OPCode = RISOPCodes.RES;
					autResponse.RequestType = "RES";

					autResponse.AuthResponse = result;
					autResponse.Orign = 130;
					autResponse.Offset = x.Length;
					 
					this.Send(ref autResponse, client.Endpoint, true);
					#endregion
					break;
				default:
					throw new NotImplementedException("Type not supported!");
			}
		}

		private byte[] ReadOSCFile(string filename, bool encrypted, Encoding encoding, byte[] key = null)
		{
			try
			{
				// unsupported for now...
				if (encrypted)
					return null;

				var file = Filesystem.ResolvePath("OSChooser/{1}/{0}".F(filename, Settings.OSC_DEFAULT_LANG), Settings.TFTPRoot);
				var length = Filesystem.Size(file);
				var buffer = new byte[length];
				var bytesRead = 0;

				Files.Read(file, ref buffer, out bytesRead);

				var oscContent = Exts.Replace(buffer, "%SERVERNAME%", Settings.ServerName, encoding);
				oscContent = Exts.Replace(oscContent, "%SERVERDOMAIN%", Settings.ServerDomain, encoding);
				oscContent = Exts.Replace(oscContent, "%NTLMV2Enabled%", Settings.EnableNTLMV2 ? "1" : "0", encoding);

				return oscContent;
			}
			catch
			{
				var oscfile = "<OSCML>";
				oscfile += "<META KEY=\"F3\" ACTION=\"REBOOT\">";
				oscfile += "<TITLE>  {0}</TITLE>".F(Settings.OSC_DEFAULT_TITLE);
				oscfile += "<FOOTER>[F3] Restart computer [ENTER] Continue</FOOTER>";
				oscfile += "<BODY left=5 right=75><BR><BR>";
				oscfile += "The requested file \"{0}\" was not found on the server.".F(filename);
				oscfile += "</BODY></OSCML>";

				return Exts.StringToByte(oscfile, encoding);
			}
		}
	}
}
