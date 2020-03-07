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
		static Trace Trace = River.Trace.Default;

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

		public override void Route(string targetHost, int targetPort, bool? proxyDns = null)
		{
			using (var prof = new ProfilingScope("Socks5 Route"))
			{
				if (targetHost is null)
				{
					throw new ArgumentNullException(nameof(targetHost));
				}

				var stream = Stream;

				var buf = new byte[1024];
				var b = 0;

				// send authentication header
				buf[b++] = 0x05; // Socks ver = 5
				buf[b++] = 0x01; // count of auth methods supported
				buf[b++] = 0x00; // #1 - no auth (0x00)
				stream.Write(buf, 0, b);
				stream.Flush();
				var count = stream.Read(buf, 0, 2); // auth response
				if (count != 2)
				{
					throw new Exception("Server must respond with 2 bytes (response 1)");
				}
				if (buf[0] != 0x05)
				{
					throw new Exception("Server do not support v5 (response 1)");
				}
				if (buf[1] != 0x00)
				{
					throw new Exception("Server requires authentication");
				}

				// here authentication handshake can be added, but I don't see any reason to add clear text passwords

				// send the actual request
				b = 0;
				buf[b++] = 0x05; // ver = 5
				buf[b++] = 0x01; // command = stream
				buf[b++] = 0x00; // reserved

				var targetIsIp = IPAddress.TryParse(targetHost, out var ip);
				if (!targetIsIp) // if targetHost is IP - just use IP, otherwise:
				{
					var dns = Dns.GetHostAddresses(targetHost);
					var ipv4 = dns.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
					var ipv6 = dns.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetworkV6);

					ip = ipv4 ?? ipv6; // have to take ipv4 first because ipv6 is not working most of the times and HappyEyeballs is not possible via socks chain due to single connection. Browser will do HappyEyeballs
				}

				if (!targetIsIp && proxyDns != false || proxyDns == true) // forward the targetHost name
				{
					buf[b++] = 0x03; // adress type = domain name
					buf[b++] = checked((byte)targetHost.Length); // len
					Utils.Ascii.GetBytes(targetHost, 0, targetHost.Length, buf, b); // target host
					b += targetHost.Length;
				}
				else if (ip != null && ip.AddressFamily == AddressFamily.InterNetworkV6)
				{
					buf[b++] = 0x04; // adress type = IPv6
					ip.GetAddressBytes().CopyTo(buf, b);
					b += 16;
				}
				else if (ip != null && ip.AddressFamily == AddressFamily.InterNetwork)
				{
					buf[b++] = 0x01; // adress type = IPv4
									 // ip.GetAddressBytes().CopyTo(buf, b);
									 // b += 4;
					var a = ip.Address;
					buf[b++] = (byte)(a >> 0);
					buf[b++] = (byte)(a >> 8);
					buf[b++] = (byte)(a >> 16);
					buf[b++] = (byte)(a >> 24);
				}
				else
				{
					throw new Exception("Host is not resolved: " + targetHost);
				}
				buf[b++] = (byte)(targetPort >> 8);
				buf[b++] = (byte)(targetPort);
				stream.Write(buf, 0, b);
				stream.Flush();

				// response
				var c = stream.Read(buf, 0, 4);
				if (c < 4)
				{
					throw new Exception("Server did not sent full response (response 2)");
				}
				if (buf[0] != 0x05)
				{
					throw new Exception("Server does not support v5 (response 2)");
				}
				if (buf[1] != 0x00)
				{
					var msg = $"Server response: {buf[1]:X}: {GetResponseErrorMessage(buf[1])}";
					Trace.WriteLine(TraceCategory.NetworkingData, msg);
					throw new Exception(msg);
				}
				// ignore reserved buf[2] byte
				// read only required number of bytes depending on address type
				switch (buf[3])
				{
					case 1:
						// IPv4
						c += stream.Read(buf, c, 4);
						break;
					case 3:
						// Name
						c += stream.Read(buf, c, 1);
						c += stream.Read(buf, c, buf[c - 1]); // 256 max... no reason to protect from owerflow
						break;
					case 4:
						// IPv6
						c += stream.Read(buf, c, 16);
						break;
					default:
						throw new Exception("Response address type not supported!");
				}
				b += stream.Read(buf, b, 2); // port
											 // we don't need those values because it is stream, not bind
			}
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