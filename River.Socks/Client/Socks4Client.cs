using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace River
{
	public class Socks4Client : SocksClient
	{
		public Socks4Client()
		{
			
		}

		public Socks4Client(string proxyHost, int proxyPort, string targetHost, int targetPort, bool? proxyDns = null)
		{
			Connect(proxyHost, proxyPort, targetHost, targetPort, proxyDns);
		}

		public override void Connect(string proxyHost, int proxyPort, string targetHost, int targetPort, bool? proxyDns = null)
		{
			ConnectBase(proxyHost, proxyPort);

			_stream.Write(
				0x04, // ver
				0x01 // command = stream
				); 

			_stream.Write(GetProtBytes(targetPort), 0, 2); // target port

			var ipv4 = proxyDns == true
				? null
				: Dns.GetHostAddresses(targetHost).FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
			if (ipv4 == null && IPAddress.TryParse(targetHost, out var ipv4g))
			{
				if (ipv4g.AddressFamily == AddressFamily.InterNetwork)
				{
					ipv4 = ipv4g;
				}
			}

			byte[] ipMessage;
			if (ipv4 == null)
			{
				if (proxyDns != false)
				{
					ipMessage = new byte[] { 0, 0, 0, 1 };
				}
				else
				{
					throw new Exception($"IPv4 for {targetHost} is not resolved");
				}
			}
			else
			{
				ipMessage = ipv4.GetAddressBytes();
			}
			if (ipMessage.Length != 4)
			{
				throw new Exception("Fatal: ipMessage must be 4 bytes");
			}
			_stream.Write(ipMessage, 0, 4); // target ip
			// var userId = _utf8.GetBytes("River");
			// _stream.Write(userId, 0, userId.Length); // userID
			_stream.WriteByte(0); // null terminated of id
			if (ipMessage[0] == 0) // dns name mode
			{
				var targetHostName = _utf8.GetBytes(targetHost);
				_stream.Write(targetHostName, 0, targetHostName.Length); // target host
				_stream.WriteByte(0); // null terminated
			}
			var response = new byte[8];
			var c = _stream.Read(response, 0, 8);
			if (c != 8)
			{
				throw new Exception("Answer is too short");
			}
			if (response[0] != 0x00)
			{
				throw new Exception($"First byte of responce expected to be 0x00 actual {response[0]:X}");
			}
			if (response[1] != 0x5A)
			{
				throw new Exception($"First byte of responce expected to be 0x5A actual {response[1]:X}");
			}

			// now connected
			_client.Client.NoDelay = true;
		}

	}
}