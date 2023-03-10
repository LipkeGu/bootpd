namespace Server.Network
{
	using System.Net;
	using System.Text;
	using Crypto;

	public class RISClient
	{
		Encoding encoding;
		byte[] lm_response;
		byte[] nt_response;

		bool targetRequested;

		bool use_ntlmv2;
		bool key128;
		bool key_56;

		bool extSessSecurity;
		bool keyExchange;


		bool ntlm_sign;
		bool always_sign;
		bool workstation_supplied;
		bool oem_encoding;
		bool unicode_encoding;

		public RISClient(IPEndPoint endpoint)
		{
			encoding = Encoding.Unicode;
			Endpoint = endpoint;
			workstation_supplied = false;
			NTLMSSP_NEGOTIATE_DOMAIN_SUPPLIED = false;
			NTLMSSP_NEGOTIATE_SEAL = false;
			NTLMSSP_NEGOTIATE_SEAL = false;
			always_sign = false;
			TargetType = NTLMTargets.Domain;
			use_ntlmv2 = !Settings.EnableNTLMV2;
		}

		public IPEndPoint Endpoint { get; set; }

		public RISOPCodes OPCode { get; set; }

		public bool NTLMSSP_NEGOTIATE_WORKSTATION_SUPPLIED
		{
			get
			{
				return workstation_supplied;
			}

			set
			{
				workstation_supplied = false;
			}
		}

		public bool NTLMSSP_NEGOTIATE_DOMAIN_SUPPLIED { get; set; }

		public bool NTLMSSP_NEGOTIATE_EXTENDED_SESSIONSECURITY
		{
			get
			{
				return extSessSecurity;
			}

			set
			{
				extSessSecurity = value;
				if (extSessSecurity)
				{
//					Errorhandler.Report(LogTypes.Info, "NTLMSSP_NEGOTIATE_EXTENDED_SESSIONSECURITY is set!");
					ServerFlags |= NTLMFlags.NTLMSSP_NEGOTIATE_EXTENDED_SESSIONSECURITY;
				}
			}
		}

		public bool NTLMSSP_NEGOTIATE_SEAL { get; set; }

		public bool NTLMSSP_REQUEST_TARGET
		{
			get
			{
				return targetRequested;
			}

			set
			{
				targetRequested = value;

				if (targetRequested)
				{
					ServerFlags |= NTLMFlags.NTLMSSP_NEGOTIATE_TARGET_INFO;
				}
			}
		}

		public bool NTLMSSP_TARGET_TYPE_DOMAIN
		{
			set
			{
				if (value)
				{
					TargetType = NTLMTargets.Domain;
				}
			}
		}

		public bool NTLMSSP_TARGET_TYPE_SERVER
		{
			set
			{
				if (value)
				{
					TargetType = NTLMTargets.Server;
				}
			}
		}

		public Encoding NTLMSSP_NEGOTIATED_ENCODING
		{
			get
			{
				if (oem_encoding && unicode_encoding)
					return Encoding.Unicode;
				else
					return Encoding.ASCII;
			}
		}

		public bool NTLMSSP_NEGOTIATE_ALWAYS_SIGN
		{
			get
			{
				return always_sign;
			}

			set
			{
				always_sign = value;
				if (always_sign)
				{
					ServerFlags |= NTLMFlags.NTLMSSP_NEGOTIATE_ALWAYS_SIGN;
				}
			}
		}

		public bool NTLMSSP_NEGOTIATE_OEM
		{
			get
			{
				return oem_encoding;
			}

			set
			{
				oem_encoding = value;

				if (oem_encoding)
					ServerFlags |= NTLMFlags.NTLMSSP_NEGOTIATE_OEM;
			}
		}

		public bool NTLMSSP_NEGOTIATE_UNICODE
		{
			get
			{
				return unicode_encoding;
			}

			set
			{
				unicode_encoding = value;
				if (unicode_encoding)
					ServerFlags |= NTLMFlags.NTLMSSP_NEGOTIATE_UNICODE;
			}
		}
		public bool NTLMSSP_NEGOTIATE_SIGN
		{
			get
			{
				return ntlm_sign;
			}

			set
			{
				ntlm_sign = value;

				if (ntlm_sign)
					ServerFlags |= NTLMFlags.NTLMSSP_NEGOTIATE_SIGN;
			}
		}

		public bool NTLMSSP_NEGOTIATE_LM_KEY { get; set; }

		public bool NTLMSSP_NEGOTIATE_128
		{
			get
			{
				return key128;
			}

			set
			{
				key128 = value;
				if (key128)
					ServerFlags |= NTLMFlags.NTLMSSP_NEGOTIATE_128;
			}
		}

		public bool NTLMSSP_NEGOTIATE_56
		{
			get
			{
				return key_56;
			}

			set
			{
				key_56 = value;
				if (key_56)
					ServerFlags |= NTLMFlags.NTLMSSP_NEGOTIATE_56;
			}
		}

		public bool NTLMSSP_NEGOTIATE_KEY_EXCH
		{
			get
			{
				return keyExchange;
			}

			set
			{
				keyExchange = value;
				if (keyExchange)
					ServerFlags |= NTLMFlags.NTLMSSP_NEGOTIATE_KEY_EXCH;
			}
		}

		public bool NTLMSSP_NEGOTIATE_NTLM
		{
			get
			{
				if (use_ntlmv2)
					return false;
				else
					return true;
			}

			set
			{
				if (value)
					use_ntlmv2 = value ? false : true;
				
				if (!use_ntlmv2)
					ServerFlags |= NTLMFlags.NTLMSSP_NEGOTIATE_NTLM;
				else
					ServerFlags |= NTLMFlags.NTLMSSP_NEGOTIATE_EXTENDED_SESSIONSECURITY;
			}
		}

		public NTLMTargets TargetType { get; set; }

		public NTLMFlags ServerFlags { get; private set; }
	}
}
