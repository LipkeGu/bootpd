namespace Bootpd.Common.Network.Protocol.TFTP
{
	public enum TFTPMsgType : ushort
	{
		UNK = 0,
		RRQ = 1,
		WRQ = 2,
		DAT = 3,
		ACK = 4,
		ERR = 5,
		OCK = 6
	}

	public enum TFTPErrorCode : ushort
	{
		Unknown,
		FileNotFound,
		AccessViolation,
		DiskFull,
		IllegalOperation,
		UnknownTID,
		FileExists,
		NoSuchUser,
		InvalidOption
	}
}
