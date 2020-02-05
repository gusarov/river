using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CSChaCha20;
using River.Common;

namespace River.ShadowSocks
{
	public class ShadowSocksClientStream : ClientStream
	{
		static ShadowSocksClientStream()
		{
			Resolver.RegisterSchema<ShadowSocksClientStream>("ss");
		}

		readonly ChaCha20B _chachaEncrypt;
		ChaCha20B _chachaDecrypt;

		const int _nonceLen = 8;

		static Encoding _encoding = new UTF8Encoding(false, false);
#pragma warning disable CA5351 // Do Not Use Broken Cryptographic Algorithms
		static MD5 _md5 = MD5.Create();
#pragma warning restore CA5351 // Do Not Use Broken Cryptographic Algorithms

		internal static byte[] Kdf(string password)
		{
			var pwd = _encoding.GetBytes(password);
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

		byte[] _nonce;
		byte[] _key;
		byte[] _serverNonce;

		public ShadowSocksClientStream(string chachaPassword)
		{
			_key = Kdf(chachaPassword);
			_nonce = Guid.NewGuid().ToByteArray().Take(_nonceLen).ToArray();
			_chachaEncrypt = new ChaCha20B(_key, _nonce, 0);
		}

		public ShadowSocksClientStream(string chachaPassword, string proxyHost, int proxyPort, string targetHost, int targetPort, bool? proxyDns = null)
			: this(chachaPassword)
		{
			Plug(proxyHost, proxyPort);
			Route(targetHost, targetPort, proxyDns);
		}

		public override void Plug(string proxyHost, int proxyPort)
		{
			base.Plug(proxyHost, proxyPort); // base performs regular Tcp connection
			Plug(Client.GetStream()); // but we are wrapping it here a little bit differently
		}

		public override void Plug(Stream stream)
		{
			Stream = new MustFlushStream(new CustomCryptoStream(stream, Encrypt, Decrypt));
		}

		byte[] _encryptBuffer = new byte[16 * 1024];
		bool _icSent;
		bool _icReceived;
		void Encrypt(Stream underlying, byte[] buffer, int offset, int count)
		{
#if DEBUG
			if (count > _encryptBuffer.Length - (_icSent ? 0 : _nonceLen))
			{
				throw new Exception("Not enough buffer to serve this request");
			}
#endif
			// var extra = _icSent ? 0 : _nonceLen;
			// var cnt = Math.Min(_encryptBuffer.Length - extra, count);
			_chachaEncrypt.Crypt(buffer, offset, _encryptBuffer, _icSent ? 0 : _nonceLen, count);
			if (!_icSent)
			{
				_nonce.CopyTo(_encryptBuffer, 0); // crypt been done with a shift to left this space for nonce
				count += _nonceLen;
				_icSent = true;
			}
			underlying.Write(_encryptBuffer, 0 , count);
		}

		byte[] _readBuffer = new byte[16 * 1024];
		int Decrypt(Stream underlying, byte[] buffer, int offset, int count)
		{
			var extra = _icReceived ? 0 : _nonceLen;
			var cnt = Math.Min(_readBuffer.Length, count + extra);

			var r = underlying.Read(_readBuffer, 0, cnt);
			if (r == 0) return 0;
			var ro = 0;

			if (!_icReceived)
			{
				_serverNonce = new byte[_nonceLen];
				Array.Copy(_readBuffer, 0, _serverNonce, 0, _nonceLen);
				_icReceived = true;
				r -= _nonceLen;
				ro += _nonceLen;
				_chachaDecrypt = new ChaCha20B(_key, _serverNonce);
			}

#if DEBUG
			if (count < r)
			{
				// this can only happen when
				throw new Exception("Underlying stream returned more than requested");
			}
#endif

			// decrypt by blocks
			_chachaDecrypt.Crypt(_readBuffer, ro, buffer, offset, r);
			return r;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			Stream.Write(buffer, offset, count);
			Stream.Flush();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return Stream.Read(buffer, offset, count);
		}

		public override void Route(string targetHost, int targetPort, bool? proxyDns = null)
		{
			var stream = Stream;
			var resolved = false;
			if (!IPAddress.TryParse(targetHost, out var ip)) // if targetHost is IP - just use IP
			{
				var ipv4 = proxyDns == true
					? null
					: Dns.GetHostAddresses(targetHost).FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);

				var ipv6 = proxyDns == true
					? null
					: Dns.GetHostAddresses(targetHost).FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetworkV6);

				ip = ipv6 ?? ipv4; // as usual - give a priority, BUT target proxy might be not IPv6 ready

				resolved = ip != null;
			}

			if (!resolved && proxyDns != false) // forward the targetHost name
			{
				stream.WriteByte(0x03); // adress type = domain name
				var targetHostName = Utils.Utf8.GetBytes(targetHost);
				stream.WriteByte(checked((byte)targetHostName.Length)); // len
				stream.Write(targetHostName, 0, targetHostName.Length); // target host
			}
			else if (ip != null && ip.AddressFamily == AddressFamily.InterNetworkV6)
			{
				stream.WriteByte(0x04); // adress type = IPv6
				var buf = ip.GetAddressBytes();
				stream.Write(buf, 0, 16);
			}
			else if (ip != null && ip.AddressFamily == AddressFamily.InterNetwork)
			{
				stream.WriteByte(0x01); // adress type = IPv4
				var buf = ip.GetAddressBytes();
				stream.Write(buf, 0, 4);
			}
			else
			{
				throw new Exception("Host is not resolved: " + targetHost);
			}
			stream.Write(Utils.GetPortBytes(targetPort), 0, 2); // target port

		}

	}
}
