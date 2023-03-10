using Server.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Server.Network
{
	public static class Functions
	{
		/// <summary>
		/// Copies the contents of an array to the specified position in the destination array.
		/// </summary>
		/// <param name="src">Source</param>
		/// <param name="srcoffset">Source Index</param>
		/// <param name="dst">Target</param>
		/// <param name="dstoffset">Target Array Index</param>
		/// <param name="length">Length to copy (formerly count)</param>
		/// <returns>the new length of the target Array</returns>
		public static int CopyTo(byte[] src, int srcoffset, byte[] dst, int dstoffset = 0, int length = 0)
		{
			var len = length == 0 ? src.Length : length;
			Array.Copy(src, srcoffset, dst, dstoffset, len);

			return len;
		}

		public static int CopyTo(byte src, byte[] dst, int dstoffset = 0)
		{
			var len = 1;

			dst[dstoffset] = src;

			return len;
		}

		public static List<IPAddress> DNSLookup(string hostname)
			=> Dns.GetHostAddresses(hostname).ToList();

		public static Encoding TestEncoding(byte[] data, int offset = 1)
		{
			return data[offset] == '\0' ? Encoding.Unicode : Encoding.ASCII;
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

		public static ushort CalcBlocksize(long tsize, ushort blksize)
		{
			var res = tsize / blksize;
			if (res < ushort.MaxValue)
				return blksize;
			else
			{
				if (res <= blksize)
					return Convert.ToUInt16(res);
				else
					return blksize;
			}
		}


		public static ushort LE16(ushort value)
		{
			var input = BitConverter.GetBytes(value);

			Array.Reverse(input);

			return BitConverter.ToUInt16(input, 0);
		}

		public static DHCPOption GenerateBootServersList(List<BootServer> servers)
		{
			var ipcount = 0;
			foreach (var server in servers)
			{
				var IPs = server.Addresses.First().GetAddressBytes().Length;
				ipcount += server.Addresses.Count * IPs;
			}

			var serverListBlock = new byte[((2 * sizeof(byte)) + 1) + ipcount];

			foreach (var server in servers)
			{
				if (!server.Addresses.Any())
					continue;

				var offset = CopyTo(BitConverter.GetBytes(LE16((ushort)server.Type)), 0, serverListBlock, 0);
				offset += CopyTo(Convert.ToByte(server.Addresses.Count()), serverListBlock, offset);

				foreach (var addr in server.Addresses)
					offset += CopyTo(addr.GetAddressBytes(), 0, serverListBlock, offset);
			}

			return new DHCPOption(8, serverListBlock);
		}


		public static DHCPOption GenerateBootMenue(List<BootServer> servers)
		{
			// Set up the Menue as it self...
			var bootmenue = new List<BootMenueEntry>();
			{
				bootmenue.Add(new BootMenueEntry(0, "Boot from local Harddisk."));
				ushort ident = 1;

				foreach (var server in servers)
					bootmenue.Add(new BootMenueEntry(ident++, server.Hostname));
			}

			var length = 0;
			foreach (var entry in bootmenue)
				length += (entry.Description.Length + sizeof(ushort));

			var menuebuffer = new byte[length + 3];
			var offset = 0;

			foreach (var entry in bootmenue)
			{
				// Ident
				var ident = BitConverter.GetBytes(LE16(entry.Id));
				offset += CopyTo(ident, 0, menuebuffer, offset);

				// Length of Description
				var descLen = Convert.ToByte(entry.Description.Length);
				offset += CopyTo(descLen, menuebuffer, offset);

				// Description
				var desc = Encoding.ASCII.GetBytes(entry.Description);
				offset += CopyTo(desc, 0, menuebuffer, offset);
			}



			return new DHCPOption(9, menuebuffer);
		}

		public static DHCPOption GenerateBootMenuePrompt()
		{
			var timeout = Convert.ToByte(255);
			var prompt = Encoding.ASCII.GetBytes(Settings.DHCP_MENU_PROMPT);
			var promptbuffer = new byte[1 + prompt.Length];
			var offset = 0;

			offset += CopyTo(timeout, promptbuffer, offset);
			offset += CopyTo(prompt, 0, promptbuffer, offset);

			return new DHCPOption(10, promptbuffer);
		}

		public static DHCPOption GenerateEncOptionsOption(byte opt, List<DHCPOption> options)
		{
			var length = 0;

			if (options.Count != 0)
			{
				options.Add(new DHCPOption(255));

				foreach (var option in options)
					length += option.Option != 255 ? 2 + option.Length : 1;

				var offset = 0;
				var block = new byte[length];

				foreach (var option in options)
				{
					offset += CopyTo(option.Option, block, offset);

					if (option.Option == 255)
						break;

					offset += CopyTo(option.Length, block, offset);
					offset += CopyTo(option.Data, 0, block, offset);
				}

				return new DHCPOption(opt, block);
			}
			else
				return null;
		}

		public static List<DHCPOption> GenerateServerList(ref Dictionary<string, BootServer> servers, ushort item)
		{
			var pxeOptions = new List<DHCPOption>();
			return pxeOptions;

			#region "Old code"
			/*	

				if (item == 0)
				{
					pxeOptions.Add(new DHCPOption(6, BitConverter.GetBytes(2)));

					#region "Menu Prompt"
					var msg = Settings.DHCP_MENU_PROMPT;

					if (msg.Length >= byte.MaxValue)
						msg.Remove(250);

					var message = Exts.StringToByte(msg, Encoding.ASCII);
					var timeout = byte.MaxValue;

					var prompt = new byte[(message.Length + 3)];


					pxeOptions.Add(new DHCPOption(10, BitConverter.GetBytes(10)));

					prompt[0] = Convert.ToByte(PXEVendorEncOptions.MenuPrompt);
					prompt[1] = Convert.ToByte(message.Length + 1);
					prompt[2] = timeout;

					CopyTo(message, 0, prompt, 3);
					#endregion

					#region "Menu"
					var menu = new byte[(servers.Count * 128)];
					var menulength = 0;
					var moffset = 0;
					var isrv2 = 0;

					foreach (var server in servers)
					{
						if (isrv2 > byte.MaxValue)
							break;

						var name = Exts.StringToByte("{0} ({1})".F(server.Value.Hostname, server.Value.IPAddress), Encoding.ASCII);
						var ident = BitConverter.GetBytes(server.Value.Ident);
						var nlen = name.Length;

						if (nlen > 128)
							nlen = 128;

						var menuentry = new byte[(ident.Length + nlen + 3)];
						moffset = CopyTo(ident, 0, menuentry, moffset);

						menuentry[2] = Convert.ToByte(nlen);
						moffset += 1;
						moffset += CopyTo(name, 0, menuentry, moffset, nlen);

						if (menulength == 0)
							menulength = 2;

						menulength += CopyTo(menuentry, 0, menu, menulength, moffset);
						isrv2++;
					}

					menu[0] = Convert.ToByte(PXEVendorEncOptions.BootMenue);
					menu[1] += Convert.ToByte(menulength - 2);
					#endregion

					#region "Serverlist"
					var entry = new byte[7];
					var srvlist = new byte[((servers.Count * entry.Length) + 2)];

					var resultoffset = 2;
					var isrv = 0;

					foreach (var server in servers)
					{
						if (isrv > byte.MaxValue)
							break;

						var entryoffset = 0;
						#region "Server entry"
						var ident = BitConverter.GetBytes(server.Value.Ident);
						var type = BitConverter.GetBytes(Convert.ToByte(server.Value.Type));
						var addr = Settings.ServerIP.GetAddressBytes();

						entryoffset += CopyTo(ident, 0, entry, entryoffset);
						entryoffset += CopyTo(type, 0, entry, entryoffset, 1);
						entryoffset += CopyTo(addr, 0, entry, entryoffset);

						resultoffset += CopyTo(entry, 0, srvlist, resultoffset);
						#endregion

						srvlist[0] = Convert.ToByte(PXEVendorEncOptions.BootServers);
						srvlist[1] += Convert.ToByte(entry.Length);
						#endregion

						isrv++;
					}

					var result = new byte[(discover.Length + menu.Length + prompt.Length + srvlist.Length)];
					var optoffset = 0;

					optoffset += CopyTo(discover, 0, result, optoffset);
					optoffset += CopyTo(srvlist, 0, result, optoffset);
					optoffset += CopyTo(prompt, 0, result, optoffset);
					optoffset += CopyTo(menu, 0, result, optoffset, menulength);

					var block = new byte[optoffset];
					CopyTo(result, 0, block, 0, block.Length);

					return block;
				}
				else
				{
					var bootitem = new byte[6];
					bootitem[0] = Convert.ToByte(PXEVendorEncOptions.BootItem);

					var itm = BitConverter.GetBytes(item);
					bootitem[1] = Convert.ToByte(4);

					CopyTo(itm, 0, bootitem, 2);

					return bootitem;
				}
			*/
			#endregion
		}

		public static void ParseNegotiatedFlags(NTLMFlags flags, ref RISClient client)
		{
			client.NTLMSSP_REQUEST_TARGET = flags.HasFlag(NTLMFlags.NTLMSSP_REQUEST_TARGET);
			client.NTLMSSP_NEGOTIATE_OEM = flags.HasFlag(NTLMFlags.NTLMSSP_NEGOTIATE_OEM);
			client.NTLMSSP_NEGOTIATE_UNICODE = flags.HasFlag(NTLMFlags.NTLMSSP_NEGOTIATE_UNICODE);
			client.NTLMSSP_NEGOTIATE_SEAL = flags.HasFlag(NTLMFlags.NTLMSSP_NEGOTIATE_SEAL);
			client.NTLMSSP_NEGOTIATE_SIGN = flags.HasFlag(NTLMFlags.NTLMSSP_NEGOTIATE_SIGN);
			client.NTLMSSP_NEGOTIATE_ALWAYS_SIGN = flags.HasFlag(NTLMFlags.NTLMSSP_NEGOTIATE_ALWAYS_SIGN);
			client.NTLMSSP_NEGOTIATE_DOMAIN_SUPPLIED = flags.HasFlag(NTLMFlags.NTLMSSP_NEGOTIATE_OEM_DOMAIN_SUPPLIED);
			client.NTLMSSP_NEGOTIATE_WORKSTATION_SUPPLIED = flags.HasFlag(NTLMFlags.NTLMSSP_NEGOTIATE_OEM_WORKSTATION_SUPPLIED);
			client.NTLMSSP_NEGOTIATE_56 = flags.HasFlag(NTLMFlags.NTLMSSP_NEGOTIATE_128 | NTLMFlags.NTLMSSP_NEGOTIATE_56);
			client.NTLMSSP_NEGOTIATE_128 = flags.HasFlag(NTLMFlags.NTLMSSP_NEGOTIATE_128 | NTLMFlags.NTLMSSP_NEGOTIATE_56);
			client.NTLMSSP_NEGOTIATE_LM_KEY = flags.HasFlag(NTLMFlags.NTLMSSP_NEGOTIATE_LM_KEY);
			client.NTLMSSP_NEGOTIATE_KEY_EXCH = flags.HasFlag(NTLMFlags.NTLMSSP_NEGOTIATE_KEY_EXCH);
			client.NTLMSSP_TARGET_TYPE_DOMAIN = flags.HasFlag(NTLMFlags.NTLMSSP_TARGET_TYPE_DOMAIN);
			client.NTLMSSP_TARGET_TYPE_SERVER = flags.HasFlag(NTLMFlags.NTLMSSP_TARGET_TYPE_SERVER);
			client.NTLMSSP_NEGOTIATE_NTLM = flags.HasFlag(NTLMFlags.NTLMSSP_NEGOTIATE_EXTENDED_SESSIONSECURITY) ?
				false : flags.HasFlag(NTLMFlags.NTLMSSP_NEGOTIATE_NTLM);
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

		public static void SelectBootFile(out string bootFile, out string bcdPath,
			bool isWDS, NextActionOptionValues nextaction, Architecture arch)
		{
			var bootfile = string.Empty;
			var bcdpath = string.Empty;

			if (isWDS)
				switch (arch)
				{
					case Architecture.Intelx86PC:
						if (nextaction == NextActionOptionValues.Approval)
						{
							bootfile = Path.Combine(Settings.WDS_BOOT_PREFIX_X86, Settings.WDS_BOOTFILE_X86);
							bcdpath = Path.Combine(Settings.WDS_BOOT_PREFIX_X86, Settings.WDS_BCD_FileName);
						}
						else
							bootfile = Path.Combine(Settings.WDS_BOOT_PREFIX_X86, Settings.WDS_BOOTFILE_ABORT);

						break;
					case Architecture.EFIItanium:
						if (nextaction == NextActionOptionValues.Approval)
						{
							bootfile = Path.Combine(Settings.WDS_BOOT_PREFIX_IA64, Settings.WDS_BOOTFILE_IA64);
							bcdpath = Path.Combine(Settings.WDS_BOOT_PREFIX_IA64, Settings.WDS_BCD_FileName);
						}
						else
							bootfile = Path.Combine(Settings.WDS_BOOT_PREFIX_IA64, Settings.WDS_BOOTFILE_ABORT);

						break;
					case Architecture.EFIx8664:
						if (nextaction == NextActionOptionValues.Approval)
						{
							bootfile = Path.Combine(Settings.WDS_BOOT_PREFIX_X64, Settings.WDS_BOOTFILE_X64);
							bcdpath = Path.Combine(Settings.WDS_BOOT_PREFIX_X64, Settings.WDS_BCD_FileName);
						}
						else
							bootfile = Path.Combine(Settings.WDS_BOOT_PREFIX_X64, Settings.WDS_BOOTFILE_ABORT);

						break;
					case Architecture.EFIBC:
						if (nextaction == NextActionOptionValues.Approval)
						{
							bootfile = Path.Combine(Settings.WDS_BOOT_PREFIX_EFI, Settings.WDS_BOOTFILE_EFI);
							bcdpath = Path.Combine(Settings.WDS_BOOT_PREFIX_EFI, Settings.WDS_BCD_FileName);
						}
						else
							bootfile = Path.Combine(Settings.WDS_BOOT_PREFIX_EFI, Settings.WDS_BOOTFILE_ABORT);

						break;
					default:
						bootfile = Path.Combine(Settings.WDS_BOOT_PREFIX_X86, Settings.WDS_BOOTFILE_X86);
						bcdpath = Path.Combine(Settings.WDS_BOOT_PREFIX_X86, Settings.WDS_BCD_FileName);
						break;
				}
			else
				bootfile = Path.Combine(Settings.WDS_BOOT_PREFIX_X86, Settings.DHCP_DEFAULT_BOOTFILE);


			bootFile = Filesystem.ReplaceSlashes(bootfile);
			bcdPath = Filesystem.ReplaceSlashes(bcdpath);
		}

		public static sbyte GetTFTPOPCode(TFTPPacket packet) => GetTFTPOPCode(packet.Data);

		public static sbyte GetTFTPOPCode(byte[] packet) => Convert.ToSByte(packet[1]);

		public static bool IsTFTPOPCode(sbyte opcode, byte[] packet)
		{
			var code = GetTFTPOPCode(packet);

			return code != opcode ? false : true;
		}

		public static int FindEndOption(ref byte[] data)
		{
			for (var i = data.Length - 1; i > 0; i--)
				if (data[i] == byte.MaxValue)
					return i;

			return 0;
		}

		public static byte[] Unpack_Packet(ref RISPacket packet)
		{
			var data = new byte[packet.Length];
			CopyTo(packet.Data, 8, data, 0, data.Length);

			return data;
		}

		public static RISPacket Pack_Packet(RISPacket data)
		{
			var packet = new RISPacket(Encoding.ASCII, new byte[(data.Length + 8)]);
			CopyTo(data.Data, 0, packet.Data, 8, data.Length);

			return data;
		}
	}
}
