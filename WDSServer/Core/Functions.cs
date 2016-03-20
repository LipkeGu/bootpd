namespace WDSServer
{
	using System;
	using System.IO;
	using System.Text;
	using WDSServer.Network;

	public static class Functions
	{
		public static sbyte GetTFTPOPCode(TFTPPacket packet) => GetTFTPOPCode(packet.Data);

		public static sbyte GetTFTPOPCode(byte[] packet) => Convert.ToSByte(packet[1]);

		public static bool IsTFTPOPCode(sbyte opcode, byte[] packet)
		{
			var code = GetTFTPOPCode(packet);
			if (code != opcode)
				return false;
			else
				return true;
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
					pos = i + 1;
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
