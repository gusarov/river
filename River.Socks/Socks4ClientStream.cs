using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace River.Socks
{
	public class Socks4ClientStream : SocksClientStream
	{
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

			var buffer = new byte[1024];
			var b = 0;
			buffer[b++] = 0x04; // ver
			buffer[b++] = 0x01; // command = stream
			buffer[b++] = (byte)(targetPort >> 8); // port high
			buffer[b++] = (byte)(targetPort); // port low

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

			bool dnsMode = false;
			if (ipv4 == null)
			{
				if (proxyDns != false)
				{
					// socks4a - DNS after fake ip
					dnsMode = true;
					buffer[b++] = 0x00;
					buffer[b++] = 0x00;
					buffer[b++] = 0x00;
					buffer[b++] = 0x01;
				}
				else
				{
					throw new Exception($"IPv4 for {targetHost} is not resolved");
				}
			}
			else
			{
				ipv4.GetAddressBytes().CopyTo(buffer, b);
				b += 4;
			}

			buffer[b++] = 0x00; // null terminated id string

			if (dnsMode)
			{
				var targetHostName = Utils.Utf8.GetBytes(targetHost);
				targetHostName.CopyTo(buffer, b);
				b += targetHostName.Length;
				buffer[b++] = 0x00; // null terminated string
			}

			stream.Write(buffer, 0, b);
			stream.Flush();
			
			// var response = new byte[8];
			var c = stream.Read(buffer, 0, 8);
			if (c != 8)
			{
				throw new Exception("Answer is too short");
			}
			if (buffer[0] != 0x00)
			{
				throw new Exception($"First byte of responce expected to be 0x00 actual {buffer[0]:X}");
			}
			if (buffer[1] != 0x5A)
			{
				throw new Exception($"First byte of responce expected to be 0x5A actual {buffer[1]:X}");
			}
		}

	}
}