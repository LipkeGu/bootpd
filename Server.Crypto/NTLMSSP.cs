using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Server.Extensions;

namespace Server.Crypto
{
	public class NTLMSSP
	{
		public class ntlmssp_state
		{
			ntlmssp_role role;
			uint expected_state;

			bool unicode;
			bool use_ntlmv2;
			bool use_ccache;
			bool use_nt_response;
			bool allow_lm_key;

			private static byte[] nullEncMagic = { 0xAA, 0xD3, 0xB4, 0x35, 0xB5, 0x14, 0x04, 0xEE };
			private static byte[] magic = { 0x4B, 0x47, 0x53, 0x21, 0x40, 0x23, 0x24, 0x25 };

			string user;
			string domain;

			byte[] nt_hash;
			byte[] lm_hash;

			public bool Unicode
			{
				get
				{
					return this.unicode;
				}

				set
				{
					this.unicode = value;
				}
			}

			public bool NTLM_V2
			{
				get
				{
					return this.use_ntlmv2;
				}

				set
				{
					this.use_ntlmv2 = value;
				}
			}

			public bool Use_CCache
			{
				get
				{
					return this.use_ccache;
				}

				set
				{
					this.use_ccache = value;
				}
			}

			public bool Use_NTResponse
			{
				get
				{
					return this.use_nt_response;
				}

				set
				{
					this.use_nt_response = value;
				}
			}

			public bool AllowLMKey
			{
				get
				{
					return this.allow_lm_key;
				}

				set
				{
					this.allow_lm_key = value;
				}
			}

			public string UserName
			{
				get
				{
					return this.user;
				}

				set
				{
					this.user = value;
				}
			}

			public string Domain
			{
				get
				{
					return this.domain;
				}

				set
				{
					this.domain = value;
				}
			}

			public byte[] NTHash
			{
				get
				{
					return this.nt_hash;
				}

				set
				{
					this.nt_hash = value;
				}
			}

			public byte[] LMHash
			{
				get
				{
					return this.lm_hash;
				}

				set
				{
					this.lm_hash = value;
				}
			}

			public class client
			{
				string nbname;
				string nbdomain;

				public client(string netbios_name, string netbios_domain)
				{
					this.nbname = netbios_name;
					this.nbdomain = netbios_domain;
				}

				/// <summary>
				/// Netbios Hostname
				/// </summary>
				public string HostName
				{
					get
					{
						return this.nbname;
					}
				}

				/// <summary>
				/// Netbios Domain
				/// </summary>
				public string Domain
				{
					get
					{
						return this.nbdomain;
					}
				}
			}

			client c;
			server s;

			public class server
			{
				string nbname;
				string nbdomain;

				string dnsname;
				string dnsdomain;

				bool is_standalone;

				public server(string netbios_name, string netbios_domain, string dns_name, string dns_domain, bool standalone = false)
				{
					this.nbname = netbios_name;
					this.nbdomain = netbios_domain;

					this.is_standalone = standalone;

					this.dnsname = dns_name;
					this.dnsdomain = dns_domain;
				}

				/// <summary>
				/// Netbios Hostname
				/// </summary>
				public string HostName
				{
					get
					{
						return this.nbname;
					}
				}

				/// <summary>
				/// Netbios Domain
				/// </summary>
				public string Domain
				{
					get
					{
						return this.nbdomain;
					}
				}

				/// <summary>
				/// FQDN Hostname (server.domain.local)
				/// </summary>
				public string DNSHostName
				{
					get
					{
						return this.dnsname;
					}
				}
				/// <summary>
				/// FQDN Domain (domain.local)
				/// </summary>
				public string DNSDomain
				{
					get
					{
						return this.dnsdomain;
					}
				}

				public bool StandAlone
				{
					get
					{
						return this.is_standalone;
					}
				}
			}

			public ntlmssp_state()
			{
				this.c = new client(string.Empty, string.Empty);
				this.s = new server(string.Empty, string.Empty, string.Empty, string.Empty);
			}

			public client Client
			{
				get
				{
					return this.c;
				}
			}

			public server Server
			{
				get
				{
					return this.s;
				}
			}


			byte[] intChallenge;
			byte[] extChallenge;
			

			byte[] lm_resp;
			byte[] nt_resp;
			byte[] session_key;

			ntlmssp_flags neg_flags;

