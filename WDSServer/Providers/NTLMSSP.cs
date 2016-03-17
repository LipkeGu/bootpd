using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace WDSServer.Providers
{
	public class NTLMSSP
	{
		static private byte[] nullEncMagic = { 0xAA, 0xD3, 0xB4, 0x35, 0xB5, 0x14, 0x04, 0xEE };
		static private byte[] magic = { 0x4B, 0x47, 0x53, 0x21, 0x40, 0x23, 0x24, 0x25 };

		public enum NTLMFlags : int
		{
			NTLMSSP_NEGOTIATE_UNICODE = 0x00000001,
			NTLM_NEGOTIATE_OEM = 0x00000002,
			NTLMSSP_REQUEST_TARGET = 0x00000004,
			NTLMSSP_RESERVED_9 = 0x00000008,
			NTLMSSP_NEGOTIATE_SIGN = 0x00000010,
			NTLMSSP_NEGOTIATE_SEAL = 0x00000020,
			NTLMSSP_NEGOTIATE_DATAGRAM = 0x00000040,
			NTLMSSP_NEGOTIATE_LM_KEY = 0x00000080,
			NTLMSSP_Negotiate_NETWARE = 0x00000100,
			NTLMSSP_NEGOTIATE_NTLM = 0x00000200,
			NTLMSSP_NEGOTIATE_NT_ONLY = 0x00000400,
			NTLMSSP_Negotiate_ANONYMOUS = 0x00000800,
			NTLMSSP_NEGOTIATE_OEM_DOMAIN_SUPPLIED = 0x00001000,
			NTLMSSP_NEGOTIATE_OEM_WORKSTATION_SUPPLIED = 0x00002000,
			NTLMSSP_NEGOTIATE_LOCALCALL = 0x00004000,
			NTLMSSP_NEGOTIATE_ALWAYS_SIGN = 0x00008000,

			NTLMSSP_TARGET_TYPE_DOMAIN = 0x00010000,
			NTLMSSP_TARGET_TYPE_SERVER = 0x00020000,
			NTLMSSP_TARGET_TYPE_SHARE = 0x00040000,
			NTLMSSP_NEGOTIATE_NTLM2 = 0x00080000,
			NTLMSSP_NEGOTIATE_IDENTIFY = 0x00100000,
			NTLMSSP_Request_ACCEPTRESPONSE = 0x00200000,
			NTLMSSP_REQUEST_NONNTSESSION_KEY = 0x00400000,
			NTLMSSP_NEGOTIATE_TARGET_INFO = 0x00800000,

			Negotiate128 = 0x20000000,
			NTLMSSP_NEGOTIATE_KEY_EXCH = 0x40000000,
			Negotiate56 = (unchecked((int)0x80000000)),
			Ris = 0x00018206
		}

		public enum NTLMSubBlockType : short
		{
			Terminator = 0x0000,
			ServerName = 0x0100,
			DomainName = 0x0200,
			DNSServerName = 0x0300,
			DNSDomainName = 0x0400
		}

		public enum NTLMMessageType : int
		{
			Negotiate = 0x01000000,
			Challenge = 0x02000000,
			Authenticate = 0x03000000
		}

		string username;
		string password;

		byte[] lmpassword;
		byte[] ntpassword;
		byte[] challenge;
		byte[] userSessionKey;

		string domain;
		string workstation;

		NTLMFlags flags;

		public NTLMSSP(string password, string challenge)
		{
			this.lmpassword = new byte[21];
			this.ntpassword = new byte[21];

			this.challenge = Encoding.ASCII.GetBytes(challenge);
			this.password = password;

			this.GenerateHashes(this.password);

			this.flags = NTLMFlags.Ris;
		}

		public NTLMSSP(string password, byte[] challenge)
		{
			this.Password = password;
			this.challenge = challenge;
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

		public string Username
		{
			get
			{
				return this.username;
			}

			set
			{
				this.username = value;
			}
		}

		public string Password
		{
			set
			{
				this.GenerateHashes(value);
			}
		}

		public string Workstation
		{
			get
			{
				return this.workstation;
			}

			set
			{
				this.workstation = value;
			}
		}

		public byte[] LM => GetResponse(this.lmpassword);

		public NTLMFlags Flags => this.flags;

		public byte[] NT => GetResponse(this.ntpassword);


		public byte[] UserSessionKey
		{
			get
			{
				var md4 = MD4.Create();

				md4.Initialize();
				this.userSessionKey = md4.ComputeHash(GetResponse(this.ntpassword));

				return this.userSessionKey;
			}

		}

		public static byte[] GenerateSubBlock(NTLMSubBlockType type, string value)
		{
			var offset = 0;
			var d = Encoding.Unicode.GetBytes(value);


			var t = BitConverter.GetBytes((short)type);
			var l = BitConverter.GetBytes((short)d.Length);

			var block = new byte[(d.Length + t.Length + l.Length)];

			Array.Copy(t, 0, block, offset, t.Length);
			offset += t.Length;

			Array.Copy(l, 0, block, offset, l.Length);
			offset += l.Length;

			Array.Copy(d, 0, block, offset, d.Length);
			offset += d.Length;

			return block;
		}

		#region "NTLMSSP Functions"
		public static byte[] CreateMessage(NTLMMessageType msgtype, NTLMFlags ntlmflags, string challenge)
		{

			var targetnameOffset = 48;

			var domain = Encoding.Unicode.GetBytes(Settings.ServerDomain);
			var tiboffset = (targetnameOffset + domain.Length);
			var offset = 0;
			var TargetInfoBlockSize = 0;
			var tib = TargetInfoBlock(out TargetInfoBlockSize);

			var signature = Encoding.ASCII.GetBytes("NTLMSSP\0");
			var indicator = BitConverter.GetBytes((int)msgtype);
			var tnsb = SecBuffer(Settings.ServerDomain, targetnameOffset);
			var flags = BitConverter.GetBytes((int)ntlmflags);

			if (BitConverter.IsLittleEndian)
				Array.Reverse(flags);

			var challeng = Encoding.ASCII.GetBytes(challenge);
			var context = new byte[8];
			var tisb = SecBuffer(tib, tiboffset);

			#region "calculate buffersize"
			var buflen = signature.Length;
			buflen += indicator.Length;
			buflen += (tnsb.Length * 2);
			buflen += flags.Length;
			buflen += challeng.Length;
			buflen += context.Length;
			// buflen += tisb.Length;
			// buflen += tib.Length;
			buflen += domain.Length;
			#endregion

			var Message = new byte[buflen];

			#region "Signature
			Array.Copy(signature, 0, Message, offset, signature.Length);
			offset += signature.Length;
			#endregion

			#region "Indicator"
			Console.WriteLine("Offset is now: {0} | 8 | {1} (indicator) | 4", offset, indicator.Length);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(indicator);

			Array.Copy(indicator, 0, Message, offset, indicator.Length);
			offset += indicator.Length;
			#endregion

			#region "Targetname Security Buffer"
			Console.WriteLine("Offset is now: {0} | 12 | {1} (tnsb) | 8", offset, tnsb.Length);
			Array.Copy(tnsb, 0, Message, offset, tnsb.Length);
			offset += tnsb.Length;
			#endregion

			#region "Flags"

			Console.WriteLine("Offset is now: {0} | 20 | {1} (flasg) | 4", offset, 4);

			if (BitConverter.IsLittleEndian)
				Array.Reverse(flags);

			Array.Copy(flags, 0, Message, offset, flags.Length);
			offset += flags.Length;
			#endregion

			#region "Challenge"

			Console.WriteLine("Offset is now: {0} | 24 | {1} (Challenge) | 8", offset, challeng.Length);

			Array.Copy(challeng, 0, Message, offset, challeng.Length);
			offset += challeng.Length;
			#endregion

			#region "Context"

			Console.WriteLine("Offset is now: {0} | 32 | {1} (Context) | 8", offset, context.Length);
			Array.Copy(context, 0, Message, offset, context.Length);
			offset += context.Length;
			#endregion

			/*
			TODO: Our C- Version is skipping this o.o" (Why?!)
			
			Console.WriteLine("Offset is now: {0} | 40 | {1} (tisb) | 8", offset, tisb.Length);
			Array.Copy(tisb, 0, Message, offset, tisb.Length);
			offset += tisb.Length;
			*/

			Console.WriteLine("Offset is now: {0} | 40 | {1} (tisb) | 8", offset, tisb.Length);
			Array.Copy(tnsb, 0, Message, offset, tnsb.Length);
			offset += tnsb.Length;

			#region "TargetName Data"

			Console.WriteLine("Offset is now: {0} | 48 | {1} (domain) | 8", offset, domain.Length);
			Array.Copy(domain, 0, Message, offset, domain.Length);
			offset += domain.Length;
			Console.WriteLine("Offset is now: {0} | X Position | {1} (tib) | {2}", offset, tib.Length, TargetInfoBlockSize);
			#endregion

			/*
			TODO: Our C- Version is skipping this o.o" (Why?!) 
			
			Array.Copy(tib, 0, Message, offset, tib.Length);
			offset += tib.Length;
			*/
			return Message;
		}

		internal static byte[] SecBuffer(string data, int position)
		{
			var offset = 0;
			var buffer = new byte[8];
			var length = BitConverter.GetBytes((short)(data.Length * 2));
			var pos = BitConverter.GetBytes(position);

			// length
			Array.Copy(length, 0, buffer, offset, length.Length);
			offset += length.Length;

			// Allocated Space
			Array.Copy(length, 0, buffer, offset, length.Length);
			offset += length.Length;

			// Offset
			Array.Copy(pos, 0, buffer, offset, pos.Length);
			return buffer;
		}

		internal static byte[] SecBuffer(byte[] data, int position)
		{
			var offset = 0;
			var buffer = new byte[8];
			var length = BitConverter.GetBytes((short)data.Length);
			var pos = BitConverter.GetBytes(position);

			// length
			Array.Copy(length, 0, buffer, offset, length.Length);
			offset += length.Length;

			// Allocated Space
			Array.Copy(length, 0, buffer, offset, length.Length);
			offset += length.Length;

			// Offset
			Array.Copy(pos, 0, buffer, offset, pos.Length);
			offset += pos.Length;

			return buffer;
		}

		internal static byte[] TargetInfoBlock(out int size)
		{
			var domainname = GenerateSubBlock(NTLMSubBlockType.DomainName, Settings.ServerDomain);
			var servername = GenerateSubBlock(NTLMSubBlockType.ServerName, Settings.ServerName);

			var dnsdomain = GenerateSubBlock(NTLMSubBlockType.DNSDomainName, Settings.UserDNSDomain);
			var fqdnservername = GenerateSubBlock(NTLMSubBlockType.DNSDomainName, "{0}.{1}".F(Settings.ServerName, Settings.UserDNSDomain));

			var blocksize = (domainname.Length + servername.Length + dnsdomain.Length + fqdnservername.Length) + 4;
			var block = new byte[blocksize];

			var offset = 0;

			Array.Copy(domainname, 0, block, offset, domainname.Length);
			offset += domainname.Length;

			Array.Copy(servername, 0, block, offset, servername.Length);
			offset += servername.Length;

			Array.Copy(dnsdomain, 0, block, offset, dnsdomain.Length);
			offset += dnsdomain.Length;

			Array.Copy(fqdnservername, 0, block, offset, fqdnservername.Length);
			offset += fqdnservername.Length + 4;

			size = offset;

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

		private byte[] GetResponse(byte[] pwd)
		{
			var response = new byte[24];
			var des = DES.Create();
			des.Mode = CipherMode.ECB;
			des.Key = this.PrepareDESKey(pwd, 0);

			var ct = des.CreateEncryptor();
			ct.TransformBlock(this.challenge, 0, this.challenge.Length, response, 0);

			des.Key = PrepareDESKey(pwd, 7);

			ct = des.CreateEncryptor();
			ct.TransformBlock(this.challenge, 0, this.challenge.Length, response, 8);

			des.Key = PrepareDESKey(pwd, 14);

			ct = des.CreateEncryptor();
			ct.TransformBlock(this.challenge, 0, this.challenge.Length, response, 16);

			return response;
		}

		private byte[] PasswordToKey(string password, int position)
		{
			var key7 = new byte[7];
			var tmp = password.ToUpper(CultureInfo.CurrentCulture);
			Encoding.ASCII.GetBytes(tmp, position, Math.Min(tmp.Length - position, 7), key7, 0);

			var key8 = this.PrepareDESKey(key7, 0);
			Array.Clear(key7, 0, key7.Length);

			return key8;
		}

		private void GenerateHashes(string password)
		{
			var des = DES.Create();
			des.Mode = CipherMode.ECB;
			var ct = (ICryptoTransform)null;

			#region "LM Password"
			if ((password == null) || (password.Length < 1))
				Buffer.BlockCopy(nullEncMagic, 0, this.lmpassword, 0, 8);
			else
			{
				des.Key = PasswordToKey(password, 0);
				ct = des.CreateEncryptor();
				ct.TransformBlock(magic, 0, 8, this.lmpassword, 0);
			}

			if ((password == null) || (password.Length < 8))
				Buffer.BlockCopy(nullEncMagic, 0, this.lmpassword, 8, 8);
			else
			{
				des.Key = PasswordToKey(password, 7);
				ct = des.CreateEncryptor();
				ct.TransformBlock(magic, 0, 8, this.lmpassword, 8);
			}
			#endregion

			#region "NT Password"
			var md4 = MD4.Create();

			md4.Initialize();

			var data = ((password == null) ? (new byte[0]) : (Encoding.Unicode.GetBytes(password)));
			var hash = md4.ComputeHash(data);
			Buffer.BlockCopy(hash, 0, this.ntpassword, 0, 16);

			Array.Clear(data, 0, data.Length);
			Array.Clear(hash, 0, hash.Length);
			#endregion

			des.Clear();
		}
		#endregion
	}
}
