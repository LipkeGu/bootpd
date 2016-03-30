namespace bootpd
{
	using System;
	using System.Collections.Specialized;
	using System.IO;
	using System.Linq;
	using System.Text;

	public sealed class HTTP : IDisposable
	{
		HTTPSocket socket;

		public HTTP(int port)
		{
			if (!Settings.EnableHTTP)
				return;

			this.socket = new HTTPSocket(port);
			this.socket.HTTPDataReceived += this.DataReceived;
			this.socket.HTTPDataSend += this.DataSend;
		}

		~HTTP()
		{
			this.Close();
		}

		public void Dispose()
		{
			this.Close();
		}

		internal void DataSend(object source, HTTPDataSendEventArgs e)
		{
		}

		internal string ParseRequest(string url, NameValueCollection arguments, out long length)
		{
			try
			{
				var retval = url;
				if (arguments.HasKeys() && url == "/approve.html")
					if (arguments["cid"] != null && arguments["action"] != null)
					{
						var client = Exts.FromBase64(arguments["cid"]);
						if (DHCP.Clients.ContainsKey(client) && !DHCP.Clients[client].ActionDone)
						{
							if (arguments["action"] == "0")
							{
								DHCP.Clients[client].ActionDone = true;
							}
							else
							{
								DHCP.Clients[client].NextAction = Definitions.NextActionOptionValues.Abort;
								DHCP.Clients[client].ActionDone = true;
							}
						}
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

				return "http{0}".F(retval);
			}
			catch (Exception)
			{
				length = 0;
				return null;
			}
		}

		internal string HTML_header(string charset, bool errorpage = false)
		{
			var pagecontent = string.Empty;

			pagecontent += "<!DOCTYPE html>\n";
			pagecontent += "<html>\n";
			pagecontent += "\t<head>\n";
			pagecontent += "\t\t<title></title>\n";
			pagecontent += "\t\t<meta charset=\"{0}\" />\n".F(charset);
			pagecontent += "\t\t<meta http-equiv=\"expires\" content=\"0\" />\n";
			pagecontent += "\t\t<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0, user-scalable=no\">\n";

			var xmldoc = Files.ReadXML("http/DataSets/index.xml");
			if (xmldoc != null && xmldoc.HasChildNodes)
			{
				var stylessheets = xmldoc.DocumentElement.GetElementsByTagName("stylesheet");
				for (var i = 0; i < stylessheets.Count; i++)
				{
					var attributes = stylessheets[i].Attributes;
					pagecontent += "\t\t<link href =\"/Designs/[[DESIGN]]/{0}.css\" rel=\"stylesheet\" type=\"{1}\" />\n"
					.F(attributes["src"].InnerText, attributes["type"].InnerText);
				}

				var scripts = xmldoc.DocumentElement.GetElementsByTagName("script");
				for (var i = 0; i < scripts.Count; i++)
				{
					var attributes = scripts[i].Attributes;
					var path = "/scripts/{0}.js".F(attributes["src"].InnerText);
					pagecontent += "\t\t<script type=\"{0}\" src=\"{1}\"></script>\n".F(attributes["type"].InnerText, path);
				}
			}

			pagecontent += "\t</head>\n";
			pagecontent += "\t<body>\n";

			return pagecontent;
		}

		internal string HTML_footer()
		{
			var pagecontent = string.Empty;
			pagecontent += "\t</body>\n";
			pagecontent += "</html>\n";

			return pagecontent;
		}

		internal void DataReceived(object source, HTTPDataReceivedEventArgs e)
		{
			var length = 0L;
			var statuscode = 200;
			var description = "OK";
			var url = this.ParseRequest(e.Filename != null ? e.Filename : "/" , e.Arguments, out length);

			if (!Filesystem.Exist(url) && e.Method == "GET")
			{
				this.Send(new byte[0], 404, "File not Found!");
				return;
			}

			var data = new byte[length];
			var bytesRead = 0;

			Files.Read(url, ref data, out bytesRead);

			if (url.EndsWith(".htm") || url.EndsWith(".html") || url.EndsWith(".js") || url.EndsWith(".css"))
			{
				var pagecontent = string.Empty;
				if (url.EndsWith(".htm") || url.EndsWith(".html"))
				{
					pagecontent += this.HTML_header(Settings.Charset);

					if (url.EndsWith("index.html"))
					{
						pagecontent += "\t\t<div id=\"page\">\n";

						pagecontent += "\t\t\t<nav>\n";
						pagecontent += this.Generate_head_bar("index", "link");
						pagecontent += "\t\t\t</nav>\n";

						pagecontent += "\t\t<header>\n";
						pagecontent += "\t\t<h2></h2>\n";
						pagecontent += "\t\t</header>\n";
					}

					pagecontent += "\t<script type=\"text/javascript\">\n";
					pagecontent += "\tdocument.getElementById('anchor_summary').click();\n";
					pagecontent += "\t</script>\n";
					pagecontent += "\t\t\t<main>\n";
				}

				pagecontent += Encoding.UTF8.GetString(data, 0, data.Length);

				if (url.EndsWith(".htm") || url.EndsWith(".html"))
				{
					pagecontent = pagecontent.Replace("[[DESIGN]]", Settings.Design);

					pagecontent = pagecontent.Replace("[[SERVER_INFO_BLOCK]]", this.Gen_ServerInfo());

					pagecontent = pagecontent.Replace("[[SERVER_SETTINGS_BLOCK]]", this.Gen_settings_page());

					pagecontent = pagecontent.Replace("[[SERVERNAME]]", Settings.ServerName);

					if (pagecontent.Contains("[[CLIENT_BOOTP_OVERVIEW_LIST]]") && e.Headers.HasKeys() && e.Headers["Needs"] == "bootp")
					{
						var bootp_clients = this.Gen_BOOTP_client_list();
						if (bootp_clients == null)
						{
							statuscode = 800;
							description = "Keine Clients gefunden";
						}
						else
							pagecontent = pagecontent.Replace("[[CLIENT_BOOTP_OVERVIEW_LIST]]", bootp_clients);
					}

					if (pagecontent.Contains("[[CLIENT_TFTP_OVERVIEW_LIST]]") && e.Headers.HasKeys() && e.Headers["Needs"] == "tftp")
					{
						var tftp_clients = this.Gen_TFTP_client_list();
						if (tftp_clients == null)
						{
							statuscode = 800;
							description = "Keine aktiven TFTP-Sitzungen";
						}
						else
							pagecontent = pagecontent.Replace("[[CLIENT_TFTP_OVERVIEW_LIST]]", tftp_clients);
					}

					pagecontent += "\t\t\t</main>\n";
					pagecontent += "\t\t</div>\n";
					pagecontent += this.HTML_footer();
				}

				if (statuscode == 800)
					pagecontent = string.Empty;

				data = Encoding.UTF8.GetBytes(pagecontent);
				pagecontent = null;
			}

			this.Send(data, statuscode, description);
			Array.Clear(data, 0, data.Length);
		}

		internal void Send(byte[] buffer, int statuscode, string description)
		{
			this.socket.Send(buffer, statuscode, description);
		}

		internal string Generate_head_bar(string pagename, string tag)
		{
			var output = string.Empty;
			output += "\t\t\t\t<ul>\n";

			var xmldoc = Files.ReadXML("http/DataSets/{0}.xml".F(pagename).ToLowerInvariant());
			if (xmldoc != null && xmldoc.HasChildNodes)
			{
				var root = xmldoc.DocumentElement.GetElementsByTagName(tag);
				for (var i = 0; i < root.Count; i++)
				{
					var attributes = root[i].Attributes;

					if (attributes["needs"].InnerText == "tftp" && !Settings.EnableTFTP)
						continue;

					if (attributes["needs"].InnerText == "bootp" && Settings.Servermode == Definitions.ServerMode.AllowAll)
						continue;

					output += "\t\t\t\t\t<li><a href=\"/#\" onclick=\"LoadDocument('{0}', '{1}', '{2}', '{3}')\" id=\"{4}\">{2}</a></li>\n"
						.F(attributes["url"].InnerText, attributes["target"].InnerText, attributes["value"].InnerText, attributes["needs"].InnerText, attributes["ident"].InnerText);
				}
			}

			output += "\t\t\t\t</ul>\n";

			return output;
		}

		internal string Gen_BOOTP_client_list()
		{
			var output = string.Empty;
			var link = string.Empty;

			var pending_clients = (from x in DHCP.Clients where !x.Value.ActionDone select x).ToList();
			if (Settings.Servermode != Definitions.ServerMode.AllowAll && pending_clients.Count > 0)
			{
				output += "<div class=\"table\">";
				output += "<div class=\"tr\">";
				output += "<p class=\"th\">ID</p><p class=\"th\">GUID (UUID)</p><p class=\"th\">IP-Addresse</p><p class=\"th\">Approval</p>";
				output += "</div>";
				foreach (var client in pending_clients)
				{
					if (client.Value == null)
						continue;

					if (Settings.Servermode != Definitions.ServerMode.AllowAll)
					{
						link += "<a onclick=\"LoadDocument('approve.html?cid={1}&action=0', 'main', 'BOOTP Übersicht')\" href=\"/#\">Annehmen</a>\n".F(client.Value.ActionDone, Exts.ToBase64(client.Value.ID));
						link += "<a onclick=\"LoadDocument('approve.html?cid={1}&action=1', 'main', 'BOOTP Übersicht')\" href=\"/#\">Ablehen</a>\n".F(client.Value.ActionDone, Exts.ToBase64(client.Value.ID));
						output += "<div class=\"tr\">";
						output += "<p class=\"td\">{1}</p><p class=\"td\">{2}</p><p class=\"td\">{3}</p><p class=\"td\">{0}</p".F(link, DHCP.RequestID, client.Value.Guid, client.Value.EndPoint.Address);
						output += "</div>";
					}
				}

				output += "</div>";
			}
			else
				output = null;

			return output;
		}

		internal string Gen_TFTP_client_list()
		{
			var output = string.Empty;
			var active_clients = (from c in TFTP.Clients where c.Value.Stage == Definitions.TFTPStage.Transmitting select c).ToList();

			if (active_clients.Count > 0)
			{
				output += "<table id=\"nv_cbox_content\" cellspacing=\"0\">\n";
				output += "<tr><th>IP-Address</th><th>File</th><th>Blocksize (KB)</th><th>Size remaining (MB)</th></tr>\n";

				foreach (var client in active_clients)
				{
					if (client.Value == null)
						continue;

					output += "<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td>"
						.F(client.Value.EndPoint.Address, Filesystem.ResolvePath(client.Value.FileName),
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

		internal string Gen_ServerInfo()
		{
			#region "Server info"
			var output = string.Empty;
			output += "<div id=\"nv_cbox\">";
			output += "<div id=\"nv_cbox_header\">BINL-Server</div>";
			output += "<div id=\"nv_cbox_content\" style=\"width: 50%\">Servername:</div><div id=\"nv_cbox_content\" style=\"width: 50%\">{0}.{1}</div>".F(Settings.ServerName, Settings.UserDNSDomain);
			output += "<div id=\"nv_cbox_content\" style=\"width: 50%\">Endpunkt:</div><div id=\"nv_cbox_content\" style=\"width: 50%\">{0}:{1}</div>".F(Settings.ServerIP, Settings.BINLPort);
			output += "<div id=\"nv_cbox_content\" style=\"width: 50%\">Auf DHCP-Anfragen reagieren:</div><div id=\"nv_cbox_content\" style=\"width: 50%\">{0}</div>".F(Settings.EnableDHCP ? "Ja" : "Nein");

			var mode = string.Empty;
			switch (Settings.Servermode)
			{
				case Definitions.ServerMode.AllowAll:
					mode = "Allen Clients antworten.";
					break;
				case Definitions.ServerMode.KnownOnly:
					mode = "Unbekannten Clients nicht antworten.";
					break;
				default:
					mode = "Unbekannt";
					break;
			}

			output += "<div id=\"nv_cbox_content\" style=\"width: 50%\">Regel:</div><div id=\"nv_cbox_content\" style=\"width: 50%\">{0}</div>".F(mode);
			output += "</div>";
			#endregion

			#region "TFTP-Box"
			if (Settings.EnableTFTP)
			{
				output += "<div id=\"nv_cbox\">";
				output += "<div id=\"nv_cbox_header\"> TFTP-Server</div>";
				output += "<div id=\"nv_cbox_content\" style=\"width: 50%\">Endpunkt:</div><div id=\"nv_cbox_content\" style=\"width: 50%\">{0}:{1}</div>".F(Settings.ServerIP, Settings.TFTPPort);
				output += "<div id=\"nv_cbox_content\" style=\"width: 50%\">Path:</div><div id=\"nv_cbox_content\" style=\"width: 50%\">{0}</div>".F(Settings.TFTPRoot);
				output += "</div>";
			}
			#endregion

			#region "DHCP / BINL-Box"
			if (Settings.EnableDHCP)
			{
				var bootfile = Filesystem.ReplaceSlashes(Path.Combine(Settings.WDS_BOOT_PREFIX_X86, Settings.DHCP_DEFAULT_BOOTFILE));

				output += "<div id=\"nv_cbox\">";
				output += "<div id=\"nv_cbox_header\">DHCP-Listener</div>";
				output += "<div id=\"nv_cbox_content\" style=\"width: 50%\">Bootfile:</div><div id=\"nv_cbox_content\" style=\"width: 50%\">[TFTP-Root]{0}</div>".F(bootfile);
				output += "<div id=\"nv_cbox_content\" style=\"width: 50%\">Requests:</div><div id=\"nv_cbox_content\" style=\"width: 50%\">{0}</div>".F(DHCP.RequestID);
				output += "</div>";

				#region "PXE Serverlist"
				if (Settings.AdvertPXEServerList)
				{
					var servers = (from s in DHCP.Servers where s.Value.Hostname != Settings.ServerName select s).ToList();
					if (servers.Count > 0)
					{
						output += "<div id=\"nv_cbox\">";
						output += "<div id=\"nv_cbox_header\">Boot-Serverliste des Servers</div>";

						foreach (var server in servers)
						{
							output += "<div id=\"nv_cbox_content\" style=\"width: 50%\">{0} ({1})</div>".F(server.Value.Hostname, server.Value.IPAddress);
							output += "<div id=\"nv_cbox_content\" style=\"width: 50%\">{0}</div>".F(server.Value.Type);
						}

						output += "</div>";
					}
				}
				#endregion
			}
			#endregion

			return output;
		}

		internal string Gen_settings_page()
		{
			var output = string.Empty;

			output += "<form action=\"settings.html\" method=\"POST\">";

			#region "Server Modus"
			output += "<div id=\"nv_cbox\">";
			output += "<div id=\"nv_cbox_header\">Server Modus</div>";

			switch (Settings.Servermode)
			{
				case Definitions.ServerMode.AllowAll:
					output += "<div id=\"nv_cbox_content\" style =\"width: 100%\"><input type=\"radio\" name=\"servermode\" id=\"allowall\" value=\"allowall\" checked /> <label for=\"allowall\">Unbekannten Clients antworten</label></div>";
					output += "<div id=\"nv_cbox_content\" style =\"width: 100%\"><input type=\"radio\" name=\"servermode\" id=\"knownonly\" value=\"knownonly\" /> <label for=\"knownonly\">Unbekannten Clients nicht antworten</label></div>";
					break;
				case Definitions.ServerMode.KnownOnly:
					output += "<div id=\"nv_cbox_content\" style =\"width: 100%\"><input type=\"radio\" name=\"servermode\" id=\"allowall\" value=\"allowall\" /> <label for=\"allowall\">Unbekannten Clients antworten</label></div>";
					output += "<div id=\"nv_cbox_content\" style =\"width: 100%\"><input type=\"radio\" name=\"servermode\" id=\"knownonly\" value=\"knownonly\" checked /> <label for=\"knownonly\">Unbekannten Clients nicht antworten</label></div>";
					break;
				default:
					break;
			}

			output += "</div>";
			#endregion

			#region "RIS Settings"
			output += "<div id=\"nv_cbox\">";
			output += "<div id=\"nv_cbox_header\">OSChooser</div>";
			output += "<div id=\"nv_cbox_content\" style=\"width: 50%\"><label for=\"osc_welcome_file\">Welcome File:</label></div>";
			output += "<div id=\"nv_cbox_content\" style=\"width: 50%\"><input name=\"osc_welcome_file\" id=\"osc_welcome_file\" maxlength=\"8\" value=\"{0}\"/></div>".F(Settings.OSC_DEFAULT_FILE);
			output += "</div>";
			#endregion

			#region "DHCP & BINL"
			output += "<div id=\"nv_cbox\">";
			output += "<div id=\"nv_cbox_header\">DHCP-/BINL-Server</div>";
			output += "<div id=\"nv_cbox_content\" style=\"width: 50%\"><label for=\"pxe_enable_dhcp\">Port 67 nicht abhören</label></div>";
			output += "<div id=\"nv_cbox_content\" style=\"width: 50%\"><input type=\"checkbox\" name=\"pxe_enable_dhcp\" id=\"pxe_enable_dhcp\" /></div>";

			output += "<div id=\"nv_cbox_content\" style=\"width: 50%\"><label for=\"pxe_menu_prompt\">PXE Menu prompt:</label></div>";
			output += "<div id=\"nv_cbox_content\" style=\"width: 50%\"><textarea name=\"pxe_menu_prompt\" id=\"pxe_menu_prompt\" maxlength=\"250\"/>{0}</textarea></div>".F(Settings.DHCP_MENU_PROMPT);
			output += "<div id=\"nv_cbox_content\" style=\"width: 50%\"><label for=\"dhcp_bootfile\">DHCP Bootfile:([TFTPRoot]/Boot/[Arch]/)</label></div>";
			output += "<div id=\"nv_cbox_content\" style=\"width: 50%\"><input type=\"text\" name=\"dhcp_bootfile\" id=\"dhcp_bootfile\" maxlength=\"256\" value=\"{0}\"/></div>".F(Settings.DHCP_DEFAULT_BOOTFILE);

			output += "</div>";
			#endregion

			output += "<div id=\"nv_cbox_header\" style=\"width: 100px\"> <input type=\"submit\" value=\"Speichern\"/></div>";

			output += "</form>";

			return output;
		}

		private void Close()
		{
			this.socket.Dispose();
		}
	}
}