using Bootpd.Network.Client;
using Bootpd.Network.Server;
using Server.Network;
using System;
using System.Collections.Generic;
using System.Net;

namespace Bootpd
{
	public class BootpdCommon
	{
		#region "Mutexes"
		object __LockServersMutex = new object();
		object __LockClientsMutex = new object();
		#endregion

		public static readonly Dictionary<Guid, BaseServer> Servers = new Dictionary<Guid, BaseServer>();
		public static readonly Dictionary<Guid, BaseClient> Clients = new Dictionary<Guid, BaseClient>();

		public static string TFTPRoot { get; set; }

		public BootpdCommon(string[] args)
		{
			foreach (var item in args)
			{
				var tag = "";
				var value = "";
				var line = item;

				if (line.StartsWith("/") || line.StartsWith("-"))
				{
					if (line.Contains("="))
					{
						tag = line.Substring(1, line.IndexOf("=") - 1);
						line = line.Substring(line.IndexOf("="));

						if (line.Contains(" "))
						{
							var space = line.IndexOf(" ");
							var delim = line.IndexOf("=");

							value = line.Substring(line.IndexOf("=") + 1, space - delim);

						}
						else
							value = line.Substring(line.IndexOf("=") + 1);

						Console.WriteLine("{0} = {1}", tag, value);
					}
				}
			}
		}

		public void AddServer(ServerType type)
		{
			BaseServer server = null;

			switch (type)
			{
				case ServerType.DHCP:
				case ServerType.BOOTP:
					server = new DHCPServer(type);
					break;
				case ServerType.TFTP:
					server = new TFTPServer(type);
					break;
				default:
					break;
			}

			server.ServerDataReceived += (sender, e) =>
			{

				switch (type)
				{
					case ServerType.BOOTP:
					case ServerType.DHCP:
						{
							var clientId = AddClient(type, e.RemoteEndpoint);


							var dhcpRequest = (Network.Packet.DHCPPacket)e.Data;
							if (!dhcpRequest.HasOption(60))
							{
								Console.WriteLine("[E] DHCP Option 60 (Vendor Ident): Not found!");
								return;
							}

							if (dhcpRequest.GetOption(60).Data.Length <= 9)
							{
								Console.WriteLine("[E] DHCP Option 60 (Vendor Ident): malformed!");
							}

							if (dhcpRequest.HasOption(54))
								if (new IPAddress(dhcpRequest.GetOption(54).Data) != ((IPEndPoint)Servers[e.Server]
									.GetSocket(e.Socket).LocalEndPoint).Address)
									return;

							switch ((DHCPMsgType)Convert.ToByte(dhcpRequest.GetOption(53).Data[0]))
							{
								case DHCPMsgType.Discover:
									Functions.InvokeMethod(server, "Handle_Discover_Request", new object[] { clientId, e.Socket, dhcpRequest });
									break;
								case DHCPMsgType.Request:
									Functions.InvokeMethod(server, "Handle_Request_Request", new object[] { clientId, e.Socket, dhcpRequest });
									break;
								case DHCPMsgType.Decline:
									break;
								case DHCPMsgType.Ack:
									break;
								case DHCPMsgType.Nak:
									break;
								case DHCPMsgType.Release:
									break;
								case DHCPMsgType.Inform:
									Functions.InvokeMethod(server, "Handle_Inform_Request", new object[] { clientId, e.Server, dhcpRequest });
									break;
								case DHCPMsgType.ForceRenew:
									break;
								case DHCPMsgType.LeaseQuery:
									break;
								case DHCPMsgType.LeaseUnassined:
									break;
								case DHCPMsgType.LeaseUnknown:
									break;
								case DHCPMsgType.LeaseActive:
									break;
								case DHCPMsgType.BulkLeaseQuery:
									break;
								case DHCPMsgType.LeaseQueryDone:
									break;
								case DHCPMsgType.ActiveLeaseQuery:
									break;
								case DHCPMsgType.LeasequeryStatus:
									break;
								case DHCPMsgType.Tls:
									break;
								default:
									break;
							}
						}
						break;
					case ServerType.TFTP:
						{
							var tftpRequest = (Network.Packet.TFTPPacket)e.Data;
							switch (tftpRequest.MessageType)
							{
								case Common.Network.Protocol.TFTP.TFTPMsgType.RRQ:
									Functions.InvokeMethod(server, "Handle_Read_Request", new object[] { e.Socket, tftpRequest });
									break;
								case Common.Network.Protocol.TFTP.TFTPMsgType.ACK:
									Functions.InvokeMethod(server, "Handle_Ack_Request", new object[] { e.Socket, tftpRequest });
									break;
								case Common.Network.Protocol.TFTP.TFTPMsgType.ERR:
									Functions.InvokeMethod(server, "Handle_Err_Request", new object[] { e.Socket, tftpRequest });
									break;
								default:
									break;
							}
						}
						break;
					default:
						break;
				}


			};

			Servers.Add(server.Id, server);
		}

		public Guid AddClient(ServerType type, IPEndPoint endpoint, bool local = false)
		{
			BaseClient client = null;

			switch (type)
			{
				case ServerType.BOOTP:
				case ServerType.DHCP:
					client = new Network.Client.DHCPClient(type, endpoint, local);
					break;
				case ServerType.TFTP:

					break;
				default:
					break;
			}

			Clients.Add(client.Id, client);
			return client.Id;
		}

		public void Bootstrap()
		{
			AddServer(ServerType.DHCP);
			AddServer(ServerType.BOOTP);
			AddServer(ServerType.TFTP);

			lock (__LockServersMutex)
			{
				foreach (var server in Servers.Values)
					server.Bootstrap();
			}


			lock (__LockClientsMutex)
			{
				foreach (var client in Clients.Values)
					client.Bootstrap();
			}
		}
		public void Start()
		{
			lock (__LockServersMutex)
			{
				foreach (var server in Servers.Values)
					server.Start();
			}

			lock (__LockClientsMutex)
			{
				foreach (var client in Clients.Values)
					client.Start();
			}
		}
		public void Stop()
		{
			lock (__LockServersMutex)
			{
				foreach (var server in Servers.Values)
					server.Stop();
			}

			lock (__LockClientsMutex)
			{
				foreach (var client in Clients.Values)
					client.Stop();
			}
		}

		public void Heartbeat()
		{
			lock (__LockServersMutex)
			{
				foreach (var server in Servers.Values)
					server.HeartBeat();
			}

			lock (__LockClientsMutex)
			{
				foreach (var client in Clients.Values)
					client.HeartBeat();
			}
		}

		public void Dispose()
		{
			lock (__LockServersMutex)
			{
				foreach (var server in Servers.Values)
					server.Dispose();
			}

			lock (__LockClientsMutex)
			{
				foreach (var client in Clients.Values)
					client.Dispose();
			}
		}
	}
}
