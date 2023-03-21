using Bootpd.Common;
using Bootpd.Common.Network.Protocol.DHCP;
using Bootpd.Network.Client;
using Bootpd.Network.Server;
using Server.Extensions;
using Server.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
namespace Bootpd
{
	public class BootpdCommon
	{
		#region "Mutexes"
		object __LockServersMutex = new object();
		object __LockClientsMutex = new object();
		#endregion

		public static readonly Dictionary<string, BaseServer> Servers = new Dictionary<string, BaseServer>();
		public static readonly Dictionary<string, BaseClient> Clients = new Dictionary<string, BaseClient>();

		public static string TFTPRoot { get; set; } = Settings.TFTPRoot;

		public BootpdCommon(string[] args)
		{
			TFTPRoot = Filesystem.ResolvePath(TFTPRoot);

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
				var clientid = string.Empty;
				var endpoint = e.RemoteEndpoint;

				switch (type)
				{
					case ServerType.BOOTP:
					case ServerType.DHCP:
						{
							clientid = AddClient(Guid.NewGuid().ToString(), type, endpoint);
							var dhcpRequest = (Network.Packet.DHCPPacket)e.Data;
							if (dhcpRequest.GetOption(60).AsString().Contains("MSFT"))
								return;

							switch ((DHCPMsgType)dhcpRequest.GetOption(53).AsByte())
							{
								case DHCPMsgType.Discover:
									Functions.InvokeMethod(server, "Handle_Discover_Request", new object[] { clientid, e.Socket, dhcpRequest });
									break;
								case DHCPMsgType.Request:
									Functions.InvokeMethod(server, "Handle_Request_Request", new object[] { clientid, e.Socket, dhcpRequest });
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
									Functions.InvokeMethod(server, "Handle_Inform_Request", new object[] { clientid, e.Server, dhcpRequest });
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

						Clients.Remove(clientid);
						break;
					case ServerType.TFTP:
						{

							clientid = AddClient(endpoint.Address.ToString(), type, endpoint);
							var tftpRequest = (Network.Packet.TFTPPacket)e.Data;
							switch (tftpRequest.MessageType)
							{
								case Common.Network.Protocol.TFTP.TFTPMsgType.RRQ:
									Console.WriteLine("TFTP : Got Read Request from {0}", endpoint);
									Functions.InvokeMethod(server, "Handle_Read_Request", new object[] { clientid, e.Socket, tftpRequest });
									break;
								case Common.Network.Protocol.TFTP.TFTPMsgType.ACK:
									Functions.InvokeMethod(server, "Handle_Ack_Request", new object[] { clientid, e.Socket, tftpRequest });
									break;
								case Common.Network.Protocol.TFTP.TFTPMsgType.ERR:
									Functions.InvokeMethod(server, "Handle_Error_Request", new object[] { clientid, e.Socket, tftpRequest });
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

		public string AddClient(string id, ServerType type, IPEndPoint endpoint, bool local = false)
		{
			BaseClient client = null;

			if (!Clients.ContainsKey(id))
			{
				switch (type)
				{
					case ServerType.BOOTP:
					case ServerType.DHCP:
						client = new DHCPClient(id, type, endpoint, local);
						break;
					case ServerType.TFTP:
						client = new TFTPClient(id, type, endpoint, local);
						break;
					default:
						break;
				}

				Clients.Add(client.Id, client);
			}
			else
			{
				Clients[id].RemoteEndpoint = endpoint;
				Clients[id].LocalInstance = local;
			}

			return Clients[id].Id;
		}

		public void Bootstrap()
		{
			AddServer(ServerType.DHCP);
			AddServer(ServerType.BOOTP);
			AddServer(ServerType.TFTP);

			lock (__LockServersMutex)
			{
				for (var i = Servers.Values.Count - 1; i >= 0; i--)
					Servers.Values.ElementAt(i).Bootstrap();
			}

			//	AddClient(ServerType.DHCP, new IPEndPoint(IPAddress.Any, 68), true);

			lock (__LockClientsMutex)
			{
				for (var i = Clients.Values.Count - 1; i >= 0; i--)
					Clients.Values.ElementAt(i).Bootstrap();
			}
		}

		public void Start()
		{
			lock (__LockServersMutex)
			{
				for (var i = Servers.Values.Count - 1; i >= 0; i--)
					Servers.Values.ElementAt(i).Start();
			}

			lock (__LockClientsMutex)
			{
				for (var i = Clients.Values.Count - 1; i >= 0; i--)
					Clients.Values.ElementAt(i).Start();
			}
		}

		public void Stop()
		{
			lock (__LockServersMutex)
			{
				for (var i = Servers.Values.Count - 1; i >= 0; i--)
					Servers.Values.ElementAt(i).Stop();
			}

			lock (__LockClientsMutex)
			{
				for (var i = Clients.Values.Count - 1; i >= 0; i--)
					Clients.Values.ElementAt(i).Stop();

			}
		}

		public void Heartbeat()
		{
			lock (__LockServersMutex)
			{
				for (var i = Servers.Values.Count - 1; i >= 0; i--)
					Servers.Values.ElementAt(i).HeartBeat();
			}

			lock (__LockClientsMutex)
			{

				for (var i = Clients.Values.Count - 1; i >= 0; i--)
					Clients.Values.ElementAt(i).HeartBeat();
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
				for (var i = Clients.Count - 1; i >= 0; i--)
				{
					var id = Clients.ElementAt(i).Key;
					Clients.ElementAt(i).Value.Dispose();
					Clients.Remove(id);
				}
			}
		}
	}
}
