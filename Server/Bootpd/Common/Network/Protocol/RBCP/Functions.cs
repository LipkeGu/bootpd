using Bootpd.Common.Network.Protocol.DHCP;
using Server.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Bootpd.Functions;

namespace Bootpd.Common.Network.Protocol.RBCP
{
	public static partial class Functions
	{
		public class BootMenueEntry
		{
			public ushort Id { get; private set; }

			public string Description { get; private set; }

			public BootMenueEntry(ushort id, string desc)
			{
				Id = id;
				Description = desc;
			}
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

				var offset = CopyTo(BitConverter.GetBytes(((ushort)server.Type).LE16()), 0, serverListBlock, 0);
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
				var ident = BitConverter.GetBytes(entry.Id.LE16());
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




	}
}
