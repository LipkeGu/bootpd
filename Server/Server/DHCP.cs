namespace Server.Network
{
	using Bootpd;
	using Bootpd.Common.Network.Protocol.DHCP;
	using Bootpd.Common.Network.Protocol.RBCP;
	using Bootpd.Network.Packet;
	using Crypto;
	using Extensions;
	using System;
	using System.Collections.Generic;
	using System.Net;
	using System.Text;
	using static Bootpd.Functions;

	public sealed class DHCP : ServerProvider
	{
		public static Dictionary<string, aDHCPClient> Clients = new Dictionary<string, aDHCPClient>();

		public static List<BootServer> Bootservers = new List<BootServer>();

		public DHCPSocket DHCPsocket;

		public BINLSocket BINLsocket;
		byte[] ntlmkey;

		public DHCP(IPEndPoint socket, int port, ServerMode mode = ServerMode.KnownOnly)
		{
			ntlmkey = new byte[24];
			RequestID = Settings.RequestID;
			Bootservers.Add(new BootServer("FB-WDS.ad.fblipke.de"));
			BINLsocket = new BINLSocket(new IPEndPoint(IPAddress.Any, port));
			BINLsocket.Type = SocketType.BINL;
			BINLsocket.DataReceived += DataReceived;
			BINLsocket.DataSend += DataSend;

			if (Settings.EnableDHCP)
			{
				DHCPsocket = new DHCPSocket(new IPEndPoint(IPAddress.Any, 67), true)
				{
					Type = SocketType.DHCP
				};
				DHCPsocket.DataReceived += DataReceived;
				DHCPsocket.DataSend += DataSend;
				/*
				if (Settings.AdvertPXEServerList)
					ReadServerList(ref Program);
				*/
			}
		}

		public static int RequestID { get; set; }

		public DHCPMsgType MsgType { get; set; }

		public void Handle_DHCP_Request(DHCPPacket packet, string client)
		{

			var pktlength = (ushort)1024;
			Clients[client].VendorIdent = VendorIdents.UNKN;

			var vendor_str = Encoding.ASCII.GetString(packet.GetOption(60).Data);
			if (vendor_str.Contains("PXEClient"))
				Clients[client].VendorIdent = VendorIdents.PXEClient;
			else if (vendor_str.Contains("PXEServer"))
				Clients[client].VendorIdent = VendorIdents.PXEServer;
			else if (vendor_str.Contains("AAPLBSDPC")) //AAPLBSDPC/i386/iMac4,1
				Clients[client].VendorIdent = VendorIdents.BSDP;

			switch (Clients[client].VendorIdent)
			{
				case VendorIdents.PXEServer:
				case VendorIdents.PXEClient:

					if (vendor_str.Contains(":"))
					{
						var vendor_parts = vendor_str.Split(':');
						if (vendor_parts.Length > 1)
						{
							Clients[client].PXEFramework = PXEFrameworks.UNDI;
							if (vendor_parts[1].ToUpper() == "ARCH" && !Clients[client].IsWDSClient)
								Clients[client].Arch = (Architecture)ushort.Parse(vendor_parts[2]).LE16();

							if (vendor_parts[3].ToUpper() == PXEFrameworks.UNDI.ToString())
							{
								Clients[client].UNDI_Major = ushort.Parse(vendor_parts[4].Substring(0, 3).Replace("00", string.Empty));
								Clients[client].UNDI_Minor = ushort.Parse(vendor_parts[4].Substring(3, 3).Replace("00", string.Empty));
							}
						}
					}

					break;
				case VendorIdents.BSDP:
					/* Apple Boot server discovery Protocol */
					var vendoridenParts = Encoding.ASCII.GetString(packet.GetOption(60).Data).Split('/');
					if (vendoridenParts[1].Contains("ppc"))
						Clients[client].BSDP.Architecture = BSDPArch.PPC;
					else if (vendoridenParts[1].Contains("i386"))
						Clients[client].BSDP.Architecture = BSDPArch.I386;

					var encBSDPopts = packet.GetEncOptions(43);

					foreach (var option in encBSDPopts)
					{
						switch ((BSDPEncOptions)option.Option)
						{
							case BSDPEncOptions.MessageType:
								Clients[client].BSDP.MsgType = (BSDPMsgType)Convert.ToByte(option.Data);
								break;
							case BSDPEncOptions.Version:
								Clients[client].BSDP.Version = BitConverter.ToUInt16(option.Data, 0);
								break;
							case BSDPEncOptions.ServerIdent:
								if (Clients[client].BSDP.MsgType != BSDPMsgType.Select)
									continue;

								Clients[client].BSDP.ServerIdent = new IPAddress(option.Data);
								break;
							case BSDPEncOptions.ServerPriority:
								Clients[client].BSDP.ServerPriority = ushort.MaxValue;

								break;
							case BSDPEncOptions.ReplyPort:
								if (Clients[client].BSDP.MsgType != BSDPMsgType.Select ||
									Clients[client].BSDP.MsgType != BSDPMsgType.List)
									continue;

								Clients[client].BSDP.ReplyPort
									= BitConverter.ToUInt16(option.Data, 0);

								if (Clients[client].BSDP.ReplyPort >= 1024)
									Console.WriteLine("[W] ReplyPort must be less than 1024!");

								break;
							case BSDPEncOptions.BoorImageListPath:
								if (Clients[client].BSDP.MsgType != BSDPMsgType.List)
									continue;

								/* Not used, too large and has to be downloaded by TFTP */

								Clients[client].BSDP.ImageListPath
									= Encoding.ASCII.GetString(option.Data);
								break;
							case BSDPEncOptions.DefaultBootimageId:
								break;
							case BSDPEncOptions.SelectedBootImage:
								break;
							case BSDPEncOptions.BootImageList:
								break;
							case BSDPEncOptions.Netboot10Firmware:
								Clients[client].BSDP.NetBoot10CLient = true;
								Clients[client].BSDP.Version = 0x0000;
								break;
							case BSDPEncOptions.BootimageAttribs:
								var attribCount = option.Length / sizeof(ushort);
								Console.WriteLine("[D] Got {0} image attributes", attribCount);
								for (var i = 0; i < attribCount;)
								{
									Clients[client].BSDP.ImageAttributes
										.Add((BSDPImageAttributes)BitConverter.ToUInt16(option.Data, i));

									i += sizeof(ushort);
								}
								break;
							case BSDPEncOptions.MaxMessageSize:
								Clients[client].BSDP.MaxMessageSize
									= BitConverter.ToUInt16(option.Data, 0);
								break;
							default:
								break;
						}
					}

					break;
				default:
					Console.WriteLine("[D] Got DHCP Request for {0}",
						Encoding.ASCII.GetString(packet.GetOption(60).Data));
					return;
			}

			Clients[client].IsWDSClient = packet.HasOption(250);

			// Bootfile

			var bootfile = string.Empty;
			var bcdPAth = string.Empty;

			var arch = Clients[client].Arch;
			var nextAction = Clients[client].WDS.NextAction;

			if (packet.HasOption(250))
			{
				Clients[client].IsWDSClient = true;
				//				Handle_WDS_Request(client, ref packet);

				arch = Clients[client].Arch;
				nextAction = Clients[client].WDS.NextAction;

				//		SelectBootFile(out bootfile, out bcdPAth, Clients[client].IsWDSClient, nextAction, arch);


				// Option 252 - BCDStore
				//if (!string.IsNullOrEmpty(bcdPAth) && Clients[client].WDS.ActionDone && Clients[client].IsWDSClient)
				//	response.AddOption(new DHCPOption(252, bcdPAth));
			}

			//			SelectBootFile(out bootfile, out bcdPAth, Clients[client].IsWDSClient, nextAction, arch);

			//response.Bootfile = bootfile;

			//var rbcpOptions = Handle_RBCP_Request(client, packet);
			//response.AddOption(rbcpOptions);

			// Windows Deployment Server (WDSNBP Options)
			//response.AddOption(Handle_WDS_Options(client, ref packet));


			// End of Packet (255)
			//response.AddOption(new DHCPOption(255));


			//response.CommitOptions();

			/*
			switch (packet.Type)
			{
				case SocketType.DHCP:
					Send(ref response, Clients[client].EndPoint);
					Clients.Remove(client);
					break;
				case SocketType.BINL:
					if (!Clients.ContainsKey(client))
						break;

					if (Clients[client].IsWDSClient)
						if (Clients[client].WDS.ActionDone)
						{
							Send(ref response, Clients[client].EndPoint);
							Clients.Remove(client);
							RequestID += 1;
						}
						else
							break;
					else
						Send(ref response, Clients[client].EndPoint);
					break;
				default:
					break;
			}
			*/
		}

		public static byte[] ParameterlistEntry(string name, string type, string value)
		{
			var n = Exts.StringToByte(name, Encoding.ASCII);
			var t = Exts.StringToByte(type, Encoding.ASCII);
			var v = Exts.StringToByte(value, Encoding.ASCII);

			var data = new byte[n.Length + t.Length + v.Length + 2];

			var offset = 0;
			offset += CopyTo(n, 0, data, offset) + 1;
			offset += CopyTo(t, 0, data, offset) + 1;

			CopyTo(v, 0, data, offset);

			return data;
		}

		public void Handle_RIS_Request(ref RISPacket packet, ref RISClient client, bool encrypted = false)
		{
			var retval = 1;

			switch (packet.OPCode)
			{
				case RISOPCodes.RQU:
					#region "OSC File Request"
					var d = packet.Data;

					Files.Write("RIS_{0}_packet_{1}.bin".F(packet.OPCode, packet.FileName), ref d);
					var data = ReadOSCFile(packet.FileName, encrypted, Encoding.ASCII, encrypted ? ntlmkey : null);

					if (data == null)
						return;

					var rquResponse = new RISPacket(Encoding.ASCII, new byte[(data.Length + 40)])
					{
						RequestType = !encrypted ? "RSU" : "RSP",
						Orign = 130
					};

					Array.Copy(packet.Data, 8, rquResponse.Data, 8, 28);
					rquResponse.Offset = 36;

					Array.Copy(data, 0, rquResponse.Data, rquResponse.Offset, data.Length);

					rquResponse.Offset += data.Length;
					rquResponse.Length = data.Length + 36;

					//	Send(ref rquResponse, client.Endpoint, true);
					#endregion
					break;
				case RISOPCodes.NCQ:
					#region "Network Card Query"
					var ncq_packet = Unpack_Packet(ref packet);

					var vendorid = new byte[2];
					Array.Copy(ncq_packet, 28, vendorid, 0, vendorid.Length);
					Array.Reverse(vendorid);

					var deviceid = new byte[2];
					Array.Copy(ncq_packet, 30, deviceid, 0, deviceid.Length);
					Array.Reverse(deviceid);

					var vid = Encoding.ASCII.GetString(vendorid);
					var pid = Encoding.ASCII.GetString(deviceid);

					var sysfile = string.Empty;
					var service = string.Empty;

					var bus = string.Empty;
					var characs = string.Empty;

					retval = 0; //FindDrv(Settings.DriverFile, vid, pid, out sysfile, out service, out bus, out characs);

					if (retval == 0)
					{
						var drv = Exts.StringToByte(sysfile, Encoding.Unicode);
						var svc = Exts.StringToByte(service, Encoding.Unicode);
						var pciid = Exts.StringToByte("PCI\\VEN_{0}&DEV_{1}".F(vid, pid), Encoding.Unicode);

						var ncr_packet = new RISPacket(Encoding.ASCII, new byte[512])
						{
							RequestType = "NCR",
							Orign = 130,
							Offset = 8
						};

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

						var description = ParameterlistEntry("Description", "2", "RIS Network Card");
						var characteristics = ParameterlistEntry("Characteristics", "1", characs);
						var bustype = ParameterlistEntry("BusType", "1", bus);
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

						//	Send(ref ncr_packet, client.Endpoint);
					}
					else
						Errorhandler.Report(LogTypes.Error, "Could not find Driver for: {0} - {1}".F(vid, pid));

					#endregion
					break;
				case RISOPCodes.OFF:
					if (packet.Length == 0)
						return;

					//	var off_packet = Unpack_Packet(ref packet);
					break;
				default:
					Console.WriteLine("Unknown Packet!");
					break;
			}
		}

		private byte[] Unpack_Packet(ref RISPacket packet)
		{
			throw new NotImplementedException();
		}

		internal override void DataReceived(object sender, DataReceivedEventArgs e)
		{
			switch ((BootMessageType)Convert.ToUInt32(e.Data[0]))
			{
				case BootMessageType.RISRequest:
					#region "RIS - Request"
					var packet = new RISPacket(Encoding.ASCII, e.Data);
					var client = new RISClient(e.RemoteEndpoint);

					if (!packet.IsNTLMPacket)
						Handle_RIS_Request(ref packet, ref client);
					else
						Handle_NTLMSSP_Request(ref packet, ref client);
					#endregion
					break;
				case BootMessageType.RISReply:
				default:
					break;
			}
		}

		internal override void DataSend(object sender, DataSendEventArgs e)
		{
			throw new NotImplementedException();
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

					//	ParseNegotiatedFlags(packet.Flags, ref client);

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
					var TargetInformation = NTLMSSP.TargetInfoBlock(Settings.ServerDomain, Settings.ServerName,
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

					//Send(ref negResponse, client.Endpoint, true);
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

					//Send(ref autResponse, client.Endpoint, true);
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
