using River;
using River.Common;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace River.Socks
{
	public class Socks4ClientStream : SocksClientStream
	{
		public Socks4ClientStream()
		{

		}

		public Socks4ClientStream(string proxyHost, int proxyPort, string targetHost, int targetPort, bool? proxyDns = null)
		{
			this.Plug(proxyHost, proxyPort);
			Route(targetHost, targetPort, proxyDns);
		}

		public Socks4ClientStream(Stream stream, string targetHost, int targetPort, bool? proxyDns = null)
		{
			Plug(null, stream);
			Route(targetHost, targetPort, proxyDns);
		}

		public void Plug(string host, int port)
		{
			ClientStreamExtensions.Plug(this, host, port);
		}

		public void Plug(Stream stream)
		{
			Plug(null, stream);
		}

		// enable write cache
		public override void Plug(Uri uri, Stream stream) => base.Plug(uri, new MustFlushStream(stream));

		bool _routed;

		public override async void Route(string targetHost, int targetPort, bool? proxyDns = null)
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
			stream.Flush(); // todo do not flush now, let it be write-cached

			// var response = new byte[8];
			// first await:
			var c = await stream.ReadAsync(buffer, 0, 8); // just schecule a 8 bytes read. It will read nothing till actual write-flush happens
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