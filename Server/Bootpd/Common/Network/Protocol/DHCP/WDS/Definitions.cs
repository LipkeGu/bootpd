namespace bootpd.Bootpd.Common.Network.Protocol.DHCP
{
	/// <summary>
	/// Options used by the Windows Deployment Server NBP
	/// </summary>
	public enum WDSNBPOptions
	{
		Unknown,
		Architecture = 1,
		NextAction = 2,
		PollInterval = 3,
		PollRetryCount = 4,
		RequestID = 5,
		Message = 6,
		VersionQuery = 7,
		ServerVersion = 8,
		ReferralServer = 9,
		PXEClientPrompt = 11,
		PxePromptDone = 12,
		NBPVersion = 13,
		ActionDone = 14,
		AllowServerSelection = 15,
		ServerFeatures = 16,

		End = byte.MaxValue
	}
}

/// <summary>
/// Options used by the PXEClientPrompt and PXEPromptDone
/// </summary>
public enum PXEPromptOptionValues
{
	Unknown,
	OptIn,
	NoPrompt,
	OptOut
}

/// <summary>
/// Options used by the WDSNBPOptions.NBPVersion
/// </summary>
public enum NBPVersionValues
{
	Seven = 0x0700,
	Eight = 0x0800,
	Unknown = ushort.MinValue
}

/// <summary>
/// Options used by the WDSNBPOptions.NextAction
/// </summary>
public enum NextActionOptionValues : int
{
	Drop = 0,
	Approval = 1,
	Referral = 3,
	Abort = 5
}