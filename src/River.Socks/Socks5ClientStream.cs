using River.Common;
using River.Socks;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace River.Socks
{
	public class Socks5ClientStream : SocksClientStream
	{
		public Socks5ClientStream()
		{

		}

		public Socks5ClientStream(string proxyHost, int proxyPort, string targetHost, int targetPort, bool? proxyDns = null)
		{
			Plug(proxyHost, proxyPort);
			Route(targetHost, targetPort, proxyDns);
		}

		public void Plug(string proxyHost, int proxyPort)
		{
			ClientStreamExtensions.Plug(this, proxyHost, proxyPort);
		}

		public override void Plug(Uri uri, Stream stream)
		{
			// Socks5 implementation here is not very efficient here, so, let's just buffer writes
			Stream = new MustFlushStream(stream);
		}

		public override void Route(string targetHost, int targetPort, bool? proxyDns = null)
		{
			var stream = Stream;

			// send authentication header
			stream.Write(new byte[] {
				0x05, // ver = 5
				0x01, // count of auth methods supported
				0x00, // #1 - no auth
			});

			stream.Flush();
			var authResponse = new byte[2];
			var count = stream.Read(authResponse, 0, authResponse.Length);
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
			stream.WriteByte(0x05); // ver = 5
			stream.WriteByte(0x01); // command = stream
			stream.WriteByte(0x00); // reserved

			var targetIsIp = IPAddress.TryParse(targetHost, out var ip);
			if (proxyDns != false && !targetIsIp) // if targetHost is IP - just use IP
			{
				var dns = Dns.GetHostAddresses(targetHost);
				var ipv4 = dns.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
				var ipv6 = dns.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetworkV6);

				ip = ipv6 ?? ipv4;
			}
			if (!targetIsIp && proxyDns != false || proxyDns == true) // forward the targetHost name
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
			stream.WriteByte((byte)(targetPort >> 8)); // target port
			stream.WriteByte((byte)targetPort); // target port
			stream.Flush();

			// response
			var response = new byte[1024];
			var readed = stream.Read(response, 0, 4);
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
				var msg = $"Server response: {response[1]:X}: {GetResponseErrorMessage(response[1])}";
				Trace.WriteLine(msg);
				throw new Exception(msg);
			}
			// ignore reserved response[2] byte
			// read only required number of bytes depending on address type
			switch (response[3])
			{
				case 1:
					// IPv4
					readed += stream.Read(response, readed, 4);
					break;
				case 3:
					// Name
					readed += stream.Read(response, readed, 1);
					readed += stream.Read(response, readed, response[readed - 1]); // 256 max... no reason to protect from owerflow
					break;
				case 4:
					// IPv6
					readed  += stream.Read(response, readed, 16);
					break;
				default:
					throw new Exception("Response address type not supported!");
			}
			readed += stream.Read(response, readed, 2); // port
			// we don't need those values because it is stream, not bind
			
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