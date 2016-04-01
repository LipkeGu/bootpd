namespace bootpd
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Net;

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
			CopyTo(ref packet, 8, ref data, 0, data.Length);

			return data;
		}

		public static byte[] Pack_Packet(byte[] data)
		{
			var packet = new byte[(data.Length + 8)];
			CopyTo(ref data, 0, ref packet, 8, data.Length);

			return data;
		}

		/// <summary>
		/// Returns the offset of the specified DHCP option in the Packet.
		/// </summary>
		/// <param name="packet">The DHCP Packet</param>
		/// <param name="option">The DHCP Option</param>
		/// <returns>This function returns 0 if the DHCP option is not in the packet.</returns>
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

		/// <summary>
		/// Copies the contents of an array to the specified position in the destination array.
		/// </summary>
		/// <param name="src">Source</param>
		/// <param name="srcoffset">Source Index</param>
		/// <param name="dst">Target</param>
		/// <param name="dstoffset">Target Array Index</param>
		/// <param name="length">Length to copy (formerly count)</param>
		/// <returns>the new length of the target Array</returns>
		public static int CopyTo(ref byte[] src, int srcoffset, ref byte[] dst, int dstoffset, int length)
		{
			Array.Copy(src, srcoffset, dst, dstoffset, length);

			return dst.Length;
		}

		public static void ReadServerList(string filename, ref Dictionary<string, Serverentry> servers)
		{
			var serverlist = Files.ReadXML(filename.ToLowerInvariant());
			var list = serverlist.GetElementsByTagName("Server");
			servers.Add(Settings.ServerName, new Serverentry(254, Settings.ServerName, Settings.DHCP_DEFAULT_BOOTFILE,
			Settings.ServerIP, Definitions.BootServerTypes.MicrosoftWindowsNTBootServer));

			for (var i = 0; i < list.Count; i++)
			{
				if (servers.Count > 254)
					break;

				var addr = IPAddress.Parse(list[i].Attributes["address"].InnerText);
				var hostname = list[i].Attributes["hostname"].InnerText;
				var type = (Definitions.BootServerTypes)int.Parse(list[i].Attributes["type"].InnerText);

				var bootfile = list[i].Attributes["bootfile"].InnerText;
				var ident = (short)(servers.Count + 1);
				var e = new Serverentry(ident, hostname, bootfile, addr, type);

				if (!servers.ContainsKey(hostname))
					servers.Add(hostname, e);
			}
		}

		public static byte[] GenerateDHCPEncOption(byte option, int length, byte[] data)
		{
			var o = BitConverter.GetBytes(option);
			var l = BitConverter.GetBytes(Convert.ToByte(length));

			var offset = 0;
			var result = new byte[(2 + data.Length)];

			Array.Copy(o, 0, result, 0, 1);
			offset += 1;

			Array.Copy(l, 0, result, 1, 1);
			offset += 1;

			Array.Copy(data, 0, result, 2, data.Length);

			return result;
		}

		public static byte[] GenerateServerList(ref Dictionary<string, Serverentry> servers, short item)
		{
			if (item == 0)
			{
				var discover = new byte[3];
				discover[0] = Convert.ToByte(Definitions.PXEVendorEncOptions.DiscoveryControl);
				discover[1] = sizeof(byte);
				discover[2] = Convert.ToByte(Settings.DiscoveryType);

				#region "Menu Prompt"
				var msg = Settings.DHCP_MENU_PROMPT;

				if (msg.Length >= byte.MaxValue)
					msg.Remove(250);

				var message = Exts.StringToByte(msg);
				var timeout = byte.MaxValue;

				var prompt = new byte[(message.Length + 3)];
				prompt[0] = Convert.ToByte(Definitions.PXEVendorEncOptions.MenuPrompt);
				prompt[1] = Convert.ToByte(message.Length + 1);
				prompt[2] = timeout;

				CopyTo(ref message, 0, ref prompt, 3, message.Length);
				#endregion

				#region "Menu"
				var menu = new byte[(servers.Count * 32)];
				var menulength = 0;
				var moffset = 0;
				var isrv2 = 0;

				foreach (var server in servers)
				{
					if (isrv2 > 254)
						continue;

					var name = Exts.StringToByte(server.Value.Hostname);
					var ident = BitConverter.GetBytes(server.Value.Ident);

					var nlen = name.Length;

					if (nlen > 32)
						nlen = 32;

					var menuentry = new byte[(ident.Length + nlen + 3)];
					moffset = 0;
					CopyTo(ref ident, 0, ref menuentry, moffset, ident.Length);
					moffset += ident.Length;

					menuentry[2] = Convert.ToByte(nlen);
					moffset += 1;

					CopyTo(ref name, 0, ref menuentry, moffset, nlen);
					moffset += nlen;

					if (menulength == 0)
						menulength = 2;

					CopyTo(ref menuentry, 0, ref menu, menulength, moffset);
					menulength += moffset;

					isrv2++;
				}

				menu[0] = Convert.ToByte(Definitions.PXEVendorEncOptions.BootMenue);
				menu[1] += Convert.ToByte(menulength - 2);
				#endregion

				#region "Serverlist"
				var entry = new byte[7];
				var srvlist = new byte[((servers.Count * entry.Length) + 2)];

				var resultoffset = 2;
				var isrv = 0;
				foreach (var server in servers)
				{
					if (isrv > 254)
						continue;

					var entryoffset = 0;
					#region "Server entry"
					var ident = BitConverter.GetBytes(server.Value.Ident);
					var type = BitConverter.GetBytes((byte)server.Value.Type);
					var addr = Settings.ServerIP.GetAddressBytes();

					CopyTo(ref ident, 0, ref entry, entryoffset, ident.Length);
					entryoffset += ident.Length;

					CopyTo(ref type, 0, ref entry, entryoffset, 1);
					entryoffset += 1;

					CopyTo(ref addr, 0, ref entry, entryoffset, addr.Length);
					entryoffset += addr.Length;
					#endregion

					CopyTo(ref entry, 0, ref srvlist, resultoffset, entry.Length);
					resultoffset += entry.Length;

					srvlist[0] = Convert.ToByte(Definitions.PXEVendorEncOptions.BootServers);
					srvlist[1] += Convert.ToByte(entry.Length);
					#endregion

					isrv++;
				}

				var result = new byte[(discover.Length + menu.Length + prompt.Length + srvlist.Length)];
				var optoffset = 0;

				CopyTo(ref discover, 0, ref result, optoffset, discover.Length);
				optoffset += discover.Length;

				CopyTo(ref srvlist, 0, ref result, optoffset, srvlist.Length);
				optoffset += srvlist.Length;

				CopyTo(ref prompt, 0, ref result, optoffset, prompt.Length);
				optoffset += prompt.Length;

				CopyTo(ref menu, 0, ref result, optoffset, menulength);
				optoffset += menulength;

				var block = new byte[optoffset];
				CopyTo(ref result, 0, ref block, 0, block.Length);

				return block;
			}
			else
			{
				var bootitem = new byte[8];
				bootitem[0] = Convert.ToByte(Definitions.PXEVendorEncOptions.BootItem);
				var itm = BitConverter.GetBytes(item);
				bootitem[1] = Convert.ToByte(6);
				CopyTo(ref itm, 0, ref bootitem, 2, itm.Length);

				return bootitem;
			}
		}

		public static int FindDrv(string filename, string vid, string pid, out string sysfile, out string service,
		out string bustype, out string characteristics)
		{
			var drivers = Files.ReadXML(filename);
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
			var n = Exts.StringToByte(name);
			var t = Exts.StringToByte(type);
			var v = Exts.StringToByte(value);

			var offset = 0;
			var data = new byte[n.Length + t.Length + v.Length + 2];

			CopyTo(ref n, 0, ref data, offset, n.Length);
			offset += n.Length + 1;

			CopyTo(ref t, 0, ref data, offset, t.Length);
			offset += t.Length + 1;

			CopyTo(ref v, 0, ref data, offset, v.Length);
			offset += v.Length + 1;

			return data;
		}

		public static void SelectBootFile(ref DHCPClient client, bool wdsclient, Definitions.NextActionOptionValues nextaction)
		{
			if (wdsclient)
				switch (client.Arch)
				{
					case Definitions.Architecture.Intelx86PC:
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
					case Definitions.Architecture.EFIItanium:
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
					case Definitions.Architecture.EFIx8664:
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
					case Definitions.Architecture.EFIBC:
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
