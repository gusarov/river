using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using River.ChaCha;
using River.Common;
using River.Internal;

namespace River.ShadowSocks
{
	public class ShadowSocksClientStream : ClientStream
	{
		ChaCha20 _chachaEncrypt;
		ChaCha20 _chachaDecrypt;

		const int _nonceLen = 8;

		static Encoding _encoding = new UTF8Encoding(false, false);

		byte[] _nonce;
		byte[] _key;
		byte[] _serverNonce;

		(string algo, string pass) GetUserInfo(Uri uri)
		{
			var info = uri.UserInfo;
			var i = info.IndexOf(':');
			if (i >= 0)
			{
				return (info.Substring(0, i), info.Substring(i + 1));
			}
			return (null, null);
		}

		void ConfigureUri(Uri proxyUri)
		{
			if (proxyUri is null)
			{
				throw new ArgumentNullException(nameof(proxyUri));
			}
			var (user, pass) = GetUserInfo(proxyUri);
			_key = ChaCha20.Kdf(pass);
			_nonce = Guid.NewGuid().ToByteArray().Take(_nonceLen).ToArray();
			_chachaEncrypt = new ChaCha20(_key, _nonce, 0);
		}

		public ShadowSocksClientStream()
		{

		}

		public ShadowSocksClientStream(string algorythm, string password, string targetProxyHost, int targetProxyPort, string targetHost, int targetPort, bool? proxyDns = null)
		{
			Plug(new Uri($"ss://{algorythm}:{password}@{targetProxyHost}:{targetProxyPort}"));
			Route(targetHost, targetPort, proxyDns);
		}

		public override void Plug(Uri proxyUri)
		{
			ConfigureUri(proxyUri);
			base.Plug(proxyUri); // base performs regular Tcp connection
			Plug(Client.GetStream()); // but we are wrapping it here a little bit differently
		}

		public override void Plug(Uri proxyUri, Stream stream)
		{
			ConfigureUri(proxyUri);
			Plug(stream);
		}

		void Plug(Stream stream)
		{
			Stream = new MustFlushStream(new CustomStream(stream, Encrypt, Decrypt));
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
				_chachaDecrypt = new ChaCha20(_key, _serverNonce);
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
			IPAddress ipr4 = null;
			IPAddress ipr6 = null;
			var targetIsIp = IPAddress.TryParse(targetHost, out var ip);
			if (!targetIsIp)
			{
				ipr4 = Dns.GetHostAddresses(targetHost).FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);

				ipr6 = Dns.GetHostAddresses(targetHost).FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetworkV6);
				ip = ipr4 ?? ipr6;
			}

			if (!targetIsIp && proxyDns != false || proxyDns == true) // forward the targetHost name
			{
				stream.WriteByte(0x03); // adress type = domain name
				var targetHostName = _utf8.GetBytes(targetHost);
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
