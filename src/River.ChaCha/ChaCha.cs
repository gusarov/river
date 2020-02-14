/*
 * Copyright (c) 2015, 2018 Scott Bennett
 *           (c) 2018 Kaarlo Räihä
 *           (c) 2020 Dmitry Gusarov
 *
 * Permission to use, copy, modify, and distribute this software for any
 * purpose with or without fee is hereby granted, provided that the above
 * copyright notice and this permission notice appear in all copies.
 *
 * THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
 * WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
 * ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
 * WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
 * ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
 * OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
 */

using System;
using System.Text;
using System.Runtime.CompilerServices; // For MethodImplOptions.AggressiveInlining
using System.Security.Cryptography;

namespace River.ChaCha
{
	/// <summary>
	/// Class that can be used for ChaCha20 encryption / decryption
	/// </summary>
	public sealed class ChaCha20
	{
		/// <summary>
		/// Key lenght in bytes
		/// </summary>
		public const int KeyLength = 32;

		/// <summary>
		/// How many bytes are processed per loop
		/// </summary>
		public const int BlockSize = 64;

		private const int _stateLength = 16;

		/// <summary>
		/// The ChaCha20 state (aka "context")
		/// </summary>
		private uint[] _state = new uint[_stateLength];

		/// <summary>
		/// Set up a new ChaCha20 state. The lengths of the given parameters are checked before encryption happens.
		/// </summary>
		/// <remarks>
		/// See <a href="https://tools.ietf.org/html/rfc7539#page-10">ChaCha20 Spec Section 2.4</a> for a detailed description of the inputs.
		/// </remarks>
		/// <param name="key">
		/// A 32-byte (256-bit) key, treated as a concatenation of eight 32-bit little-endian integers
		/// </param>
		/// <param name="nonce">
		/// A 12-byte (96-bit) nonce, treated as a concatenation of three 32-bit little-endian integers
		/// </param>
		/// <param name="counter">
		/// A 4-byte (32-bit) block counter, treated as a 32-bit little-endian integer
		/// </param>
		public ChaCha20(byte[] key, byte[] nonce, uint counter = 0)
		{
			KeySetup(key);
			IvSetup(nonce, counter);
		}

		public ChaCha20(string password, byte[] nonce, uint counter = 0)
		{
			KeySetup(Kdf(password));
			IvSetup(nonce, counter);
		}

		public ChaCha20(string password, int nonceLen, uint counter = 0)
			: this(Kdf(password), nonceLen, counter)
		{
		}

		public ChaCha20(byte[] key, int nonceLen, uint counter = 0)
		{
			KeySetup(key);

			var rnd = Guid.NewGuid().ToByteArray();
			var nonce = new byte[nonceLen];
			Array.Copy(rnd, 0, nonce, 0, nonceLen);
			IvSetup(nonce, counter);
		}

		static Encoding _utf8 = new UTF8Encoding(false, false);
#pragma warning disable CA5351 // Do Not Use Broken Cryptographic Algorithms
		static Lazy<MD5> _lazyMd5 = new Lazy<MD5>(() => MD5.Create());
		static MD5 _md5 => _lazyMd5.Value;
#pragma warning restore CA5351 // Do Not Use Broken Cryptographic Algorithms

		public static byte[] Kdf(string password)
		{
			if (password is null)
			{
				throw new ArgumentNullException(nameof(password));
			}

			var pwd = _utf8.GetBytes(password);
			var hash1 = _md5.ComputeHash(pwd);
			var buf = new byte[hash1.Length + pwd.Length];
			hash1.CopyTo(buf, 0);
			pwd.CopyTo(buf, hash1.Length);
			var hash2 = _md5.ComputeHash(buf);

			buf = new byte[hash1.Length + hash2.Length];
			hash1.CopyTo(buf, 0);
			hash2.CopyTo(buf, 16);

			return buf;
		}

		// These are the same constants defined in the reference implementation.
		// http://cr.yp.to/streamciphers/timings/estreambench/submissions/salsa20/chacha8/ref/chacha.c
		private static readonly byte[] _sigma = Encoding.ASCII.GetBytes("expand 32-byte k");
		private static readonly byte[] _tau = Encoding.ASCII.GetBytes("expand 16-byte k");

