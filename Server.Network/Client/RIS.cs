namespace Server.Network
{
	using System.Net;
	using System.Text;
	using Crypto;

	public class RISClient
	{
		IPEndPoint endpoint;
		Encoding encoding;
		NTLMFlags server_flags;

		byte[] lm_response;
		byte[] nt_response;

		bool targetRequested;

		bool use_ntlmv2;
		bool key128;
		bool key_56;

		bool extSessSecurity;
		bool keyExchange;


		bool ntlm_sign;
		bool ntlm_seal;
		bool always_sign;

		bool useLMKey;

		bool workstation_supplied;
		bool domain_supplied;

		bool oem_encoding;
		bool unicode_encoding;

		NTLMTargets target_type;


		RISOPCodes opcode;

		public RISClient(IPEndPoint endpoint)
		{
			this.encoding = Encoding.Unicode;
			this.endpoint = endpoint;
			this.workstation_supplied = false;
			this.domain_supplied = false;
			this.ntlm_seal = false;
			this.ntlm_seal = false;
			this.always_sign = false;
			this.target_type = NTLMTargets.Domain;
			this.use_ntlmv2 = !Settings.EnableNTLMV2;
		}

		public IPEndPoint Endpoint
		{
			get
			{
				return this.endpoint;
			}

			set
			{
				this.endpoint = value;
			}
		}

		public RISOPCodes OPCode
		{
			get
			{
				return this.opcode;
			}

			set
			{
				this.opcode = value;
			}
		}

		public bool NTLMSSP_NEGOTIATE_WORKSTATION_SUPPLIED
		{
			get
			{
				return this.workstation_supplied;
			}

			set
			{
				this.workstation_supplied = false;
			}
		}

		public bool NTLMSSP_NEGOTIATE_DOMAIN_SUPPLIED
		{
			get
			{
				return this.domain_supplied;
			}

			set
			{
				this.domain_supplied = value;
			}
		}

		public bool NTLMSSP_NEGOTIATE_EXTENDED_SESSIONSECURITY
		{
			get
			{
				return this.extSessSecurity;
			}

			set
			{
				this.extSessSecurity = value;
				if (this.extSessSecurity)
				{
//					Errorhandler.Report(LogTypes.Info, "NTLMSSP_NEGOTIATE_EXTENDED_SESSIONSECURITY is set!");
					this.server_flags |= NTLMFlags.NTLMSSP_NEGOTIATE_EXTENDED_SESSIONSECURITY;
				}
			}
		}

		public bool NTLMSSP_NEGOTIATE_SEAL
		{
			get
			{
				return this.ntlm_seal;
			}

			set
			{
				this.ntlm_seal = value;
			}
		}

		public bool NTLMSSP_REQUEST_TARGET
		{
			get
			{
				return this.targetRequested;
			}

			set
			{
				this.targetRequested = value;

				if (this.targetRequested)
				{
					this.server_flags |= NTLMFlags.NTLMSSP_NEGOTIATE_TARGET_INFO;
				}
			}
		}

		public bool NTLMSSP_TARGET_TYPE_DOMAIN
		{
			set
			{
				if (value)
				{
					this.target_type = NTLMTargets.Domain;
				}
			}
		}

		public bool NTLMSSP_TARGET_TYPE_SERVER
		{
			set
			{
				if (value)
				{
					this.target_type = NTLMTargets.Server;
				}
			}
		}

		public Encoding NTLMSSP_NEGOTIATED_ENCODING
		{
			get
			{
				if (this.oem_encoding && this.unicode_encoding)
					return Encoding.Unicode;
				else
					return Encoding.ASCII;
			}
		}

		public bool NTLMSSP_NEGOTIATE_ALWAYS_SIGN
		{
			get
			{
				return this.always_sign;
			}

			set
			{
				this.always_sign = value;
				if (this.always_sign)
				{
					this.server_flags |= NTLMFlags.NTLMSSP_NEGOTIATE_ALWAYS_SIGN;
				}
			}
		}

		public bool NTLMSSP_NEGOTIATE_OEM
		{
			get
			{
				return this.oem_encoding;
			}

			set
			{
				this.oem_encoding = value;

				if (this.oem_encoding)
					this.server_flags |= NTLMFlags.NTLMSSP_NEGOTIATE_OEM;
			}
		}

		public bool NTLMSSP_NEGOTIATE_UNICODE
		{
			get
			{
				return this.unicode_encoding;
			}

			set
			{
				this.unicode_encoding = value;
				if (this.unicode_encoding)
					this.server_flags |= NTLMFlags.NTLMSSP_NEGOTIATE_UNICODE;
			}
		}
		public bool NTLMSSP_NEGOTIATE_SIGN
		{
			get
			{
				return this.ntlm_sign;
			}

			set
			{
				this.ntlm_sign = value;

				if (this.ntlm_sign)
					this.server_flags |= NTLMFlags.NTLMSSP_NEGOTIATE_SIGN;
			}
		}

		public bool NTLMSSP_NEGOTIATE_LM_KEY
		{
			get
			{
				return this.useLMKey;
			}

			set
			{
				this.useLMKey = value;
			}
		}

		public bool NTLMSSP_NEGOTIATE_128
		{
			get
			{
				return this.key128;
			}

			set
			{
				this.key128 = value;
				if (this.key128)
					this.server_flags |= NTLMFlags.NTLMSSP_NEGOTIATE_128;
			}
		}

		public bool NTLMSSP_NEGOTIATE_56
		{
			get
			{
				return this.key_56;
			}

			set
			{
				this.key_56 = value;
				if (this.key_56)
					this.server_flags |= NTLMFlags.NTLMSSP_NEGOTIATE_56;
			}
		}

		public bool NTLMSSP_NEGOTIATE_KEY_EXCH
		{
			get
			{
				return this.keyExchange;
			}

			set
			{
				this.keyExchange = value;
				if (this.keyExchange)
					this.server_flags |= NTLMFlags.NTLMSSP_NEGOTIATE_KEY_EXCH;
			}
		}

		public bool NTLMSSP_NEGOTIATE_NTLM
		{
			get
			{
				if (this.use_ntlmv2)
					return false;
				else
					return true;
			}

			set
			{
				if (value)
					this.use_ntlmv2 = value ? false : true;
				
				if (!this.use_ntlmv2)
					this.server_flags |= NTLMFlags.NTLMSSP_NEGOTIATE_NTLM;
				else
					this.server_flags |= NTLMFlags.NTLMSSP_NEGOTIATE_EXTENDED_SESSIONSECURITY;
			}
		}

		public NTLMTargets TargetType
		{
			get
			{
				return this.target_type;
			}

			set
			{
				this.target_type = value;
			}
		}

		public NTLMFlags ServerFlags
		{
			get
			{
				return this.server_flags;
			}
		}
	}
}
