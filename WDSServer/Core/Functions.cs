namespace WDSServer
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Net;
	using System.Text;
	using WDSServer.Network;

	public static class Functions
	{
		public static sbyte GetTFTPOPCode(TFTPPacket packet) => GetTFTPOPCode(packet.Data);

		public static sbyte GetTFTPOPCode(byte[] packet) => Convert.ToSByte(packet[1]);

		public static bool IsTFTPOPCode(sbyte opcode, byte[] packet)
		{
			var code = GetTFTPOPCode(packet);

			return code != opcode ? false : true;
		}

		public static int FindEndOption(ref byte[] data)
		{
			var pos = data.Length;
			for (var i = pos - 1; i > 0; i--)
				if (data[i] == byte.MaxValue)
				{
					pos = i + 1;
					break;
				}

			return pos;
		}

		public static byte[] Unpack_Packet(byte[] packet)
		{
			var data = new byte[(packet.Length - 8)];
			Array.Copy(packet, 8, data, 0, data.Length);

			return data;
		}

		public static byte[] Pack_Packet(byte[] data)
		{
			var packet = new byte[(data.Length + 8)];
			Array.Copy(data, 0, packet, 8, data.Length);

			return data;
		}

		public static int GetOptionOffset(ref DHCPPacket packet, Definitions.DHCPOptionEnum option)
		{
			var pos = 0;
			for (var i = 0; i < packet.Data.Length; i++)
				if (packet.Data[i] == (int)option)
				{
					pos = i;
					break;
				}

			return pos;
		}

		public static bool IsTFTPOPCode(sbyte opcode, TFTPPacket packet) => IsTFTPOPCode(opcode, packet.Data);

		/// <summary>
		/// Copy an Array into another to the desired position
		/// </summary>
		/// <param name="src">Source</param>
		/// <param name="srcoffset">Source Index</param>
		/// <param name="dst">Target</param>
		/// <param name="dstoffset">Target Array Index</param>
		/// <param name="length">Length to copy (formerly count)</param>
		/// <returns>the new length of the Target Array</returns>
		public static int CopyTo(ref byte[] src, int srcoffset, ref byte[] dst, int dstoffset, int length)
		{
			Array.Copy(src, srcoffset, dst, dstoffset, length);

			return dst.Length;
		}

		public static void ReadServerList(string filename, ref Dictionary<string, Serverentry> servers)
		{
			var serverlist = Files.ReadXML(filename.ToLowerInvariant());
			var list = serverlist.GetElementsByTagName("Server");
			servers.Add(Settings.ServerName, new Serverentry(254, Settings.ServerName, Settings.DHCP_DEFAULT_BOOTFILE, Exts.GetServerIP(), Definitions.BootServerTypes.MicrosoftWindowsNTBootServer));

			for (var i = 0; i < list.Count; i++)
			{
				var addr = IPAddress.Parse(list[i].Attributes["address"].InnerText);
				var hostname = list[i].Attributes["hostname"].InnerText;
				var type = (Definitions.BootServerTypes)int.Parse(list[i].Attributes["type"].InnerText);

				var bootfile = list[i].Attributes["bootfile"].InnerText;
				var ident = (short)(servers.Count + 1);
				var e = new Serverentry(ident, hostname, bootfile, addr, type);
				servers.Add(hostname, e);
			}
		}

		public static byte[] GenerateServerList(ref Dictionary<string, Serverentry> servers, short item)
		{
			if (item == 0)
			{
				var discover = new byte[3];
				discover[0] = (byte)Definitions.PXEVendorEncOptions.DiscoveryControl;
				discover[1] = 1;
				discover[2] = 3;

				#region "Menu Prompt"
				var message = Encoding.ASCII.GetBytes("This server includes a list in its response. Choose the desired one!");
				var timeout = byte.MaxValue;

				var prompt = new byte[(message.Length + 3)];
				prompt[0] = (byte)Definitions.PXEVendorEncOptions.MenuPrompt;
				prompt[1] = Convert.ToByte(message.Length + 1);
				prompt[2] = timeout;

				Array.Copy(message, 0, prompt, 3, message.Length);
				#endregion

				#region "Menu"
				var menu = new byte[(servers.Count * 32)];
				var menulength = 0;
				var moffset = 0;

				foreach (var server in servers)
				{
					var name = Encoding.ASCII.GetBytes(server.Value.Hostname);
					var ident = BitConverter.GetBytes(server.Value.Ident);

					var nlen = name.Length;

					if (nlen > 32)
						nlen = 32;

					var menuentry = new byte[(ident.Length + nlen + 3)];
					moffset = 0;
					Array.Copy(ident, 0, menuentry, moffset, ident.Length);
					moffset += ident.Length;

					menuentry[2] = Convert.ToByte(nlen);
					moffset += 1;

					Array.Copy(name, 0, menuentry, moffset, nlen);
					moffset += nlen;

					if (menulength == 0)
						menulength = 2;

					Array.Copy(menuentry, 0, menu, menulength, moffset);
					menulength += moffset;
				}

				menu[0] = (byte)Definitions.PXEVendorEncOptions.BootMenue;
				menu[1] += Convert.ToByte(menulength - 2);
				#endregion

				#region "Serverlist"
				var entry = new byte[7];
				var srvlist = new byte[((servers.Count * entry.Length) + 2)];

				var resultoffset = 2;

				foreach (var server in servers)
				{
					var entryoffset = 0;
					#region "Server entry"
					var ident = BitConverter.GetBytes(server.Value.Ident);
					var type = BitConverter.GetBytes((byte)server.Value.Type);
					var addr = Exts.GetServerIP().GetAddressBytes();

					Array.Copy(ident, 0, entry, entryoffset, ident.Length);
					entryoffset += ident.Length;

					Array.Copy(type, 0, entry, entryoffset, 1);
					entryoffset += 1;

					Array.Copy(addr, 0, entry, entryoffset, addr.Length);
					entryoffset += addr.Length;
					#endregion

					Array.Copy(entry, 0, srvlist, resultoffset, entry.Length);
					resultoffset += entry.Length;

					srvlist[0] = (byte)Definitions.PXEVendorEncOptions.BootServers;
					srvlist[1] += Convert.ToByte(entry.Length);
					#endregion
				}

				var result = new byte[(discover.Length + menu.Length + prompt.Length + srvlist.Length)];
				var optoffset = 0;

				Array.Copy(discover, 0, result, optoffset, discover.Length);
				optoffset += discover.Length;

				Array.Copy(srvlist, 0, result, optoffset, srvlist.Length);
				optoffset += srvlist.Length;

				Array.Copy(prompt, 0, result, optoffset, prompt.Length);
				optoffset += prompt.Length;

				Array.Copy(menu, 0, result, optoffset, menulength);
				optoffset += menulength;

				var block = new byte[optoffset];

				Array.Copy(result, 0, block, 0, block.Length);

				return block;
			}
			else
			{
				var bootitem = new byte[8];

				bootitem[0] = (byte)Definitions.PXEVendorEncOptions.BootItem;
				bootitem[1] = 7;

				var itm = BitConverter.GetBytes(item);
				Array.Copy(itm, 0, bootitem, 2, itm.Length);

				return bootitem;
			}
		}

		public static int FindDrv(string filename, string vid, string pid, out string sysfile, out string service,
		out string bustype, out string characteristics)
		{
			/*
			<drivers> <-- root node
				<driver vid="100b" did="0020" file="dp83815.sys" service="dp83815" /> <--- driver entry
				<driver vid="8086" did="100f" file="e1000b.sys" service="e1000b" /> <--- driver entry
			</drivers>
			*/

			var drivers = Files.ReadXML(filename.ToLowerInvariant());
			var retval = 1;

			var fil = string.Empty;
			var svc = string.Empty;
			var cha = string.Empty;
			var bus = string.Empty;

			var list = drivers.GetElementsByTagName("driver");

			for (var i = 0; i < list.Count; i++)
			{
				var v = list[i].Attributes["vid"].InnerText;
				var p = list[i].Attributes["did"].InnerText;

				if (v == vid.ToLower() && p == pid.ToLower())
				{
					fil = list[i].Attributes["file"].InnerText.ToLower();
					svc = list[i].Attributes["service"].InnerText;
					bus = list[i].Attributes["bustype"].InnerText;
					cha = list[i].Attributes["characteristics"].InnerText;

					retval = 0;
					break;
				}
			}

			sysfile = fil;
			service = svc;
			characteristics = cha;
			bustype = bus;

			return retval;
		}

		public static byte[] ParameterlistEntry(string name, string type, string value)
		{
			var n = Encoding.ASCII.GetBytes(name);
			var t = Encoding.ASCII.GetBytes(type);
			var v = Encoding.ASCII.GetBytes(value);

			var offset = 0;
			var data = new byte[n.Length + t.Length + v.Length + 2];

			Array.Copy(n, 0, data, offset, n.Length);
			offset += n.Length + 1;

			Array.Copy(t, 0, data, offset, t.Length);
			offset += t.Length + 1;

			Array.Copy(v, 0, data, offset, v.Length);
			offset += v.Length + 1;

			return data;
		}

		public static void SelectBootFile(ref DHCPClient client, bool wdsclient, Definitions.NextActionOptionValues nextaction)
		{
			if (wdsclient)
				switch (client.Arch)
				{
					case Definitions.Architecture.INTEL_X86:
						if (nextaction == Definitions.NextActionOptionValues.Approval)
						{
							client.BootFile = Path.Combine(Settings.WDS_BOOT_PREFIX_X86, Settings.WDS_BOOTFILE_X86);
							client.BCDPath = Path.Combine(Settings.WDS_BOOT_PREFIX_X86, Settings.WDS_BCD_FileName);
						}
						else
						{
							client.BootFile = Path.Combine(Settings.WDS_BOOT_PREFIX_X86, Settings.WDS_BOOTFILE_ABORT);
						}

						break;
					case Definitions.Architecture.INTEL_IA64:
						if (nextaction == Definitions.NextActionOptionValues.Approval)
						{
							client.BootFile = Path.Combine(Settings.WDS_BOOT_PREFIX_IA64, Settings.WDS_BOOTFILE_IA64);
							client.BCDPath = Path.Combine(Settings.WDS_BOOT_PREFIX_IA64, Settings.WDS_BCD_FileName);
						}
						else
						{
							client.BootFile = Path.Combine(Settings.WDS_BOOT_PREFIX_IA64, Settings.WDS_BOOTFILE_ABORT);
						}

						break;
					case Definitions.Architecture.INTEL_X64:
						if (nextaction == Definitions.NextActionOptionValues.Approval)
						{
							client.BootFile = Path.Combine(Settings.WDS_BOOT_PREFIX_X64, Settings.WDS_BOOTFILE_X64);
							client.BCDPath = Path.Combine(Settings.WDS_BOOT_PREFIX_X64, Settings.WDS_BCD_FileName);
						}
						else
						{
							client.BootFile = Path.Combine(Settings.WDS_BOOT_PREFIX_X64, Settings.WDS_BOOTFILE_ABORT);
						}

						break;
					case Definitions.Architecture.INTEL_EFI:
						if (nextaction == Definitions.NextActionOptionValues.Approval)
						{
							client.BootFile = Path.Combine(Settings.WDS_BOOT_PREFIX_EFI, Settings.WDS_BOOTFILE_EFI);
							client.BCDPath = Path.Combine(Settings.WDS_BOOT_PREFIX_EFI, Settings.WDS_BCD_FileName);
						}
						else
						{
							client.BootFile = Path.Combine(Settings.WDS_BOOT_PREFIX_EFI, Settings.WDS_BOOTFILE_ABORT);
						}

						break;
					default:
						client.BootFile = Path.Combine(Settings.WDS_BOOT_PREFIX_X86, Settings.WDS_BOOTFILE_X86);
						client.BCDPath = Path.Combine(Settings.WDS_BOOT_PREFIX_X86, Settings.WDS_BCD_FileName);
						break;
				}
			else
				client.BootFile = Path.Combine(Settings.WDS_BOOT_PREFIX_X86, Settings.DHCP_DEFAULT_BOOTFILE);
		}
	}
}
