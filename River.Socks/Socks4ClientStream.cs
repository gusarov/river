using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace River.Socks
{
	public class Socks4ClientStream : SocksClientStream
	{
		static Socks4ClientStream()
		{
			Resolver.RegisterSchema<Socks4ClientStream>("socks4");
		}

		public Socks4ClientStream()
		{

		}

		public Socks4ClientStream(string proxyHost, int proxyPort, string targetHost, int targetPort, bool? proxyDns = null)
		{
			Plug(proxyHost, proxyPort);
			Route(targetHost, targetPort, proxyDns);
		}

		public Socks4ClientStream(Stream stream, string targetHost, int targetPort, bool? proxyDns = null)
		{
			Plug(stream);
			Route(targetHost, targetPort, proxyDns);
		}

		bool _routed;

		public override void Route(string targetHost, int targetPort, bool? proxyDns = null)
		{
			if (targetHost is null)
			{
				throw new ArgumentNullException(nameof(targetHost));
			}

			var stream = Stream;
			if (_routed)
			{
				throw new Exception("Already been routed");
			}
			_routed = true;
			stream.Write(
				0x04, // ver
				0x01 // command = stream
				);

			stream.Write(Utils.GetPortBytes(targetPort), 0, 2); // target port

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
			stream.Write(ipMessage, 0, 4); // target ip
											// var userId = _utf8.GetBytes("River");
											// _stream.Write(userId, 0, userId.Length); // userID
			stream.WriteByte(0); // null terminated of id
			if (ipMessage[0] == 0) // dns name mode
			{
				var targetHostName = Utils.Utf8.GetBytes(targetHost);
				stream.Write(targetHostName, 0, targetHostName.Length); // target host
				stream.WriteByte(0); // null terminated
			}
			stream.Flush();
			var response = new byte[8];
			var c = stream.Read(response, 0, 8);
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
		}

	}
}