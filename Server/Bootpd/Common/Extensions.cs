﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Bootpd
{
	public static class Extensions
	{
		public static string GetString(this byte[] input)
			=> Encoding.ASCII.GetString(input);

		public static string GetString(this byte[] input, Encoding encoding)
			=> encoding.GetString(input);

		public static byte[] GetBytes(this int input)
			=> BitConverter.GetBytes(input);

		public static byte[] GetBytes(this uint input)
			=> BitConverter.GetBytes(input);

		public static byte[] GetBytes(this long input)
			=> BitConverter.GetBytes(input);

		public static byte[] GetBytes(this ulong input)
			=> BitConverter.GetBytes(input);

		public static byte[] GetBytes(this short input)
				=> BitConverter.GetBytes(input);

		public static byte[] GetBytes(this ushort input)
				=> BitConverter.GetBytes(input);

		public static byte[] GetBytes(this string input)
			=> Encoding.ASCII.GetBytes(input);

		public static byte[] GetBytes(this string input, Encoding encoding)
			=> encoding.GetBytes(input);

		public static short ToInt16(this Stream input)
		{
			var buffer = new byte[sizeof(short)];
			input.Read(buffer, 0, buffer.Length);

			return BitConverter.ToInt16(buffer, 0);
		}

		public static ushort ToUInt16(this Stream input)
		{
			var buffer = new byte[sizeof(ushort)];
			input.Read(buffer, 0, buffer.Length);

			return BitConverter.ToUInt16(buffer, 0).LE16();
		}

		public static int ToInt32(this Stream input)
		{
			var buffer = new byte[sizeof(int)];
			input.Read(buffer, 0, buffer.Length);

			return BitConverter.ToInt32(buffer, 0);
		}

		public static IPAddress ToIPAddress(this Stream input)
		{
			var buffer = new byte[sizeof(uint)];
			input.Read(buffer, 0, buffer.Length);

			return new IPAddress(buffer);
		}

		public static uint ToUint32(this Stream input)
		{
			var buffer = new byte[sizeof(uint)];
			input.Read(buffer, 0, buffer.Length);

			return BitConverter.ToUInt32(buffer, 0);
		}

		public static void Write(this Stream input, IPAddress addr)
		{
			var ipBytes = addr.GetAddressBytes();
			input.Write(ipBytes);
		}

		public static byte[] MacAsBytes(this string input, string delimeter = ":")
			=> input.Split(delimeter.ToCharArray()).Select(x => Convert.ToByte(x, 16)).ToArray();


		public static int Write(this Stream input, byte[] buffer, long position = 0)
		{
			var curPos = input.Position;
			var retval = 0;

			if (position != 0)
				input.Position = position;

			retval = input.Write(buffer);
			return retval;
		}

		public static int Write(this Stream input, byte[] buffer)
		{
			input.Write(buffer, 0, buffer.Length);
			return buffer.Length;
		}

		public static void Write(this Stream input, int buffer)
		{
			var buf = BitConverter.GetBytes(buffer);
			input.Write(buf, 0, buf.Length);
		}

		public static void Write(this Stream input, uint buffer)
		{
			var buf = BitConverter.GetBytes(buffer);
			input.Write(buf, 0, buf.Length);
		}

		public static void Write(this Stream input, short buffer)
		{
			var buf = BitConverter.GetBytes(buffer);
			input.Write(buf, 0, buf.Length);
		}

		public static void Write(this Stream input, ushort buffer)
		{
			var buf = BitConverter.GetBytes(buffer);
			input.Write(buf, 0, buf.Length);
		}

		public static void Write(this Stream input, long buffer)
		{
			var buf = BitConverter.GetBytes(buffer);
			input.Write(buf, 0, buf.Length);
		}

		public static void Write(this Stream input, ulong buffer)
		{
			var buf = BitConverter.GetBytes(buffer);
			input.Write(buf, 0, buf.Length);
		}

		public static int Write(this Stream input, string buffer, int buffersize = 0)
		{
			var strBytes = buffer.GetBytes();
			var byteBuffer = new byte[buffersize != 0 ? buffersize : strBytes.Length];

			Array.Copy(strBytes, 0, byteBuffer, 0, strBytes.Length);
			input.Write(byteBuffer, 0, byteBuffer.Length);

			return byteBuffer.Length;
		}

		public static byte[] Read(this Stream input, long count)
		{
			var buffer = new byte[count];
			var curpos = input.Position;
			input.Read(buffer, 0, buffer.Length);
			input.Position = curpos;

			return buffer;
		}

		public static void CopyTo(this Stream src, Stream dest, int srcPos, int destPos, int count)
		{
			var bytes = new byte[count];
			var curSrcPos = src.Position;
			var curDstPos = dest.Position;

			src.Position = srcPos;
			src.Read(bytes, 0, bytes.Length);
			src.Position = curSrcPos;


			dest.Position = destPos;
			dest.Write(bytes, 0, bytes.Length);
			dest.Position = curDstPos;
		}


		public static int _WriteByte(this Stream input, byte value)
		{

			input.WriteByte(value);

			return sizeof(byte);
		}

		public static uint LE32(this uint value)
		{
			if (BitConverter.IsLittleEndian)
			{
				var input = BitConverter.GetBytes(value);

				Array.Reverse(input);

				return BitConverter.ToUInt32(input, 0);
			}

			return value; // BIG Endian
		}

		public static ushort BE16(this ushort value)
		{
			if (!BitConverter.IsLittleEndian)
			{
				var input = BitConverter.GetBytes(value);

				Array.Reverse(input);

				return BitConverter.ToUInt16(input, 0);
			}

			return value;
		}

		public static ushort LE16(this ushort value)
		{
			if (BitConverter.IsLittleEndian)
			{
				var input = BitConverter.GetBytes(value);

				Array.Reverse(input);

				return BitConverter.ToUInt16(input, 0);
			}

			return value; // BIG Endian
		}

		public static string ReadString(this Stream input, long count)
			=> Encoding.ASCII.GetString(Read(input, count));
	}
}
