using Bootpd.Common.Network.Protocol.DHCP;
using Bootpd.Network.Client;
using Bootpd.Network.Packet;
using Server.Network;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Bootpd.Network.Server
{
	public sealed class DHCPServer : BaseServer
	{
		public DHCPServer(ServerType serverType) : base(serverType)
		{

		}

		private DHCPOption Handle_RBCP_Request(Guid client, DHCPPacket request)
		{

			if (request.HasOption(43))
			{
				var venEncOpts = request.GetEncOptions(43);
				foreach (var option in venEncOpts)
				{
					switch ((PXEVendorEncOptions)option.Option)
					{
						case PXEVendorEncOptions.MultiCastIPAddress:
							break;
						case PXEVendorEncOptions.MulticastClientPort:
							break;
						case PXEVendorEncOptions.MulticastServerPort:
							break;
						case PXEVendorEncOptions.MulticastTFTPTimeout:
							break;
						case PXEVendorEncOptions.MulticastTFTPDelay:
							break;
						case PXEVendorEncOptions.DiscoveryControl:
							break;
						case PXEVendorEncOptions.DiscoveryMulticastAddress:
							break;
						case PXEVendorEncOptions.BootServers:
							break;
						case PXEVendorEncOptions.BootMenue:
							break;
						case PXEVendorEncOptions.MenuPrompt:
							break;
						case PXEVendorEncOptions.MulticastAddressAllocation:
							break;
						case PXEVendorEncOptions.CredentialTypes:
							break;
						case PXEVendorEncOptions.BootItem:
							((DHCPClient)BootpdCommon.Clients[client]).RBCP.Item = BitConverter.ToUInt16(option.Data, 0).LE16();
							((DHCPClient)BootpdCommon.Clients[client]).RBCP.Layer = BitConverter.ToUInt16(option.Data, 2).LE16();

							switch (((DHCPClient)BootpdCommon.Clients[client]).RBCP.Layer)
							{
								case 0:
									Console.WriteLine("[RBCP] Layer: Client Bootfile request...");
									break;
								case 1:
									Console.WriteLine("[RBCP] Layer: Client Credential request...");
									break;
								default:
									break;
							}
							break;
						case PXEVendorEncOptions.End:
							break;
						default:
							break;
					}
				}
			}

			var encOptions = new List<DHCPOption>();
			/*
						if (Bootservers.Count != 0)
						{
							encOptions.Add(GenerateBootServersList(Bootservers));
							encOptions.Add(GenerateBootMenue(Bootservers));
							encOptions.Add(GenerateBootMenuePrompt());
						}
			*/
			return new DHCPOption(43, encOptions);
		}

		internal void Handle_WDS_Request(Guid client, ref DHCPPacket request)
		{
			var wdsData = request.GetEncOptions(250);
			foreach (var wdsOption in wdsData)
			{
				switch ((WDSNBPOptions)wdsOption.Option)
				{
					case WDSNBPOptions.Unknown:
						break;
					case WDSNBPOptions.Architecture:
						Array.Reverse(wdsOption.Data);
						((DHCPClient)BootpdCommon.Clients[client]).Arch = (Architecture)BitConverter.ToUInt16(wdsOption.Data, 0);
						break;
					case WDSNBPOptions.NextAction:
						((DHCPClient)BootpdCommon.Clients[client]).WDS.NextAction = (NextActionOptionValues)BitConverter.ToUInt16(wdsOption.Data, 0);
						break;
					case WDSNBPOptions.PollInterval:
						((DHCPClient)BootpdCommon.Clients[client]).WDS.PollInterval = BitConverter.ToUInt16(wdsOption.Data, 0);
						break;
					case WDSNBPOptions.PollRetryCount:
						((DHCPClient)BootpdCommon.Clients[client]).WDS.RetryCount = BitConverter.ToUInt16(wdsOption.Data, 0);
						break;
					case WDSNBPOptions.RequestID:
						((DHCPClient)BootpdCommon.Clients[client]).WDS.RequestId = BitConverter.ToUInt16(wdsOption.Data, 0);
						break;
					case WDSNBPOptions.Message:
						((DHCPClient)BootpdCommon.Clients[client]).WDS.AdminMessage = Encoding.ASCII.GetString(wdsOption.Data);
						break;
					case WDSNBPOptions.VersionQuery:
						break;
					case WDSNBPOptions.ServerVersion:
						((DHCPClient)BootpdCommon.Clients[client]).WDS.ServerVersion = (NBPVersionValues)BitConverter.ToUInt32(wdsOption.Data, 0);
						break;
					case WDSNBPOptions.ReferralServer:
						((DHCPClient)BootpdCommon.Clients[client]).WDS.ReferralServer = new IPAddress(wdsOption.Data);
						break;
					case WDSNBPOptions.PXEClientPrompt:
						((DHCPClient)BootpdCommon.Clients[client]).WDS.ClientPrompt = (PXEPromptOptionValues)BitConverter.ToUInt32(wdsOption.Data, 0);
						break;
					case WDSNBPOptions.PxePromptDone:
						((DHCPClient)BootpdCommon.Clients[client]).WDS.PromptDone = (PXEPromptOptionValues)BitConverter.ToUInt32(wdsOption.Data, 0);
						break;
					case WDSNBPOptions.NBPVersion:
						((DHCPClient)BootpdCommon.Clients[client]).WDS.NBPVersiopn = (NBPVersionValues)BitConverter.ToUInt32(wdsOption.Data, 0);
						break;
					case WDSNBPOptions.ActionDone:
						((DHCPClient)BootpdCommon.Clients[client]).WDS.ActionDone = BitConverter.ToBoolean(wdsOption.Data, 0);

						if (Settings.Servermode == ServerMode.AllowAll)
							((DHCPClient)BootpdCommon.Clients[client]).WDS.ActionDone = true;
						break;
					case WDSNBPOptions.AllowServerSelection:
						((DHCPClient)BootpdCommon.Clients[client]).WDS.ServerSelection = BitConverter.ToBoolean(wdsOption.Data, 0);
						break;
					case WDSNBPOptions.End:
						break;
					default:
						break;
				}
			}
		}

		internal DHCPOption Handle_WDS_Options(Guid client, ref DHCPPacket request)
		{
			var options = new List<DHCPOption>
			{
				new DHCPOption(2, BitConverter.GetBytes((byte)((DHCPClient)BootpdCommon.Clients[client]).WDS.NextAction)),
				new DHCPOption(5, BitConverter.GetBytes((byte)((DHCPClient)BootpdCommon.Clients[client]).WDS.RequestId)),
				new DHCPOption(3, BitConverter.GetBytes((byte)((DHCPClient)BootpdCommon.Clients[client]).WDS.PollInterval)),
				new DHCPOption(4, BitConverter.GetBytes((byte)((DHCPClient)BootpdCommon.Clients[client]).WDS.RetryCount))
			};


			if (((DHCPClient)BootpdCommon.Clients[client]).WDS.AllowServerSelection)
			{
				options.Add(new DHCPOption(10, BitConverter.GetBytes((byte)((DHCPClient)BootpdCommon.Clients[client]).WDS.ClientPrompt)));
				options.Add(new DHCPOption(14, BitConverter.GetBytes(((DHCPClient)BootpdCommon.Clients[client]).WDS.ServerSelection)));
			}


			switch (((DHCPClient)BootpdCommon.Clients[client]).WDS.NextAction)
			{
				case NextActionOptionValues.Drop:
					break;
				case NextActionOptionValues.Approval:
					options.Add(new DHCPOption(6, ((DHCPClient)BootpdCommon.Clients[client]).WDS.AdminMessage));
					break;
				case NextActionOptionValues.Referral:
					options.Add(new DHCPOption(14, ((DHCPClient)BootpdCommon.Clients[client]).WDS.ReferralServer.GetAddressBytes()));
					break;
				case NextActionOptionValues.Abort:
					break;
				default:
					break;
			}

			return new DHCPOption(250, options);
		}


		public void Handle_Discover_Request(Guid client, Guid socket, Packet.DHCPPacket request)
		{
			var response = request.CreateResponse(GetSocket(socket).ServerIP);
			response.Bootfile = "wdsnbp.com";
			response.CommitOptions();

			Sockets[socket].Send(((DHCPClient)BootpdCommon.Clients[client]).RemoteEndpoint, response);
		}

		public void Handle_Request_Request(Guid client, Guid socket, Packet.DHCPPacket request)
		{
			var response = request.CreateResponse(GetSocket(socket).ServerIP);
			response.Bootfile = "wdsnbp.com";
			response.CommitOptions();

			Sockets[socket].Send(((DHCPClient)BootpdCommon.Clients[client]).RemoteEndpoint, response);
		}

		public void Handle_Inform_Request(Guid client, Guid socket, Packet.DHCPPacket request)
		{
			var response = request.CreateResponse(GetSocket(socket).ServerIP);
			response.CommitOptions();

			Sockets[socket].Send(((DHCPClient)BootpdCommon.Clients[client]).RemoteEndpoint, response);
		}
	}
}
