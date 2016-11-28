using System;

namespace Server.Crypto
{
	/// <summary>
	/// Indicates the type of data in a subblock 
	/// </summary>
	public enum NTLMSSPSubBlockTypes : ushort
	{
		Terminator = 0x0000,
		ServerName = 0x0001,
		DomainName = 0x0002,
		DNSHostname = 0x0003,
		DNSDomainName = 0x0004,
		DnsTreeName = 0x0005,
		Flags = 0x0006,
		Timestamp = 0x0007,
		SingleHost = 0x0008,
		TargetName = 0x0009,
		ChannelBindings = 0x000a
	}

	public enum ntlmssp_role
	{
		NTLMSSP_SERVER,
		NTLMSSP_CLIENT
	}

	public enum ntlmssp_sign_version : byte
	{
		ONE = 1
	}

	public enum ntlmssp_sig_size : int
	{
		SIXTEEN = 16
	}

	public enum ntlmssp_message_type
	{
		NTLMSSP_INITIAL = 0 /* samba internal state */,
		NTLMSSP_NEGOTIATE = 1,
		NTLMSSP_CHALLENGE = 2,
		NTLMSSP_AUTH = 3,
		NTLMSSP_UNKNOWN = 4,
		NTLMSSP_DONE = 5 /* samba final state */
	}