		/// <summary>
		/// Set up the ChaCha state with the given key. A 32-byte key is required and enforced.
		/// </summary>
		/// <param name="key">
		/// A 32-byte (256-bit) key, treated as a concatenation of eight 32-bit little-endian integers
		/// </param>
		private void KeySetup(byte[] key)
		{
			if (key == null)
			{
				throw new ArgumentNullException(nameof(key));
			}

			if (key.Length != KeyLength)
			{
				throw new ArgumentException($"Key length must be {KeyLength}. Actual: {key.Length}");
			}

			_state[4] = Util.U8To32Little(key, 0);
			_state[5] = Util.U8To32Little(key, 4);
			_state[6] = Util.U8To32Little(key, 8);
			_state[7] = Util.U8To32Little(key, 12);

			var constants = (key.Length == KeyLength) ? _sigma : _tau;
			var keyIndex = key.Length - 16;

			_state[8] = Util.U8To32Little(key, keyIndex + 0);
			_state[9] = Util.U8To32Little(key, keyIndex + 4);
			_state[10] = Util.U8To32Little(key, keyIndex + 8);
			_state[11] = Util.U8To32Little(key, keyIndex + 12);

			_state[0] = Util.U8To32Little(constants, 0);
			_state[1] = Util.U8To32Little(constants, 4);
			_state[2] = Util.U8To32Little(constants, 8);
			_state[3] = Util.U8To32Little(constants, 12);
		}

		// extra copy of nonce
		public byte[] Nonce { get; private set; }

		/// <summary>
		/// Set up the ChaCha state with the given nonce (aka Initialization Vector or IV) and block counter. A 12-byte nonce and a 4-byte counter are required.
		/// </summary>
		/// <param name="nonce">
		/// A 12-byte (96-bit) nonce, treated as a concatenation of three 32-bit little-endian integers
		/// </param>
		/// <param name="counter">
		/// A 4-byte (32-bit) block counter, treated as a 32-bit little-endian integer
		/// </param>
		public void IvSetup(byte[] nonce, uint counter = 0)
		{
			if (nonce == null)
			{
				// There has already been some state set up. Clear it before exiting.
				throw new ArgumentNullException(nameof(nonce));
			}
			Nonce = nonce;

			if (nonce.Length != 8 && nonce.Length != 12)
			{
				// There has already been some state set up. Clear it before exiting.
				throw new ArgumentException($"Nonce length must be 8 or 12. Actual: {nonce.Length}");
			}

			_state[12] = counter;
			if (nonce.Length == 8)
			{
				_state[13] = 0;
				_state[14] = Util.U8To32Little(nonce, 0);
				_state[15] = Util.U8To32Little(nonce, 4);
			}
			else if (nonce.Length == 12)
			{
				_state[13] = Util.U8To32Little(nonce, 0);
				_state[14] = Util.U8To32Little(nonce, 4);
				_state[15] = Util.U8To32Little(nonce, 8);
			}
		}

		public byte[] EncryptBytes(byte[] data)
		{
			var buf = new byte[data.Length];
			Crypt(data, 0, buf, 0, data.Length);
			return buf;
		}

		public void EncryptBytes(byte[] output, byte[] data, int count = -1)
		{
			if (count == -1) count = data.Length;
			Crypt(data, 0, output, 0, data.Length);
		}

		public byte[] DecryptBytes(byte[] data)
		{
			var buf = new byte[data.Length];
			Crypt(data, 0, buf, 0, data.Length);
			return buf;
		}

		public void DecryptBytes(byte[] output, byte[] data, int count = -1)
		{
			if (count == -1) count = data.Length;
			Crypt(data, 0, output, 0, data.Length);
		}

		/// <summary>
		/// Keep track of how many bytes already fulfilled in current block
		/// </summary>
		int _currentBlockBytes;

		public int Encrypt(byte[] sourceArray, int sourceIndex, byte[] destinationArray, int destinationIndex, int length)
		{
			Crypt(sourceArray, sourceIndex, destinationArray, destinationIndex, length);
			return length;
		}

		public int Dencrypt(byte[] sourceArray, int sourceIndex, byte[] destinationArray, int destinationIndex, int length)
		{
			Crypt(sourceArray, sourceIndex, destinationArray, destinationIndex, length);
			return length;
		}

