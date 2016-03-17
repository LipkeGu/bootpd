using System;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using static WDSServer.Definitions;

namespace WDSServer.Network
{
	sealed public class HTTP : IDisposable
	{
		HTTPSocket socket;

		public HTTP(int port)
		{
			this.socket = new HTTPSocket(Settings.ServerName, port);
			this.socket.HTTPDataReceived += DataReceived;
			this.socket.HTTPDataSend += DataSend;
		}

		internal void DataSend(object source, HTTPDataSendEventArgs e)
		{
		}

		internal string parseRequest(string url, NameValueCollection arguments, out long length)
		{
			try
			{
				var retval = url;

				if (arguments.HasKeys() && url == "/approve.html")
					if (arguments["cid"] != null)
					{
						var client = Exts.FromBase64(arguments["cid"]);
						if (DHCP.Clients.ContainsKey(client) && !DHCP.Clients[client].ActionDone)
							DHCP.Clients[client].ActionDone = true;
					}

				if (retval == "/approve.html")
					retval = "/requests.html";

				if (retval == "/")
					retval = "/index.html";

				if (!retval.EndsWith(".htm") && !retval.EndsWith(".html") && !retval.EndsWith(".js") &&
					!retval.EndsWith(".css") && !retval.EndsWith(".png") && !retval.EndsWith(".gif") && !retval.EndsWith(".ico"))
					throw new Exception("Unsupportet Content type!");

				var size = Filesystem.Size("http{0}".F(retval));
				length = size;

				if (size > Settings.MaxAllowedFileLength) // 10 MB
					throw new Exception("Maximum allowed Size exceeded!");

				length = size;

				return "http{0}".F(retval);
			}
			catch (Exception)
			{
				length = 0;
				return null;
			}
		}

		internal void DataReceived(object source, HTTPDataReceivedEventArgs e)
		{
			var length = 0L;
			var statuscode = 200;
			var description = "OK";

			var url = parseRequest(e.Filename, e.Arguments, out length);

			if (url == null)
				return;

			if (Filesystem.Exist(url))
			{
				var data = new byte[length];
				var bytesRead = 0;

				Files.Read(url, ref data, out bytesRead);

				if (url.EndsWith(".htm") || url.EndsWith(".html") || url.EndsWith(".js") || url.EndsWith(".css"))
				{
					var pagecontent = string.Empty;


					if (url.EndsWith(".htm") || url.EndsWith(".html"))
					{
						#region "HTML Header"
						pagecontent += "<!DOCTYPE html>\n";
						pagecontent += "<html>\n";
						pagecontent += "\t<head>\n";
						pagecontent += "\t\t<title>[[SERVERNAME]]</title>\n";
						pagecontent += "\t\t<meta charset=\"{0}\" />\n".F(Settings.Charset);
						pagecontent += "\t\t<meta http-equiv=\"expires\" content=\"0\" />\n";
						pagecontent += "\t\t<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0, user-scalable=no\">\n";
						pagecontent += "\t\t<link href=\"style.css\" rel=\"stylesheet\" type=\"text/css\" />\n";

						var xmldoc = Files.ReadXML("http/DataSets/index.xml");
						if (xmldoc != null && xmldoc.HasChildNodes)
						{
							var root = xmldoc.DocumentElement.GetElementsByTagName("script");
							for (var i = 0; i < root.Count; i++)
							{
								var attributes = root[i].Attributes;
								pagecontent += "\t\t<script type=\"{0}\" src=\"scripts/{1}.js\"></script>\n".F(attributes["type"].InnerText, attributes["src"].InnerText);
							}
						}

						pagecontent += "\t</head>\n";
						#endregion

						pagecontent += "\t<body>\n";
						if (url.EndsWith("index.html"))
						{
							pagecontent += "\t\t<section id=\"nav\">\n";

							pagecontent += generate_head_bar("index", "link");

							pagecontent += "\t\t</section>\n";
						}

						pagecontent += "\t\t<section id=\"main\">\n";
						pagecontent += "\t\t</section>\n";
					}

					pagecontent += Encoding.UTF8.GetString(data, 0, data.Length);

					if (url.EndsWith(".htm") || url.EndsWith(".html"))
					{
						pagecontent = pagecontent.Replace("[[SERVER_INFO_BLOCK]]", gen_ServerInfo());
						pagecontent = pagecontent.Replace("[[SERVERNAME]]", Settings.ServerName);

						if (pagecontent.Contains("[[CLIENT_BOOTP_OVERVIEW_LIST]]"))
						{
							var bootp_clients = gen_BOOTP_client_list();
							if (bootp_clients == null)
							{
								statuscode = 800;
								description = "Keine Clients gefunden";
							}
							else
								pagecontent = pagecontent.Replace("[[CLIENT_BOOTP_OVERVIEW_LIST]]", bootp_clients);
						}

						if (pagecontent.Contains("[[CLIENT_TFTP_OVERVIEW_LIST]]"))
						{
							var tftp_clients = gen_TFTP_client_list();
							if (tftp_clients == null)
							{
								statuscode = 800;
								description = "Keine aktiven TFTP-Sessions";
							}
							else
								pagecontent = pagecontent.Replace("[[CLIENT_TFTP_OVERVIEW_LIST]]", tftp_clients);
						}

						#region "HTML Footer"
						pagecontent += "\t</body>\n";
						pagecontent += "</html>\n";
						#endregion
					}

					if (statuscode == 800)
						pagecontent = string.Empty;

					data = Encoding.UTF8.GetBytes(pagecontent);
					Send(data, statuscode, description);
					pagecontent = null;
				}
				else
				{
					Send(data, statuscode, description);
				}
				Array.Clear(data, 0, data.Length);
			}
		}