	[Flags]
	public enum ntlmssp_flags : uint
	{
		/// <summary>
		/// This flag is set to indicate that the server/client will be using UNICODE strings. (Server <-> Client)
		/// </summary>
		NTLMSSP_NEGOTIATE_UNICODE = 0x00000001,
		/// <summary>
		/// This flag is set to indicate that the server/client will be using OEM strings. (Server <-> Client)
		/// </summary>
		NTLMSSP_NEGOTIATE_OEM = 0x00000002,
		/// <summary>
		/// If set, a TargetName field of the CHALLENGE_MESSAGE MUST be supplied.
		/// </summary>
		NTLMSSP_REQUEST_TARGET = 0x00000004,
		/// <summary>
		/// requests session key negotiation for message signatures. If the client sends NTLMSSP_NEGOTIATE_SIGN 
		/// to the server in the NEGOTIATE_MESSAGE, the server MUST return NTLMSSP_NEGOTIATE_SIGN to the client in the CHALLENGE_MESSAGE.
		/// </summary>
		NTLMSSP_NEGOTIATE_SIGN = 0x00000010,
		/// <summary>
		/// requests session key negotiation for message confidentiality. If the client sends NTLMSSP_NEGOTIATE_SEAL
		/// to the server in the NEGOTIATE_MESSAGE, the server MUST return NTLMSSP_NEGOTIATE_SEAL to the client in the CHALLENGE_MESSAGE.
		/// </summary>
		NTLMSSP_NEGOTIATE_SEAL = 0x00000020,
		/// <summary>
		/// requests connectionless authentication. If NTLMSSP_NEGOTIATE_DATAGRAM is set, then NTLMSSP_NEGOTIATE_KEY_EXCH MUST 
		/// always be set in the AUTHENTICATE_MESSAGE to the server and the CHALLENGE_MESSAGE to the client.
		/// </summary>
		NTLMSSP_NEGOTIATE_DATAGRAM = 0x00000040,
		/// <summary>
		/// requests LAN Manager (LM) session key computation.
		/// </summary>
		NTLMSSP_NEGOTIATE_LM_KEY = 0x00000080,
		/// <summary>
		/// Netware 
		/// </summary>
		NTLMSSP_NEGOTIATE_NETWARE = 0x00000100,
		/// <summary>
		/// Indicates that NTLM authentication is supported. (Server <-> Client)
		/// </summary>
		NTLMSSP_NEGOTIATE_NTLM = 0x00000200,
		/// <summary>
		/// requests only NT session key computation
		/// </summary>
		NTLMSSP_NEGOTIATE_NT_ONLY = 0x00000400,
		/// <summary>
		/// The connection SHOULD be anonymous
		/// </summary>
		NTLMSSP_NEGOTIATE_ANONYMOUS = 0x00000800,
		/// <summary>
		/// The domain name is provided.
		/// </summary>
		NTLMSSP_NEGOTIATE_OEM_DOMAIN_SUPPLIED = 0x00001000,
		/// <summary>
		/// This flag indicates whether the Workstation field is present.
		/// </summary>
		NTLMSSP_NEGOTIATE_OEM_WORKSTATION_SUPPLIED = 0x00002000,
		/// <summary>
		/// The server sets this flag to inform the client that the server and client are on the same machine.
		/// The server provides a local security context handle with the message.
		/// </summary>
		NTLMSSP_NEGOTIATE_LOCAL_CALL = 0x00004000,
		/// <summary>
		/// requests the presence of a signature block on all messages. NTLMSSP_NEGOTIATE_ALWAYS_SIGN MUST
		/// be set in the NEGOTIATE_MESSAGE to the server and the CHALLENGE_MESSAGE to the client.
		/// </summary>
		NTLMSSP_NEGOTIATE_ALWAYS_SIGN = 0x00008000,
		/// <summary>
		/// TargetName MUST be a domain name. The data corresponding to this flag is
		/// provided by the server in the TargetName field of the CHALLENGE_MESSAGE.
		/// </summary>
		NTLMSSP_TARGET_TYPE_DOMAIN = 0x00010000,
		/// <summary>
		/// If set, TargetName MUST be a server name. The data corresponding to this flag is provided
		/// by the server in the TargetName field of the CHALLENGE_MESSAGE.
		/// </summary>
		NTLMSSP_TARGET_TYPE_SERVER = 0x00020000,
		/// <summary>
		/// TargetName MUST be a share name. The data corresponding to this flag is
		/// provided by the server in the TargetName field of the CHALLENGE_MESSAGE.
		/// </summary>
		NTLMSSP_TARGET_TYPE_SHARE = 0x00040000,
		/// <summary>
		/// requests usage of the NTLM v2 session security. 
		/// </summary>
		NTLMSSP_NEGOTIATE_EXTENDED_SESSIONSECURITY = 0x00080000,
		/// <summary>
		/// Requests an identify level token.
		/// </summary>
		NTLMSSP_NEGOTIATE_IDENTIFY = 0x00100000,
		/// <summary>
		/// requests the usage of the LMOWF
		/// </summary>
		NTLMSSP_REQUEST_NON_NT_SESSION_KEY = 0x00400000,
		/// <summary>
		/// indicates that the TargetInfo fields in the CHALLENGE_MESSAGE are populated.
		/// </summary>
		NTLMSSP_NEGOTIATE_TARGET_INFO = 0x00800000,
		/// <summary>
		/// If set, requests the protocol version number. The data corresponding to this flag
		/// is provided in the Version field of the NEGOTIATE_MESSAGE, the CHALLENGE_MESSAGE,
		/// and the AUTHENTICATE_MESSAGE.
		/// </summary>
		NTLMSSP_NEGOTIATE_VERSION = 0x02000000,
		/// <summary>
		/// If the client sends NTLMSSP_NEGOTIATE_128 to the server in the NEGOTIATE_MESSAGE,
		/// the server MUST return NTLMSSP_NEGOTIATE_128 to the client in the CHALLENGE_MESSAGE
		/// only if the client sets NTLMSSP_NEGOTIATE_SEAL or NTLMSSP_NEGOTIATE_SIGN.
		/// </summary>
		NTLMSSP_NEGOTIATE_128 = 0x20000000,
		/// <summary>
		/// This requests an explicit key exchange. This capability SHOULD be used because it improves security for message integrity or confidentiality.
		/// </summary>
		NTLMSSP_NEGOTIATE_KEY_EXCH = 0x40000000,
		/// <summary>
		/// If the client sends NTLMSSP_NEGOTIATE_SEAL or NTLMSSP_NEGOTIATE_SIGN with NTLMSSP_NEGOTIATE_56 to the server in the NEGOTIATE_MESSAGE,
		/// the server MUST return NTLMSSP_NEGOTIATE_56 to the client in the CHALLENGE_MESSAGE.
		/// </summary>
		NTLMSSP_NEGOTIATE_56 = 0x80000000
	}

