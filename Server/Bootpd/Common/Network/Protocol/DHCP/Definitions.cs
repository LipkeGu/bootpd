﻿namespace Bootpd.Common.Network.Protocol.DHCP
{
	public enum DHCPMsgType : byte
	{
		Discover = 1,
		Offer = 2,
		Request = 3,
		Decline = 4,
		Ack = 5,
		Nak = 6,
		Release = 7,
		Inform = 8,
		ForceRenew = 9,
		LeaseQuery = 10,
		LeaseUnassined = 11,
		LeaseUnknown = 12,
		LeaseActive = 13,
		BulkLeaseQuery = 14,
		LeaseQueryDone = 15,
		ActiveLeaseQuery = 16,
		LeasequeryStatus = 17,
		Tls = 18
	}
}

public enum BootpFlags : ushort
{
	Unicast = 0,
	Broadcast = 128
}

public enum BootpHWType : byte
{
	Ethernet = 1
}

public enum BootpMsgType : byte
{
	Request = 1,
	Reply = 2,
}