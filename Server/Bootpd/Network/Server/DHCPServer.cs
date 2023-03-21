using bootpd.Bootpd.Common.Network.Protocol.DHCP;
using Bootpd.Common;
using Bootpd.Common.Network.Protocol.DHCP;
using Bootpd.Network.Client;
using Bootpd.Network.Packet;
using Server.Extensions;
using Server.Network;
using System;
using System.Collections.Generic;
using System.IO;

namespace Bootpd.Network.Server
{
	public sealed class DHCPServer : BaseServer
	{
		public DHCPServer(ServerType serverType) : base(serverType)
		{

		}

		private DHCPOption Handle_RBCP_Request(string client, DHCPPacket request)
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
						((DHCPClient)BootpdCommon.Clients[client]).RBCP.Item = option.AsUInt16();
						((DHCPClient)BootpdCommon.Clients[client]).RBCP.Layer = option.AsUInt16();

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

		internal void Handle_WDS_Request(string client, ref DHCPPacket request)
		{
			if (!BootpdCommon.Clients.ContainsKey(client))
				return;

			var wdsData = request.GetEncOptions(250);
			foreach (var wdsOption in wdsData)
			{

				Console.WriteLine("WDS Request Option: {0}", (WDSNBPOptions)wdsOption.Option);

				switch ((WDSNBPOptions)wdsOption.Option)
				{
					case WDSNBPOptions.Unknown:
						break;
					case WDSNBPOptions.Architecture:
						((DHCPClient)BootpdCommon.Clients[client]).Arch = (Architecture)wdsOption.AsUInt16();
						break;
					case WDSNBPOptions.NextAction:
						((DHCPClient)BootpdCommon.Clients[client]).WDS.NextAction = (NextActionOptionValues)wdsOption.AsByte();
						break;
					case WDSNBPOptions.PollInterval:
						((DHCPClient)BootpdCommon.Clients[client]).WDS.PollInterval = wdsOption.AsUInt16();
						break;
					case WDSNBPOptions.PollRetryCount:
						((DHCPClient)BootpdCommon.Clients[client]).WDS.RetryCount = wdsOption.AsUInt16();
						break;
					case WDSNBPOptions.RequestID:
						((DHCPClient)BootpdCommon.Clients[client]).WDS.RequestId = wdsOption.AsUInt32();
						break;
					case WDSNBPOptions.Message:
						((DHCPClient)BootpdCommon.Clients[client]).WDS.AdminMessage = wdsOption.AsString();
						break;
					case WDSNBPOptions.VersionQuery:
						((DHCPClient)BootpdCommon.Clients[client]).WDS.VersionQery = true;
						break;
					case WDSNBPOptions.ServerVersion:
						((DHCPClient)BootpdCommon.Clients[client]).WDS.ServerVersion = (NBPVersionValues)wdsOption.AsUInt32();
						break;
					case WDSNBPOptions.ReferralServer:
						((DHCPClient)BootpdCommon.Clients[client]).WDS.ReferralServer = wdsOption.AsIPAddress();
						break;
					case WDSNBPOptions.PXEClientPrompt:
						((DHCPClient)BootpdCommon.Clients[client]).WDS.ClientPrompt = (PXEPromptOptionValues)wdsOption.AsByte();
						break;
					case WDSNBPOptions.PxePromptDone:
						((DHCPClient)BootpdCommon.Clients[client]).WDS.PromptDone = (PXEPromptOptionValues)wdsOption.AsByte();
						break;
					case WDSNBPOptions.NBPVersion:
						((DHCPClient)BootpdCommon.Clients[client]).WDS.NBPVersiopn = (NBPVersionValues)wdsOption.AsUInt16();

						switch (((DHCPClient)BootpdCommon.Clients[client]).WDS.NBPVersiopn)
						{
							case NBPVersionValues.Seven:
								Console.WriteLine("NBP Version: 7!");
								break;
							case NBPVersionValues.Eight:
								Console.WriteLine("NBP Version: 8!");
								break;
							case NBPVersionValues.Unknown:
								Console.WriteLine("NBP Version: Unknown!");
								break;
							default:
								Console.WriteLine("NBP Version: {0}", (NBPVersionValues)wdsOption.AsUInt16());
								break;
						}

						break;
					case WDSNBPOptions.ServerFeatures:
						((DHCPClient)BootpdCommon.Clients[client]).WDS.ServerFeatures = wdsOption.AsUInt32();
						break;
					case WDSNBPOptions.ActionDone:
						((DHCPClient)BootpdCommon.Clients[client]).WDS.ActionDone = wdsOption.AsBool();

						if (Settings.Servermode == ServerMode.AllowAll)
							((DHCPClient)BootpdCommon.Clients[client]).WDS.ActionDone = true;
						break;
					case WDSNBPOptions.AllowServerSelection:
						((DHCPClient)BootpdCommon.Clients[client]).WDS.ServerSelection = wdsOption.AsBool();
						break;
					case WDSNBPOptions.End:
						break;
					default:
						break;
				}
			}
		}