		public void Inplace(byte[] buffer, int pos, int length)
		{
			Crypt(buffer, pos, buffer, pos, length);
		}

		/// <summary>
		/// Encrypt or decrypt the buffers (this is bidirectional for chacha due to xor)
		/// </summary>
		public void Crypt(byte[] sourceArray, int sourceIndex, byte[] destinationArray, int destinationIndex, int length)
		{
			if (sourceArray == null)
			{
				throw new ArgumentNullException(nameof(sourceArray), $"{nameof(sourceArray)} cannot be null");
			}
			if (destinationArray == null)
			{
				throw new ArgumentNullException(nameof(destinationArray), $"{nameof(destinationArray)} cannot be null");
			}

			if (sourceIndex < 0) throw new ArgumentException("index is less than zero", nameof(sourceIndex));
			if (sourceIndex > sourceArray.Length) throw new ArgumentException("index is more than buf", nameof(sourceIndex));

			if (destinationIndex < 0) throw new ArgumentException("index is less than zero", nameof(destinationIndex));
			if (destinationIndex > destinationArray.Length) throw new ArgumentException("index is more than buf", nameof(destinationIndex));

			var space = sourceArray.Length - sourceIndex;
			var data = destinationArray.Length - destinationIndex;
			if (length < 0) throw new ArgumentException("length is less than zero", nameof(length));
			if (length > space) throw new ArgumentException("length is more than data available in buf", nameof(length));
			if (length > data) throw new ArgumentException("length is more than free space available", nameof(length));

			/*
			if (numBytes < 0 || numBytes > input.Length)
			{
				throw new ArgumentOutOfRangeException("numBytes", "The number of bytes to read must be between [0..input.Length]");
			}

			if (output.Length < numBytes)
			{
				throw new ArgumentOutOfRangeException("output", $"Output byte array should be able to take at least {numBytes}");
			}
			*/

			var x = new uint[_stateLength]; // Working buffer
			var tmp = new byte[BlockSize];  // Temporary buffer

			while (length > 0)
			{
				// Copy state to working buffer
				Buffer.BlockCopy(_state, 0, x, 0, _stateLength * sizeof(uint));

				for (var i = 0; i < 10; i++)
				{
					QuarterRound(x, 0, 4, 8, 12);
					QuarterRound(x, 1, 5, 9, 13);
					QuarterRound(x, 2, 6, 10, 14);
					QuarterRound(x, 3, 7, 11, 15);

					QuarterRound(x, 0, 5, 10, 15);
					QuarterRound(x, 1, 6, 11, 12);
					QuarterRound(x, 2, 7, 8, 13);
					QuarterRound(x, 3, 4, 9, 14);
				}

				for (var i = 0; i < _stateLength; i++)
				{
					Util.ToBytes(tmp, Util.Add(x[i], _state[i]), 4 * i);
				}

				var remainedInCurrent = BlockSize - _currentBlockBytes;
				var m = length >= remainedInCurrent ? remainedInCurrent : length;
				for (var i = 0; i < m; i++)
				{
					destinationArray[i + destinationIndex] = (byte)(sourceArray[i + sourceIndex] ^ tmp[i + _currentBlockBytes]);
				}

				length -= m;
				destinationIndex += m;
				sourceIndex += m;
				_currentBlockBytes += m;
				if (_currentBlockBytes == BlockSize)
				{
					_currentBlockBytes = 0;
					_state[12] = Util.AddOne(_state[12]);

					// TODO Need prove of this from spec:
					if (_state[12] <= 0) // less is extra prove here. Actually counter is uint [0...]
					{
						// Stopping at 2^70 bytes per nonce is the user's responsibility
						_state[13] = Util.AddOne(_state[13]);
					}
				}
			}
		}