		internal void Send(byte[] buffer, int statuscode, string description)
		{
			this.socket.Send(buffer, statuscode, description);
		}

		internal string generate_head_bar(string pagename, string tag)
		{
			var output = string.Empty;

			output += "<nav id=\"nav\">\n";
			output += "<ul>\n";

			var xmldoc = Files.ReadXML("http/DataSets/{0}.xml".F(pagename).ToLowerInvariant());
			if (xmldoc != null && xmldoc.HasChildNodes)
			{
				var root = xmldoc.DocumentElement.GetElementsByTagName(tag);
				for (var i = 0; i < root.Count; i++)
				{
					var attributes = root[i].Attributes;
					output += "<li><a href=\"/#\" onclick=\"LoadDocument('{0}', '{1}', '{2}', '{3}')\">{2}</a></li>\n"
						.F(attributes["url"].InnerText, attributes["target"].InnerText, attributes["value"].InnerText, attributes["needs"].InnerText);
				}
			}

			output += "</ul>\n";
			output += "</nav>\n";

			return output;
		}

		internal string gen_BOOTP_client_list()
		{
			var output = string.Empty;

			var pending_clients = (from x in DHCP.Clients where !x.Value.ActionDone select x).ToList();

			if (pending_clients.Count > 0)
			{
				var size = "width: 25%;";
				if (DHCP.Mode != ServerMode.AllowAll)
					output += "<div id=\"th\">ID</div><div id=\"th\">GUID (UUID)</div><div id=\"th\">IP-Address</div><div id=\"th\">Approval</div>";

				var link_approval = string.Empty;

				foreach (var client in pending_clients)
				{
					if (DHCP.Mode != ServerMode.AllowAll)
						link_approval = "<a onclick=\"LoadDocument('approve.html?cid={1}', 'main', 'GET')\" href=\"#\">{0}</a>\n".F(client.Value.ActionDone, Exts.toBase64(client.Value.ID));
					else
						link_approval = string.Empty;

					output += "<div id=\"td\">{1}</div><div id=\"td\">{2}</div><div id=\"td\">{3}</div><div id=\"td\">{4}</div>"
					.F(size, client.Value.RequestID, client.Value.Guid, client.Value.EndPoint.Address, link_approval);
				}
			}
			else
				output = null;

			return output;
		}

		internal string gen_TFTP_client_list()
		{
			var output = string.Empty;
			var active_clients = (from c in TFTP.Clients where c.Value.Stage == TFTPStage.Transmitting select c).ToList();

			if (active_clients.Count > 0)
			{
				output += "<table id=\"overview\" cellspacing=\"0\">\n";
				output += "<tr><th>IP-Address</th><th>File</th><th>Blocksize (KB)</th><th>Size remaining (MB)</th></tr>\n";

				foreach (var client in active_clients)
				{
					output += "<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td>"
							 .F(client.Value.EndPoint.Address,
							 Filesystem.StripRoot(client.Value.FileName),
							 Math.Round((double)(client.Value.BlockSize / 1024), 2),
							 Math.Round((double)(client.Value.TransferSize / 1024) / 1024, 2));
					output += "</tr>\n";
				}

				output += "</table>\n";
			}
			else
				output = null;

			return output;
		}

		internal string gen_ServerInfo()
		{
			var output = string.Empty;
			output += "<table>\n";

			output += "<thead>\n";
			output += "<tr><th colspan=\"2\">BINL-Server</th></tr>\n";
			output += "</thead>\n";

			output += "<tbody id=\"overview\">\n";
			output += "<tr><td>Servername:</td><td>{0}.{1}</td></tr>\n".F(Settings.ServerName, Settings.UserDNSDomain);
			output += "<tr><td>Endpunkt:</td><td>{0}:{1}</td></tr>\n".F(Settings.ServerIP, Settings.BINLPort);
			output += "<tr><td>Auf DHCP-Anfragen reagieren:</td><td>{0}</td></tr>\n".F(Settings.enableDHCP ? "Ja" : "Nein");


			var mode = string.Empty;
			switch (DHCP.Mode)
			{
				case ServerMode.AllowAll:
					mode = "Allen Clients antworten.";
					break;
				case ServerMode.KnownOnly:
					mode = "Unbekannten Clients nicht antworten.";
					break;
				default:
					mode = "Unbekannt";
					break;
			}

			output += "<tr><td>Regel:</td><td>{0}</td></tr>\n".F(mode);
			output += "</tbody>\n";
			output += "</table><br />\n";

			if (Settings.enableTFTP)
			{
				output += "<table>\n";
				output += "<thead>\n";
				output += "<tr><th colspan=\"2\">TFTP-Server</th></tr>\n";
				output += "</thead>\n";
				output += "<tbody id=\"overview\">\n";
				output += "<tr><td>Endpunkt: </td><td>{0}:{1}</td></tr>\n".F(Settings.ServerIP, Settings.TFTPPort);
				output += "<tr><td>Directory: </td><td>{0}</td></tr>\n".F(Settings.TFTPRoot);
				output += "</tbody>\n";

				output += "</table><br />\n";
			}

			return output;
		}

		private void Close()
		{
			this.socket.Dispose();
		}

		public void Dispose()
		{
			Close();
		}

		~HTTP()
		{
			Close();
		}
	}
}