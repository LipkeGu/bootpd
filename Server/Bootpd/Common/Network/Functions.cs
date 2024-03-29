﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Bootpd
{
	public static partial class Functions
	{
		public static string GetHostName(this IPAddress ipAddress)
		{
			var entry = Dns.GetHostEntry(ipAddress);
			return (entry != null) ? entry.HostName : ipAddress.ToString();
		}

		public static int CopyTo(byte[] src, int srcoffset, byte[] dst, int dstoffset = 0, int length = 0)
		{
			var len = length == 0 ? src.Length : length;
			Array.Copy(src, srcoffset, dst, dstoffset, len);

			return len;
		}

		public static int CopyTo(byte src, byte[] dst, int dstoffset = 0)
		{
			var len = 1;

			dst[dstoffset] = src;

			return len;
		}

		public static List<IPAddress> DNSLookup(string hostname)
			=> Dns.GetHostAddresses(hostname).ToList();

		public static T AsType<T>(object obj) => (T)Convert.ChangeType(obj, typeof(T));

		public static bool HasMethod(object obj, string name) => obj.GetType().GetMethod(name) != null;

		public static TS InvokeMethod<TS>(object obj, string name, object[] parameters = null)
			=> AsType<TS>(obj.GetType().GetMethod(name).Invoke(obj, parameters));

		public static void InvokeMethod(object obj, string name, object[] parameters = null)
			=> obj.GetType().GetMethod(name).Invoke(obj, parameters);
	}
}