		public static void SelectBootFile(out string bootFile, out string bcdPath, bool isWDS, bool actionDone, NextActionOptionValues nextaction, Architecture arch)
		{
			var bootfile = string.Empty;
			var bcdpath = string.Empty;

			if (isWDS)
			{
				switch (arch)
				{
					case Architecture.Intelx86PC:
						if (nextaction == NextActionOptionValues.Approval)
						{
							bootfile = Path.Combine(Settings.WDS_BOOT_PREFIX_X86, !actionDone ? Settings.WDS_BOOTFILE_X86 : Settings.WDS_BOOTFILE_x86_Done);
							bcdpath = Path.Combine(Settings.WDS_BOOT_PREFIX_X86, Settings.WDS_BCD_FileName);
						}
						else
							bootfile = Path.Combine(Settings.WDS_BOOT_PREFIX_X86, Settings.WDS_BOOTFILE_ABORT);

						break;
					case Architecture.EFIItanium:
						if (nextaction == NextActionOptionValues.Approval)
						{
							bootfile = Path.Combine(Settings.WDS_BOOT_PREFIX_IA64, !actionDone ? Settings.WDS_BOOTFILE_IA64 : Settings.WDS_BOOTFILE_IA64_Done);
							bcdpath = Path.Combine(Settings.WDS_BOOT_PREFIX_IA64, Settings.WDS_BCD_FileName);
						}
						else
							bootfile = Path.Combine(Settings.WDS_BOOT_PREFIX_IA64, Settings.WDS_BOOTFILE_ABORT);

						break;
					case Architecture.EFIx8664:
						if (nextaction == NextActionOptionValues.Approval)
						{
							bootfile = Path.Combine(Settings.WDS_BOOT_PREFIX_X64, !actionDone ? Settings.WDS_BOOTFILE_X64 : Settings.WDS_BOOTFILE_x64_Done);
							bcdpath = Path.Combine(Settings.WDS_BOOT_PREFIX_X64, Settings.WDS_BCD_FileName);
						}
						else
							bootfile = Path.Combine(Settings.WDS_BOOT_PREFIX_X64, Settings.WDS_BOOTFILE_ABORT);

						break;
					case Architecture.EFIBC:
						if (nextaction == NextActionOptionValues.Approval)
						{
							bootfile = Path.Combine(Settings.WDS_BOOT_PREFIX_EFI, !actionDone ? Settings.WDS_BOOTFILE_EFI : Settings.WDS_BOOTFILE_EFI_Done);
							bcdpath = Path.Combine(Settings.WDS_BOOT_PREFIX_EFI, Settings.WDS_BCD_FileName);
						}
						else
							bootfile = Path.Combine(Settings.WDS_BOOT_PREFIX_EFI, Settings.WDS_BOOTFILE_ABORT);

						break;
					default:
						bootfile = Path.Combine(Settings.WDS_BOOT_PREFIX_X86, !actionDone ? Settings.WDS_BOOTFILE_X86 : Settings.WDS_BOOTFILE_x86_Done);
						bcdpath = Path.Combine(Settings.WDS_BOOT_PREFIX_X86, Settings.WDS_BCD_FileName);
						break;
				}
			}
			else
			{
				switch (arch)
				{
					case Architecture.EFIItanium:
						bootfile = Path.Combine("/Boot/ia64/", "wdsmgfw.efi");
						break;
					case Architecture.EFIx8664:
						bootfile = Path.Combine("/Boot/x64/", "wdsnbp.com");
						break;
					case Architecture.EFIBC:
						bootfile = Path.Combine("/Boot/efi/", "wdsmgfw.efi");
						break;
					case Architecture.Intelx86PC:
					default:
						bootfile = Path.Combine("/Boot/x86/", "wdsnbp.com");
						break;
				}
			}

			bootFile = Filesystem.ReplaceSlashes(bootfile);
			bcdPath = Filesystem.ReplaceSlashes(bcdpath);
		}