			/// <summary>
			/// Random challenge as supplied to the client for NTLM authentication.
			/// </summary>
			public byte[] Internal_Challenge
			{
				get
				{
					return this.intChallenge;
				}

				set
				{
					this.intChallenge = value;
				}
			}

			/// <summary>
			/// Random challenge as input into the actual NTLM (or NTLM V2) authentication.
			/// </summary>
			public byte[] Challenge
			{
				get
				{
					return this.extChallenge;
				}

				set
				{
					this.extChallenge = value;
				}
			}

			public byte[] LM_Response
			{
				get
				{
					return this.lm_resp;
				}

				set
				{
					this.lm_resp = value;
				}
			}

			public byte[] NT_Response
			{
				get
				{
					return this.nt_resp;
				}

				set
				{
					this.nt_resp = value;
				}
			}

			public ntlmssp_flags NegotiatedFlags
			{
				get
				{
					return this.neg_flags;
				}

				set
				{
					this.neg_flags = value;
				}
			}
		}
		byte[] challenge;
		byte[] magic = Exts.StringToByte("KGS!@#$%", Encoding.ASCII);
		byte[] lmpassword;
		byte[] ntpassword;





		/// <summary>
		/// Determine correct target name flags for reply, given server role and negotiated flags.
		/// </summary>
		/// <param name="state">The NTLMSSP State</param>
		/// <param name="neg_flags">The flags from the packet.</param>
		/// <param name="chal_flags">The flags to be set in the reply packet.</param>
		/// <returns>The targetname</returns>
		public string ntlmssp_target_name(ref ntlmssp_state state, ref ntlmssp_flags neg_flags, ref ntlmssp_flags chal_flags)
		{
			if (neg_flags.HasFlag(ntlmssp_flags.NTLMSSP_REQUEST_TARGET))
			{
				chal_flags |= ntlmssp_flags.NTLMSSP_NEGOTIATE_TARGET_INFO;
				chal_flags |= ntlmssp_flags.NTLMSSP_REQUEST_TARGET;

				if (state.Server.StandAlone)
				{
					chal_flags |= ntlmssp_flags.NTLMSSP_TARGET_TYPE_SERVER;
					return state.Server.HostName;
				}
				else
				{
					chal_flags |= ntlmssp_flags.NTLMSSP_TARGET_TYPE_DOMAIN;
					return state.Server.Domain;
				}
			}
			else
				return string.Empty;
		}

		private byte[] GetResponse(byte[] pwd)
		{
			var response = new byte[24];
			var des = DES.Create();
			des.Mode = CipherMode.ECB;
			des.Padding = PaddingMode.None;
			des.Key = this.PrepareDESKey(pwd, 0);

			var ct = des.CreateEncryptor();
			ct.TransformBlock(this.challenge, 0, this.challenge.Length, response, 0);

			des.Key = this.PrepareDESKey(pwd, 7);

			ct = des.CreateEncryptor();
			ct.TransformBlock(this.challenge, 0, this.challenge.Length, response, 8);

			des.Key = this.PrepareDESKey(pwd, 14);

			ct = des.CreateEncryptor();
			ct.TransformBlock(this.challenge, 0, this.challenge.Length, response, 16);

			return response;
		}

		private byte[] PasswordToKey(string password, int position, Encoding encoding)
		{
			var key7 = new byte[7];
			var tmp = password.ToUpper(CultureInfo.CurrentCulture);
			encoding.GetBytes(tmp, position, Math.Min(tmp.Length - position, key7.Length), key7, 0);

			var key8 = this.PrepareDESKey(key7, 0);
			Array.Clear(key7, 0, key7.Length);

			return key8;
		}

