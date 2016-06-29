namespace bootpd
{
	using System;
	using System.Globalization;
	using System.Security.Cryptography;
	using System.Text;

	public class NTLMSSP
	{
		private static byte[] nullEncMagic = { 0xAA, 0xD3, 0xB4, 0x35, 0xB5, 0x14, 0x04, 0xEE };
		private static byte[] magic = { 0x4B, 0x47, 0x53, 0x21, 0x40, 0x23, 0x24, 0x25 };

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

			this.challenge = Exts.StringToByte(challenge, Encoding.ASCII);
			this.password = password;

			this.GenerateHashes(this.password);

			this.flags = NTLMFlags.Ris;
		}

		public NTLMSSP(string password, byte[] challenge)
		{
			this.Password = password;
			this.challenge = challenge;
		}

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

		public enum NTLMSubBlockType : ushort
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

		public byte[] LM => this.GetResponse(this.lmpassword);

		public NTLMFlags Flags => this.flags;

		public byte[] NT => this.GetResponse(this.ntpassword);

		public byte[] UserSessionKey
		{
			get
			{
				var md4 = MD4.Create();

				md4.Initialize();
				this.userSessionKey = md4.ComputeHash(this.GetResponse(this.ntpassword));

				return this.userSessionKey;
			}
		}

		public static byte[] GenerateSubBlock(NTLMSubBlockType type, string value)
		{
			var offset = 0;
			var d = Exts.StringToByte(value, Encoding.Unicode);
			var t = BitConverter.GetBytes((ushort)type);
			var l = BitConverter.GetBytes((ushort)d.Length);

			var block = new byte[(d.Length + t.Length + l.Length)];

			offset += Functions.CopyTo(ref t, 0, ref block, offset, t.Length);
			offset += Functions.CopyTo(ref l, 0, ref block, offset, l.Length);
			offset += Functions.CopyTo(ref d, 0, ref block, offset, d.Length);
			
			return block;
		}

		public static byte[] CreateMessage(NTLMMessageType msgtype, NTLMFlags ntlmflags, string challenge)
		{
			var targetnameOffset = 48;

			var domain = Exts.StringToByte(Settings.ServerDomain, Encoding.Unicode);
			var tiboffset = targetnameOffset + domain.Length;
			var offset = 0;
			var targetInfoBlockSize = 0;
			var tib = TargetInfoBlock(out targetInfoBlockSize);

			var signature = Exts.StringToByte("NTLMSSP\0", Encoding.ASCII);
			var indicator = BitConverter.GetBytes((int)msgtype);
			var tnsb = SecBuffer(Settings.ServerDomain, targetnameOffset);
			var flags = BitConverter.GetBytes((int)ntlmflags);

			if (BitConverter.IsLittleEndian)
				Array.Reverse(flags);

			var challeng = Exts.StringToByte(challenge, Encoding.ASCII);
			var context = new byte[8];
			var tisb = SecBuffer(tib, tiboffset);

			var buflen = signature.Length;
			buflen += indicator.Length;
			buflen += tnsb.Length * 2;
			buflen += flags.Length;
			buflen += challeng.Length;
			buflen += context.Length;

			/*
			buflen += tisb.Length;
			buflen += tib.Length;
			
			*/

			buflen += domain.Length;
			var message = new byte[buflen];
			offset += Functions.CopyTo(ref signature, 0, ref message, offset, signature.Length);

			/* Indicator */
			if (BitConverter.IsLittleEndian)
				Array.Reverse(indicator);

			offset += Functions.CopyTo(ref indicator, 0, ref message, offset, indicator.Length);

			/* Target Name Security Buffer */
			offset += Functions.CopyTo(ref tnsb, 0, ref message, offset, tnsb.Length);
			

			/* Flags */
			if (BitConverter.IsLittleEndian)
				Array.Reverse(flags);

			offset += Functions.CopyTo(ref flags, 0, ref message, offset, flags.Length);

			/* Challenge */
			offset += Functions.CopyTo(ref challeng, 0, ref message, offset, challeng.Length);
			
			/* Context */
			offset += Functions.CopyTo(ref context, 0, ref message, offset, context.Length);

			/* TODO: Our C-Version is skipping this o.o" (Why?!)
			offset += Functions.CopyTo(ref tisb, 0, ref message, offset, tisb.Length);
			*/

			/* TNSB (Dummy Copy) */
			offset += Functions.CopyTo(ref tnsb, 0, ref message, offset, tnsb.Length);

			/* Domain */
			offset += Functions.CopyTo(ref domain, 0, ref message, offset, domain.Length);
		
			/*
			TODO: Our C- Version is skipping this o.o" (Why?!) 
			offset += Functions.CopyTo(ref tib, 0, ref Message, offset, tib.Length);
			*/

			return message;
		}

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

		internal static byte[] TargetInfoBlock(out int size)
		{
			var domainname = GenerateSubBlock(NTLMSubBlockType.DomainName, Settings.ServerDomain);
			var servername = GenerateSubBlock(NTLMSubBlockType.ServerName, Settings.ServerName);

			var dnsdomain = GenerateSubBlock(NTLMSubBlockType.DNSDomainName, Settings.UserDNSDomain);
			var fqdnservername = GenerateSubBlock(NTLMSubBlockType.DNSDomainName, "{0}.{1}".F(Settings.ServerName, Settings.UserDNSDomain));

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

		private void GenerateHashes(string password)
		{
			var des = DES.Create();
			des.Mode = CipherMode.ECB;
			var ct = (ICryptoTransform)null;

			if ((password == null) || (password.Length < 1))
				Buffer.BlockCopy(nullEncMagic, 0, this.lmpassword, 0, 8);
			else
			{
				des.Key = this.PasswordToKey(password, 0, Encoding.ASCII);
				ct = des.CreateEncryptor();
				ct.TransformBlock(magic, 0, 8, this.lmpassword, 0);
			}

			if ((password == null) || (password.Length < 8))
				Buffer.BlockCopy(nullEncMagic, 0, this.lmpassword, 8, 8);
			else
			{
				des.Key = this.PasswordToKey(password, 7, Encoding.ASCII);
				ct = des.CreateEncryptor();
				ct.TransformBlock(magic, 0, 8, this.lmpassword, 8);
			}

			var md4 = MD4.Create();

			md4.Initialize();

			var data = (password == null) ? new byte[0] : Exts.StringToByte(password, Encoding.Unicode);
			var hash = md4.ComputeHash(data);
			Buffer.BlockCopy(hash, 0, this.ntpassword, 0, 16);

			Array.Clear(data, 0, data.Length);
			Array.Clear(hash, 0, hash.Length);

			des.Clear();
		}
	}
}
