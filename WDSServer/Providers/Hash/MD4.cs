using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

public class MD4 : HashAlgorithm
{
	private uint a;
	private uint b;
	private uint c;
	private uint d;
	private uint[] x;
	private int bytesProcessed;

	public MD4()
	{
		this.x = new uint[16];
	}

	~MD4()
	{
		Array.Clear(this.x, 0, this.x.Length);
	}

	public override void Initialize()
	{
		this.a = 0x67452301;
		this.b = 0xefcdab89;
		this.c = 0x98badcfe;
		this.d = 0x10325476;

		this.bytesProcessed = 0;
	}

	protected override void HashCore(byte[] array, int offset, int length)
	{
		this.ProcessMessage(Bytes(array, offset, length));
	}

	protected override byte[] HashFinal()
	{
		try
		{
			this.ProcessMessage(this.Padding());

			return new[] { this.a, this.b, this.c, this.d }.SelectMany(word => Bytes(word)).ToArray();
		}
		finally
		{
			this.Initialize();
		}
	}

	private static IEnumerable<byte> Bytes(byte[] bytes, int offset, int length)
	{
		for (var i = offset; i < length; i++)
			yield return bytes[i];
	}

	private static uint ROL(uint value, int numberOfBits) => (value << numberOfBits) | (value >> (32 - numberOfBits));

	private static uint Round1Operation(uint a, uint b, uint c, uint d, uint xk, int s)
	{
		unchecked
		{
			return ROL(a + ((b & c) | (~b & d)) + xk, s);
		}
	}

	private static uint Round2Operation(uint a, uint b, uint c, uint d, uint xk, int s)
	{
		unchecked
		{
			return ROL(a + ((b & c) | (b & d) | (c & d)) + xk + 0x5a827999, s);
		}
	}

	private static uint Round3Operation(uint a, uint b, uint c, uint d, uint xk, int s)
	{
		unchecked
		{
			return ROL(a + (b ^ c ^ d) + xk + 0x6ed9eba1, s);
		}
	}

	private void ProcessMessage(IEnumerable<byte> bytes)
	{
		foreach (var b in bytes)
		{
			var c = this.bytesProcessed & 63;
			var i = c >> 2;
			var s = (c & 3) << 3;

			this.x[i] = (this.x[i] & ~((uint)255 << s)) | ((uint)b << s);

			if (c == 63)
				this.Process16WordBlock();

			this.bytesProcessed++;
		}
	}

	private IEnumerable<byte> Bytes(uint word)
	{
		yield return (byte)(word & 255);
		yield return (byte)((word >> 8) & 255);
		yield return (byte)((word >> 16) & 255);
		yield return (byte)((word >> 24) & 255);
	}

	private IEnumerable<byte> Repeat(byte value, int count)
	{
		for (var i = 0; i < count; i++)
			yield return value;
	}

	private IEnumerable<byte> Padding() => this.Repeat(128, 1)
	.Concat(this.Repeat(0, ((this.bytesProcessed + 8) & 0x7fffffc0) + 55 - this.bytesProcessed))
	.Concat(this.Bytes((uint)this.bytesProcessed << 3))
	.Concat(this.Repeat(0, 4));

	private void Process16WordBlock()
	{
		var aa = this.a;
		var bb = this.b;
		var cc = this.c;
		var dd = this.d;

		foreach (var k in new[] { 0, 4, 8, 12 })
		{
			aa = Round1Operation(aa, bb, cc, dd, this.x[k], 3);
			dd = Round1Operation(dd, aa, bb, cc, this.x[k + 1], 7);
			cc = Round1Operation(cc, dd, aa, bb, this.x[k + 2], 11);
			bb = Round1Operation(bb, cc, dd, aa, this.x[k + 3], 19);
		}

		foreach (var k in new[] { 0, 1, 2, 3 })
		{
			aa = Round2Operation(aa, bb, cc, dd, this.x[k], 3);
			dd = Round2Operation(dd, aa, bb, cc, this.x[k + 4], 5);
			cc = Round2Operation(cc, dd, aa, bb, this.x[k + 8], 9);
			bb = Round2Operation(bb, cc, dd, aa, this.x[k + 12], 13);
		}

		foreach (var k in new[] { 0, 2, 1, 3 })
		{
			aa = Round3Operation(aa, bb, cc, dd, this.x[k], 3);
			dd = Round3Operation(dd, aa, bb, cc, this.x[k + 8], 9);
			cc = Round3Operation(cc, dd, aa, bb, this.x[k + 4], 11);
			bb = Round3Operation(bb, cc, dd, aa, this.x[k + 12], 15);
		}

		unchecked
		{
			this.a += aa;
			this.b += bb;
			this.c += cc;
			this.d += dd;
		}
	}
}