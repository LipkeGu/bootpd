using System;
using System.IO;
using WDSServer.Network;

namespace WDSServer
{
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

		public static void SelectBootFile(ref DHCPClient client, bool wdsclient)
		{
			if (wdsclient)
				switch (client.Arch)
				{
					case Definitions.Architecture.INTEL_X86:
						client.BootFile = Path.Combine(Settings.WDS_BOOT_PREFIX_X86, Settings.WDS_BOOTFILE_X86);
						client.BCDPath = Path.Combine(Settings.WDS_BOOT_PREFIX_X86, Settings.WDS_BCD_FileName);
						break;
					case Definitions.Architecture.INTEL_IA64:
						client.BootFile = Path.Combine(Settings.WDS_BOOT_PREFIX_IA64, Settings.WDS_BOOTFILE_IA64);
						client.BCDPath = Path.Combine(Settings.WDS_BOOT_PREFIX_IA64, Settings.WDS_BCD_FileName);
						break;
					case Definitions.Architecture.INTEL_X64:
						client.BootFile = Path.Combine(Settings.WDS_BOOT_PREFIX_X64, Settings.WDS_BOOTFILE_X64);
						client.BCDPath = Path.Combine(Settings.WDS_BOOT_PREFIX_X64, Settings.WDS_BCD_FileName);
						break;
					case Definitions.Architecture.INTEL_EFI:
						client.BootFile = Path.Combine(Settings.WDS_BOOT_PREFIX_EFI, Settings.WDS_BOOTFILE_EFI);
						client.BCDPath = Path.Combine(Settings.WDS_BOOT_PREFIX_EFI, Settings.WDS_BCD_FileName);
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
