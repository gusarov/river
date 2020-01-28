using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace River
{
	public class Socks5Client : SocksClient
	{
		public Socks5Client()
		{

		}

		public Socks5Client(string proxyHost, int proxyPort, string targetHost, int targetPort, bool? proxyDns = null)
		{
			Connect(proxyHost, proxyPort, targetHost, targetPort, proxyDns);
		}

		public override void Connect(string proxyHost, int proxyPort, string targetHost, int targetPort, bool? proxyDns = null)
		{
			ConnectBase(proxyHost, proxyPort);

			// send authentication header
			_stream.WriteByte(0x05); // ver = 5
			_stream.WriteByte(0x01); // count of auth methods supported
			_stream.WriteByte(0x00); // #1 - no auth

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

			var ipv4 = proxyDns == true
				? null
				: Dns.GetHostAddresses(targetHost).FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
			var ipv6 = proxyDns == true
				? null
				: Dns.GetHostAddresses(targetHost).FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetworkV6);

			if (proxyDns != false) // forward the name by default
			{
				_stream.WriteByte(0x03); // adress type = domain name
				var targetHostName = _utf8.GetBytes(targetHost);
				_stream.WriteByte(checked((byte)targetHostName.Length)); // len
				_stream.Write(targetHostName, 0, targetHostName.Length); // target host
			}
			else if (ipv6 != null) // as usual - give a priority, BUT target proxy might be not IPv6 ready
			{
				_stream.WriteByte(0x04); // adress type = IPv6
				var buf = ipv6.GetAddressBytes();
				_stream.Write(buf, 0, 16);
			}
			else if (ipv4 != null)
			{
				_stream.WriteByte(0x01); // adress type = IPv4
				var buf = ipv4.GetAddressBytes();
				_stream.Write(buf, 0, 4);
			}
			else
			{
				throw new Exception("Host is not resolved");
			}
			_stream.Write(GetProtBytes(targetPort), 0, 2); // target port

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
					readed  += _stream.Read(response, readed, 16);
					break;
				default:
					throw new Exception("Response address type not supported!");
			}
			readed += _stream.Read(response, readed, 2); // port
			// we don't need those values because it is stream, not bind
			// from now, any subsequent byte is a part of the stream, including remaining part of the network buffer
			_client.NoDelay = true;
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