	public enum ntlmssp_Feature
	{
		NTLMSSP_FEATURE_SESSION_KEY = 0x00000001,
		NTLMSSP_FEATURE_SIGN = 0x00000002,
		NTLMSSP_FEATURE_SEAL = 0x00000004,
		NTLMSSP_FEATURE_CCACHE = 0x00000008
	}

	public enum NTLMTargets
	{
		Domain,
		Server,
		Share,
		Local
	}

	[Flags]
	public enum NTLMFlags : int
	{
		/// <summary>
		/// This flag is set to indicate that the server/client will be using UNICODE strings. (Server <-> Client)
		/// </summary>
		NTLMSSP_NEGOTIATE_UNICODE = 0x00000001,
		/// <summary>
		/// This flag is set to indicate that the server/client will be using OEM strings. (Server <-> Client)
		/// </summary>
		NTLMSSP_NEGOTIATE_OEM = 0x00000002,
		/// <summary>
		/// If set, a TargetName field of the CHALLENGE_MESSAGE MUST be supplied.
		/// </summary>
		NTLMSSP_REQUEST_TARGET = 0x00000004,
		/// <summary>
		/// requests session key negotiation for message signatures. If the client sends NTLMSSP_NEGOTIATE_SIGN 
		/// to the server in the NEGOTIATE_MESSAGE, the server MUST return NTLMSSP_NEGOTIATE_SIGN to the client in the CHALLENGE_MESSAGE.
		/// </summary>
		NTLMSSP_NEGOTIATE_SIGN = 0x00000010,
		/// <summary>
		/// requests session key negotiation for message confidentiality. If the client sends NTLMSSP_NEGOTIATE_SEAL
		/// to the server in the NEGOTIATE_MESSAGE, the server MUST return NTLMSSP_NEGOTIATE_SEAL to the client in the CHALLENGE_MESSAGE.
		/// </summary>
		NTLMSSP_NEGOTIATE_SEAL = 0x00000020,
		/// <summary>
		/// requests connectionless authentication. If NTLMSSP_NEGOTIATE_DATAGRAM is set, then NTLMSSP_NEGOTIATE_KEY_EXCH MUST 
		/// always be set in the AUTHENTICATE_MESSAGE to the server and the CHALLENGE_MESSAGE to the client.
		/// </summary>
		NTLMSSP_NEGOTIATE_DATAGRAM = 0x00000040,
		/// <summary>
		/// requests LAN Manager (LM) session key computation.
		/// </summary>
		NTLMSSP_NEGOTIATE_LM_KEY = 0x00000080,
		/// <summary>
		/// Indicates that NTLM authentication is supported. (Server <-> Client)
		/// </summary>
		NTLMSSP_NEGOTIATE_NTLM = 0x00000200,
		/// <summary>
		/// The connection SHOULD be anonymous
		/// </summary>
		NTLMSSP_NEGOTIATE_ANONYMOUS = 0x00000800,
		/// <summary>
		/// The domain name is provided.
		/// </summary>
		NTLMSSP_NEGOTIATE_OEM_DOMAIN_SUPPLIED = 0x00001000,
		/// <summary>
		/// This flag indicates whether the Workstation field is present.
		/// </summary>
		NTLMSSP_NEGOTIATE_OEM_WORKSTATION_SUPPLIED = 0x00002000,
		/// <summary>
		/// The server sets this flag to inform the client that the server and client are on the same machine.
		/// The server provides a local security context handle with the message.
		/// </summary>
		NTLMSSP_NEGOTIATE_LOCAL_CALL = 0x00004000,
		/// <summary>
		/// requests the presence of a signature block on all messages. NTLMSSP_NEGOTIATE_ALWAYS_SIGN MUST
		/// be set in the NEGOTIATE_MESSAGE to the server and the CHALLENGE_MESSAGE to the client.
		/// </summary>
		NTLMSSP_NEGOTIATE_ALWAYS_SIGN = 0x00008000,
		/// <summary>
		///	TargetName MUST be a domain name. The data corresponding to this flag is
		///	provided by the server in the TargetName field of the CHALLENGE_MESSAGE.
		/// </summary>
		NTLMSSP_TARGET_TYPE_DOMAIN = 0x00010000,
		/// <summary>
		/// If set, TargetName MUST be a server name. The data corresponding to this flag is provided
		/// by the server in the TargetName field of the CHALLENGE_MESSAGE.
		/// </summary>
		NTLMSSP_TARGET_TYPE_SERVER = 0x00020000,
		/// <summary>
		///	requests usage of the NTLM v2 session security. 
		/// </summary>
		NTLMSSP_NEGOTIATE_EXTENDED_SESSIONSECURITY = 0x00080000,
		/// <summary>
		/// Requests an identify level token.
		/// </summary>
		NTLMSSP_NEGOTIATE_IDENTIFY = 0x00100000,
		/// <summary>
		/// requests the usage of the LMOWF
		/// </summary>
		NTLMSSP_REQUEST_NON_NT_SESSION_KEY = 0x00400000,
		/// <summary>
		/// indicates that the TargetInfo fields in the CHALLENGE_MESSAGE are populated.
		/// </summary>
		NTLMSSP_NEGOTIATE_TARGET_INFO = 0x00800000,
		/// <summary>
		/// If the client sends NTLMSSP_NEGOTIATE_128 to the server in the NEGOTIATE_MESSAGE,
		/// the server MUST return NTLMSSP_NEGOTIATE_128 to the client in the CHALLENGE_MESSAGE
		/// only if the client sets NTLMSSP_NEGOTIATE_SEAL or NTLMSSP_NEGOTIATE_SIGN.
		/// </summary>
		NTLMSSP_NEGOTIATE_128 = 0x20000000,
		/// <summary>
		/// This requests an explicit key exchange. This capability SHOULD be used because it improves security for message integrity or confidentiality.
		/// </summary>
		NTLMSSP_NEGOTIATE_KEY_EXCH = 0x40000000,
		/// <summary>
		/// If the client sends NTLMSSP_NEGOTIATE_SEAL or NTLMSSP_NEGOTIATE_SIGN with NTLMSSP_NEGOTIATE_56 to the server in the NEGOTIATE_MESSAGE,
		/// the server MUST return NTLMSSP_NEGOTIATE_56 to the client in the CHALLENGE_MESSAGE.
		/// </summary>
		NTLMSSP_NEGOTIATE_56 = (unchecked((int)0x80000000)),
	}

