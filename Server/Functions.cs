using Bootpd;
using Server.Extensions;
using System;
using System.IO;
using System.Text;
using static Bootpd.Functions;

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