		// Lookup TALLOC CTX!!!!
		void ntlmssp_server_negotiate(ref ntlmssp_state state, /* TALLOC_CTX*/ ref byte[] out_mem_ctx, byte[] request, ref byte[] reply)
		{
			ntlmssp_flags neg_flags = ntlmssp_flags.NTLMSSP_NEGOTIATE_128;
			//uint ntlmssp_command;
			ntlmssp_flags chal_flags;
			byte[] cryptkey = new byte[8];
			string target_name;

			/* parse the NTLMSSP packet */
			//ntlmssp_handle_neg_flags(state, neg_flags, state.AllowLMKey);

			/* Check if we may set the challenge 
			if (!state.may_set_challenge(state))
			{
				state.NegotiatedFlags &= ~ntlmssp_flags.NTLMSSP_NEGOTIATE_EXTENDED_SESSIONSECURITY;
			}
			*/
			/* The flags we send back are not just the negotiated flags,
			* they are also 'what is in this packet'.  Therfore, we
			* operate on 'chal_flags' from here on
			*/

			chal_flags = state.NegotiatedFlags;

			/* get the right name to fill in as 'target' */
			target_name = ntlmssp_target_name(ref state, ref neg_flags, ref chal_flags);

		//	state.Challenge = data_blob_talloc(state, cryptkey, 8);
		//	state.Internal_Challenge = data_blob_talloc(state, cryptkey, 8);

			/* Marshal the packet in the right format, be it unicode or ASCII */
			var gen_string = string.Empty;
			var version_blob = string.Empty;

			gen_string = state.Unicode ? "CdUdbddBb" : "CdAdbddBb";
		//	state.expected_state = NTLMSSP_AUTH;
		}

		public static byte[] TargetInfoBlock(string nbdomain, string nbname, string dnsname, string userdnsdomain)
		{
			var domainname = genTiBlockEntry(NTLMSSPSubBlockTypes.DomainName, nbdomain);
			var servername = genTiBlockEntry(NTLMSSPSubBlockTypes.ServerName, nbname);

			var dnsdomain = genTiBlockEntry(NTLMSSPSubBlockTypes.DNSDomainName, userdnsdomain);
			var fqdnservername = genTiBlockEntry(NTLMSSPSubBlockTypes.DNSHostname, "{0}.{1}".F(nbname, userdnsdomain));
			var terminator = BitConverter.GetBytes(Convert.ToUInt32(uint.MinValue));

			var blocksize = (domainname.Length + servername.Length + dnsdomain.Length + fqdnservername.Length + terminator.Length);
			var block = new byte[blocksize];

			var offset = 0;

			offset += Functions.CopyTo(ref domainname, 0, ref block, offset, domainname.Length);
			offset += Functions.CopyTo(ref servername, 0, ref block, offset, servername.Length);
			offset += Functions.CopyTo(ref dnsdomain, 0, ref block, offset, dnsdomain.Length);
			offset += Functions.CopyTo(ref fqdnservername, 0, ref block, offset, fqdnservername.Length);
			offset += Functions.CopyTo(ref terminator, 0, ref block, offset, terminator.Length);

			return block;
		}

		private byte[] PrepareDESKey(byte[] key56bits, int position)
		{
			var key = new byte[8];

			key[0] = key56bits[position];
			key[1] = (byte)((key56bits[position] << 7) | (key56bits[position + 1] >> 1));
			key[2] = (byte)((key56bits[position + 1] << 6) | (key56bits[position + 2] >> 2));
			key[3] = (byte)((key56bits[position + 2] << 5) | (key56bits[position + 3] >> 3));
			key[4] = (byte)((key56bits[position + 3] << 4) | (key56bits[position + 4] >> 4));
			key[5] = (byte)((key56bits[position + 4] << 3) | (key56bits[position + 5] >> 5));
			key[6] = (byte)((key56bits[position + 5] << 2) | (key56bits[position + 6] >> 6));
			key[7] = (byte)(key56bits[position + 6] << 1);

			return key;
		}

		public static byte[] GenerateSubBlock(NTLMSSPSubBlockTypes type, string value)
		{
			var offset = 0;
			var d = Exts.StringToByte(value, Encoding.Unicode);
			var t = BitConverter.GetBytes(Convert.ToUInt16(type));
			var l = BitConverter.GetBytes(Convert.ToUInt16(d.Length));

			var block = new byte[(d.Length + t.Length + l.Length)];

			offset += Functions.CopyTo(ref t, 0, ref block, offset, t.Length);
			offset += Functions.CopyTo(ref l, 0, ref block, offset, l.Length);
			offset += Functions.CopyTo(ref d, 0, ref block, offset, d.Length);

			return block;
		}