		/// <summary>
		/// The ChaCha Quarter Round operation. It operates on four 32-bit unsigned integers within the given buffer at indices a, b, c, and d.
		/// </summary>
		/// <remarks>
		/// The ChaCha state does not have four integer numbers: it has 16. So the quarter-round operation works on only four of them -- hence the name. Each quarter round operates on four predetermined numbers in the ChaCha state.
		/// See <a href="https://tools.ietf.org/html/rfc7539#page-4">ChaCha20 Spec Sections 2.1 - 2.2</a>.
		/// </remarks>
		/// <param name="x">A ChaCha state (vector). Must contain 16 elements.</param>
		/// <param name="a">Index of the first number</param>
		/// <param name="b">Index of the second number</param>
		/// <param name="c">Index of the third number</param>
		/// <param name="d">Index of the fourth number</param>
		private static void QuarterRound(uint[] x, uint a, uint b, uint c, uint d)
		{
			x[a] = Util.Add(x[a], x[b]);
			x[d] = Util.Rotate(Util.XOr(x[d], x[a]), 16);

			x[c] = Util.Add(x[c], x[d]);
			x[b] = Util.Rotate(Util.XOr(x[b], x[c]), 12);

			x[a] = Util.Add(x[a], x[b]);
			x[d] = Util.Rotate(Util.XOr(x[d], x[a]), 8);

			x[c] = Util.Add(x[c], x[d]);
			x[b] = Util.Rotate(Util.XOr(x[b], x[c]), 7);
		}

		/// <summary>
		/// Utilities that are used during compression
		/// </summary>
		static class Util
		{
			/// <summary>
			/// n-bit left rotation operation (towards the high bits) for 32-bit integers.
			/// </summary>
			/// <param name="v"></param>
			/// <param name="c"></param>
			/// <returns>The result of (v LEFTSHIFT c)</returns>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static uint Rotate(uint v, int c)
			{
				unchecked
				{
					return (v << c) | (v >> (32 - c));
				}
			}

			/// <summary>
			/// Unchecked integer exclusive or (XOR) operation.
			/// </summary>
			/// <param name="v"></param>
			/// <param name="w"></param>
			/// <returns>The result of (v XOR w)</returns>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static uint XOr(uint v, uint w)
			{
				return unchecked(v ^ w);
			}

			/// <summary>
			/// Unchecked integer addition. The ChaCha spec defines certain operations to use 32-bit unsigned integer addition modulo 2^32.
			/// </summary>
			/// <remarks>
			/// See <a href="https://tools.ietf.org/html/rfc7539#page-4">ChaCha20 Spec Section 2.1</a>.
			/// </remarks>
			/// <param name="v"></param>
			/// <param name="w"></param>
			/// <returns>The result of (v + w) modulo 2^32</returns>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static uint Add(uint v, uint w)
			{
				return unchecked(v + w);
			}

			/// <summary>
			/// Add 1 to the input parameter using unchecked integer addition. The ChaCha spec defines certain operations to use 32-bit unsigned integer addition modulo 2^32.
			/// </summary>
			/// <remarks>
			/// See <a href="https://tools.ietf.org/html/rfc7539#page-4">ChaCha20 Spec Section 2.1</a>.
			/// </remarks>
			/// <param name="v"></param>
			/// <returns>The result of (v + 1) modulo 2^32</returns>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static uint AddOne(uint v)
			{
				return unchecked(v + 1);
			}

			/// <summary>
			/// Convert four bytes of the input buffer into an unsigned 32-bit integer, beginning at the inputOffset.
			/// </summary>
			/// <param name="p"></param>
			/// <param name="inputOffset"></param>
			/// <returns>An unsigned 32-bit integer</returns>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static uint U8To32Little(byte[] p, int inputOffset)
			{
				unchecked
				{
					return ((uint)p[inputOffset]
						| ((uint)p[inputOffset + 1] << 8)
						| ((uint)p[inputOffset + 2] << 16)
						| ((uint)p[inputOffset + 3] << 24));
				}
			}

			/// <summary>
			/// Serialize the input integer into the output buffer. The input integer will be split into 4 bytes and put into four sequential places in the output buffer, starting at the outputOffset.
			/// </summary>
			/// <param name="output"></param>
			/// <param name="input"></param>
			/// <param name="outputOffset"></param>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static void ToBytes(byte[] output, uint input, int outputOffset)
			{
				unchecked
				{
					output[outputOffset] = (byte)input;
					output[outputOffset + 1] = (byte)(input >> 8);
					output[outputOffset + 2] = (byte)(input >> 16);
					output[outputOffset + 3] = (byte)(input >> 24);
				}
			}
		}
	}


}