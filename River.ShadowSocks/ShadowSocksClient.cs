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
	public class ShadowSocksClient : SimpleNetworkStream
	{
		readonly ChaCha20B _chachaEncrypt;
		ChaCha20B _chachaDecrypt;

		// Stream _underlying; // direct network stream
		Stream _stream; // crypto stream
		TcpClient _client;
		const int _nonceLen = 8;

		static Encoding _encoding = new UTF8Encoding(false, false);
		static MD5 _md5 = MD5.Create();

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

		public ShadowSocksClient(string chachaPassword)
		{
			_key = Kdf(chachaPassword);
			_nonce = Guid.NewGuid().ToByteArray().Take(_nonceLen).ToArray();
			_chachaEncrypt = new ChaCha20B(_key, _nonce, 0);
		}

		/*
		public ShadowSocksClient(string proxyHost, int proxyPort, string targetHost, int targetPort, bool? proxyDns = null)
			: base(proxyHost, proxyPort, targetHost, targetPort, proxyDns)
		{
		}
		*/

		public void Plug(string proxyHost, int proxyPort)
		{
			_client = new TcpClient(proxyHost, proxyPort);
			if (_client != null)
			{
				_client.NoDelay = true;
				_client.Client.NoDelay = true;
			}
			var stream = _client.GetStream();
			Plug(stream);
		}

	
		public void Plug(Stream stream)
		{
			_stream = new MustFlushStream(new CustomCryptoStream(stream, Encrypt, Decrypt));
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
			_stream.Write(buffer, offset, count);
			_stream.Flush();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return _stream.Read(buffer, offset, count);
		}

		public void Route(string targetHost, int targetPort, bool? proxyDns = null)
		{
			// _stream.Write(new byte[] { });
			// _underlying.Write(new byte[] { 1, 2 });
			// _stream.Write(new byte[] { });
			// _stream.Write(new byte[] { 1, 2});
			// _underlying.Write(new byte[] { 1, 2, 3, 4 });
			// _underlying.Write(new byte[] { 1, 2, 3, 4, 5 });
			// _stream.Write(new byte[] { 1, 2, 3, 4, 5 });

			// send authentication header

			/*
			_stream.Write(new byte[] {
				0x05, // ver = 5
				0x01, // count of auth methods supported
				0x00, // #1 - no auth
			});

			var authResponse = new byte[2];
			var count = _stream.Read(authResponse, 0, authResponse.Length);
			if (count != 2)
			{
				throw new Exception("Server must respond with 2 bytes (response 1)");
			}
			if (authResponse[0] != 0x05)
			{
				throw new Exception("Server do not support v5 (response 1)");
			}
			if (authResponse[1] != 0x00)
			{
				throw new Exception("Server requires authentication");
			}
			// here authentication handshake can be added, but I don't see any reason to add clear text passwords


			// send the actual request
			_stream.WriteByte(0x05); // ver = 5
			_stream.WriteByte(0x01); // command = stream
			_stream.WriteByte(0x00); // reserved
			*/

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
				_stream.WriteByte(0x03); // adress type = domain name
				var targetHostName = _utf8.GetBytes(targetHost);
				_stream.WriteByte(checked((byte)targetHostName.Length)); // len
				_stream.Write(targetHostName, 0, targetHostName.Length); // target host
			}
			else if (ip != null && ip.AddressFamily == AddressFamily.InterNetworkV6)
			{
				_stream.WriteByte(0x04); // adress type = IPv6
				var buf = ip.GetAddressBytes();
				_stream.Write(buf, 0, 16);
			}
			else if (ip != null && ip.AddressFamily == AddressFamily.InterNetwork)
			{
				_stream.WriteByte(0x01); // adress type = IPv4
				var buf = ip.GetAddressBytes();
				_stream.Write(buf, 0, 4);
			}
			else
			{
				throw new Exception("Host is not resolved: " + targetHost);
			}
			_stream.Write(GetPortBytes(targetPort), 0, 2); // target port

			/*
			_stream.Flush();

			// response
			var response = new byte[1024];
			var readed = _stream.Read(response, 0, 4);
			if (readed < 4)
			{
				throw new Exception("Server not sent full response");
			}
			if (response[0] != 0x05)
			{
				throw new Exception("Server not supports v5 (response 2)");
			}
			if (response[1] != 0x00)
			{
				throw new Exception($"Server response: {response[1]:X}: {GetResponseErrorMessage(response[1])}");
			}
			// ignore reserved response[2] byte
			// read only required number of bytes depending on address type
			switch (response[3])
			{
				case 1:
					// IPv4
					readed += _stream.Read(response, readed, 4);
					break;
				case 3:
					// Name
					readed += _stream.Read(response, readed, 1);
					readed += _stream.Read(response, readed, response[readed - 1]); // 256 max... no reason to protect from owerflow
					break;
				case 4:
					// IPv6
					readed += _stream.Read(response, readed, 16);
					break;
				default:
					throw new Exception("Response address type not supported!");
			}

			readed += _stream.Read(response, readed, 2); // port
														 // we don't need those values because it is stream, not bind
			*/

		}

		protected static byte[] GetPortBytes(int targetPort)
		{
			var portBuf = BitConverter.GetBytes(checked((ushort)targetPort));
			if (BitConverter.IsLittleEndian)
			{
				portBuf = new[] { portBuf[1], portBuf[0], };
			}
#if DEBUG
			if (portBuf.Length != 2)
			{
				throw new Exception("Fatal: portBuf must be 2 bytes");
			}
#endif
			return portBuf;
		}

		string GetResponseErrorMessage(byte responseCode)
		{
			switch (responseCode)
			{
				case 0:
					return "OK";
				case 1:
					return "General SOCKS server failure";
				case 2:
					return "Connection not allowed by ruleset";
				case 3:
					return "Network unreachable";
				case 4:
					return "Host unreachable";
				case 5:
					return "Connection refused";
				case 6:
					return "TTL expired";
				case 7:
					return "Command not supported";
				case 8:
					return "Address type not supported";
				default:
					return "Unknown";
			}
		}
	}
}