		/*internal static byte[] TargetInfoBlock(out int size)
		{
			var domainname = GenerateSubBlock(NTLMSSPSubBlockTypes.DomainName, Settings.ServerDomain);
			var servername = GenerateSubBlock(NTLMSSPSubBlockTypes.ServerName, Settings.ServerName);

			var dnsdomain = GenerateSubBlock(NTLMSSPSubBlockTypes.DNSDomainName, Settings.UserDNSDomain);
			var fqdnservername = GenerateSubBlock(NTLMSSPSubBlockTypes.DNSDomainName, "{0}.{1}".F(Settings.ServerName, Settings.UserDNSDomain));

			var blocksize = (domainname.Length + servername.Length + dnsdomain.Length + fqdnservername.Length) + 4;
			var block = new byte[blocksize];

			var offset = 0;

			offset += Functions.CopyTo(ref domainname, 0, ref block, offset, domainname.Length);
			offset += Functions.CopyTo(ref servername, 0, ref block, offset, servername.Length);
			offset += Functions.CopyTo(ref dnsdomain, 0, ref block, offset, dnsdomain.Length);
			offset += Functions.CopyTo(ref fqdnservername, 0, ref block, offset, fqdnservername.Length) + 4;
			size = offset;

			return block;
		}
		*/

		internal static byte[] SecBuffer(string data, int position)
		{
			var offset = 0;
			var buffer = new byte[8];
			var length = BitConverter.GetBytes((ushort)(data.Length * 2));
			var pos = BitConverter.GetBytes(position);

			// length + Allocated Space!
			offset += Functions.CopyTo(ref length, 0, ref buffer, offset, length.Length);
			offset += Functions.CopyTo(ref length, 0, ref buffer, offset, length.Length);

			// Offset
			offset += Functions.CopyTo(ref pos, 0, ref buffer, offset, pos.Length);

			return buffer;
		}

		internal static byte[] SecBuffer(byte[] data, int position)
		{
			var offset = 0;
			var buffer = new byte[8];
			var length = BitConverter.GetBytes((ushort)data.Length);
			var pos = BitConverter.GetBytes(position);

			// length + Allocated Space!
			offset += Functions.CopyTo(ref length, 0, ref buffer, offset, length.Length);
			offset += Functions.CopyTo(ref length, 0, ref buffer, offset, length.Length);

			// Offset
			offset += Functions.CopyTo(ref pos, 0, ref buffer, offset, pos.Length);

			return buffer;
		}

		public static byte[] genTiBlockEntry(NTLMSSPSubBlockTypes type, string value)
		{
			var offset = 0;
			var d = Exts.StringToByte(value, Encoding.Unicode);
			var t = BitConverter.GetBytes(Convert.ToUInt16(type));
			var l = BitConverter.GetBytes(Convert.ToUInt16(d.Length));

			var block = new byte[(d.Length + t.Length + l.Length)];

			offset += Functions.CopyTo(ref t, 0, ref block, offset, t.Length);
			offset += Functions.CopyTo(ref l, 0, ref block, offset, l.Length);
			offset += Functions.CopyTo(ref d, 0, ref block, offset, d.Length);

			return block;
		}

		private void GenerateHashes(string password)
		{
			var des = DES.Create();
			des.Mode = CipherMode.ECB;
			des.Padding = PaddingMode.None;
			var ct = (ICryptoTransform)null;

			if (string.IsNullOrEmpty(password))
				Buffer.BlockCopy(magic, 0, this.lmpassword, 0, 8);
			else
			{
				des.Key = this.PasswordToKey(password, 0, Encoding.ASCII);
				ct = des.CreateEncryptor();
				ct.TransformBlock(magic, 0, 8, this.lmpassword, 0);
			}

			if ((string.IsNullOrEmpty(password)) || (password.Length < 8))
				Buffer.BlockCopy(magic, 0, this.lmpassword, 8, 8);
			else
			{
				des.Key = this.PasswordToKey(password, 7, Encoding.ASCII);
				ct = des.CreateEncryptor();
				ct.TransformBlock(magic, 0, 8, this.lmpassword, 8);
			}

			var md4 = MD4.Create();

			md4.Initialize();

			var data = string.IsNullOrEmpty(password) ? new byte[0] : Exts.StringToByte(password, Encoding.Unicode);
			var hash = md4.ComputeHash(data);
			Buffer.BlockCopy(hash, 0, this.ntpassword, 0, 16);

			Array.Clear(data, 0, data.Length);
			Array.Clear(hash, 0, hash.Length);

			des.Clear();
			md4.Clear();
		}
	}
}