	public enum NTLMMessageType : uint
	{
		/// <summary>
		/// Initial State
		/// </summary>
		NTLMInitial = 0,
		/// <summary>
		/// The NEGOTIATE_MESSAGE defines an NTLM Negotiate message that is sent from the client to the server.
		/// This message allows the client to specify its supported NTLM options to the server.
		/// </summary>
		NTLMNegotiate = 1,
		/// <summary>
		/// The CHALLENGE_MESSAGE defines an NTLM challenge message that is sent from the server to the client.
		/// The CHALLENGE_MESSAGE is used by the server to challenge the client to prove its identity. 
		/// For connection-oriented requests, the CHALLENGE_MESSAGE generated by the server is in response to the NEGOTIATE_MESSAGE from the client.
		/// </summary>
		NTLMChallenge = 2,
		/// <summary>
		/// The AUTHENTICATE_MESSAGE defines an NTLM authenticate message that is sent
		/// from the client to the server after the CHALLENGE_MESSAGE is processed by the client.
		/// </summary>
		NTLMAuthenticate = 3,
		/// <summary>
		/// Unknown Message
		/// </summary>
		NTLMUnknown = 4,
		/// <summary>
		/// Final State
		/// </summary>
		NTLMDone = 5
	}

	public enum NTLMSSP_Role : uint
	{
		Server,
		Client
	}

	public enum NTSTATUS : uint
	{
		ERROR_SUCCESS = uint.MinValue,
		SSPI_LOGON_DENIED = 0x8009030c,
		NT_STATUS_INVALID_PARAMETER = 0xC000000D
	}

	public enum NTLMRevisions : byte
	{
		NTLMSSP_REVISION_W2K3 = 0x0f
	}
}