		internal DHCPOption Handle_WDS_Options(string client, ref DHCPPacket request)
		{
			var options = new List<DHCPOption>
			{
				new DHCPOption((byte)WDSNBPOptions.NextAction, (byte)((DHCPClient)BootpdCommon.Clients[client]).WDS.NextAction),
				new DHCPOption((byte)WDSNBPOptions.RequestID, ((DHCPClient)BootpdCommon.Clients[client]).WDS.RequestId),
				new DHCPOption((byte)WDSNBPOptions.PollInterval, ((DHCPClient)BootpdCommon.Clients[client]).WDS.PollInterval),
				new DHCPOption((byte)WDSNBPOptions.PollRetryCount, ((DHCPClient)BootpdCommon.Clients[client]).WDS.RetryCount),
				new DHCPOption((byte)WDSNBPOptions.PXEClientPrompt, (byte)((DHCPClient)BootpdCommon.Clients[client]).WDS.PromptDone),
				new DHCPOption((byte)WDSNBPOptions.ActionDone, Convert.ToByte(((DHCPClient)BootpdCommon.Clients[client]).WDS.ActionDone))
			};


			if (((DHCPClient)BootpdCommon.Clients[client]).WDS.AllowServerSelection)
			{
				options.Add(new DHCPOption((byte)WDSNBPOptions.PXEClientPrompt, (byte)((DHCPClient)BootpdCommon.Clients[client]).WDS.ClientPrompt));
				options.Add(new DHCPOption((byte)WDSNBPOptions.AllowServerSelection, Convert.ToByte(((DHCPClient)BootpdCommon.Clients[client]).WDS.ServerSelection)));
			}

			switch (((DHCPClient)BootpdCommon.Clients[client]).WDS.NextAction)
			{
				case NextActionOptionValues.Drop:
					break;
				case NextActionOptionValues.Approval:
					options.Add(new DHCPOption((byte)WDSNBPOptions.Message, ((DHCPClient)BootpdCommon.Clients[client]).WDS.AdminMessage));
					break;
				case NextActionOptionValues.Referral:
					options.Add(new DHCPOption((byte)WDSNBPOptions.ReferralServer, ((DHCPClient)BootpdCommon.Clients[client]).WDS.ReferralServer));
					break;
				case NextActionOptionValues.Abort:
					break;
				default:
					break;
			}


			foreach (DHCPOption option in options)
			{
				Console.WriteLine("[D]: WDS Option: {0}", (WDSNBPOptions)option.Option);
			}

			return new DHCPOption(250, options);
		}


		public void Handle_Discover_Request(string client, string socket, DHCPPacket request)
		{
			var response = request.CreateResponse(GetSocket(socket).IPAddress);
			response.CommitOptions();
			var filename = Path.Combine(Environment.CurrentDirectory, string.Format("DHCP-{0}-{1}-dump.hex", client, request.BootpMsgType));
			request.Dump(filename);

			var arch = (Architecture)request.GetOption(93).AsUInt16();
			((DHCPClient)BootpdCommon.Clients[client]).Arch = arch;



			Sockets[socket].Send(((DHCPClient)BootpdCommon.Clients[client]).RemoteEndpoint, response);
		}

		public void Handle_Request_Request(string client, string socket, DHCPPacket request)
		{
			var response = request.CreateResponse(GetSocket(socket).IPAddress);

			if (request.HasOption(250))
			{
				((DHCPClient)BootpdCommon.Clients[client]).CreateWDSClient();

				Handle_WDS_Request(client, ref request);
				response.AddOption(Handle_WDS_Options(client, ref request));
			}

			var bootfile = string.Empty;
			var bcdfile = string.Empty;

			var arch = (Architecture)request.GetOption(93).AsUInt16();
			((DHCPClient)BootpdCommon.Clients[client]).Arch = arch;

			var actionDone = ((DHCPClient)BootpdCommon.Clients[client]).IsWDSClient() ? ((DHCPClient)BootpdCommon.Clients[client]).WDS.ActionDone : true;

			SelectBootFile(out bootfile, out bcdfile, ((DHCPClient)BootpdCommon.Clients[client]).IsWDSClient(), actionDone, NextActionOptionValues.Approval, arch);

			response.Bootfile = bootfile;
			response.AddOption(new DHCPOption(252, bcdfile));


			response.CommitOptions();
			Sockets[socket].Send(((DHCPClient)BootpdCommon.Clients[client]).RemoteEndpoint, response);
		}

		public void Handle_Inform_Request(string client, string socket, DHCPPacket request)
		{
			var response = request.CreateResponse(GetSocket(socket).IPAddress);
			response.CommitOptions();

			Sockets[socket].Send(((DHCPClient)BootpdCommon.Clients[client]).RemoteEndpoint, response);
		}
	}
}
