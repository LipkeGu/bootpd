using System;

namespace Server.Crypto
{
	public static class RC4
	{
		public class rc4_state
		{
			public byte[] sbox;
			public byte index_i;
			public byte index_j;

			public rc4_state()
			{
				this.sbox = new byte[256];
				this.index_i = byte.MinValue;
				this.index_j = byte.MinValue;
			}
		}

		/// <summary>
		/// Initialise the RC4 sbox with key
		/// </summary>
		public static void RC4_init(ref rc4_state state, ref byte[] key)
		{
			var ind = byte.MinValue;
			var j = 0;

			for (ind = 0; ind < state.sbox.Length; ind++)
				state.sbox[ind] = ind;
		
			for (ind = 0; ind < state.sbox.Length; ind++)
			{
				var tc = byte.MinValue;

				j += (state.sbox[ind] + key[ind % key.Length]);

				tc = state.sbox[ind];
				state.sbox[ind] = state.sbox[j];
				state.sbox[j] = tc;
			}

			state.index_i = byte.MinValue;
			state.index_j = byte.MinValue;
		}

		/// <summary>
		/// Crypt the data with RC4
		/// </summary>
		public static void Crypt_Sbox(ref rc4_state state, ref byte[] data, int len)
		{
			for (var ind = 0; ind < len; ind++)
			{
				var tc = byte.MinValue;
				var t = byte.MinValue;

				state.index_i++;
				state.index_j += state.sbox[state.index_i];

				tc = state.sbox[state.index_i];
				state.sbox[state.index_i] = state.sbox[state.index_j];
				state.sbox[state.index_j] = tc;

				t = Convert.ToByte(state.sbox[state.index_i] + state.sbox[state.index_j]);
				data[ind] = Convert.ToByte(data[ind] ^ state.sbox[t]);
			}
		}

		/// <summary>
		/// Encryption with a blob key
		/// </summary>
		public static void Crypt_blob(ref byte[] data, int len, ref byte[] key) 
		{
			var state = new rc4_state();

			RC4_init(ref state, ref key);
			Crypt_Sbox(ref state, ref data, len);
		}

		/// <summary>
		/// A variant that assumes a 16 byte key.
		/// </summary>
		public static void RC4_Crypt(ref byte[] data, byte[] keystr, int len)
		{
			var key = new byte[16];
			Array.Copy(keystr, 0, key, 0, key.Length);

			Crypt_blob(ref data, len, ref key);
			Array.Clear(key, 0, key.Length);
		}
	}
}
