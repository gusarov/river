using System;
using System.IO;
using System.Net;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using River.Socks;

namespace River.V2.Tests
{

	public class HttpBinTest
	{

		protected static string TestConnction(Stream client)
		{
			var request = Encoding.ASCII.GetBytes("GET /get HTTP/1.0\r\nConnection: keep-alive\r\n\r\n");
			client.Write(request, 0, request.Length);

			var sb = new StringBuilder();

			var buf = new byte[1024];
			using (var ms = new MemoryStream())
			{
				int received;
				do
				{
					received = client.Read(buf, 0, buf.Length);
					ms.Write(buf, 0, received);
				} while (received == buf.Length);
				var response = Encoding.UTF8.GetString(ms.ToArray());
				Console.WriteLine(response);
				sb.AppendLine(response);
				Assert.IsTrue(response.Contains("nginx"));
			}

			client.Write(request, 0, request.Length);
			using (var ms = new MemoryStream())
			{
				int received;
				do
				{
					received = client.Read(buf, 0, buf.Length);
					ms.Write(buf, 0, received);
				} while (received == buf.Length);
				var response = Encoding.UTF8.GetString(ms.ToArray());
				Console.WriteLine(response);
				sb.AppendLine(response);
				Assert.IsTrue(response.Contains("nginx"));
			}
			return sb.ToString();
		}
	}

	[TestClass]
	public class ChainTests : HttpBinTest
	{
		[TestMethod]
		public void Should_chain_3_socks()
		{
			var proxy1 = new SocksServer(new ListenerConfig
			{
				EndPoints =
				{
					new IPEndPoint(IPAddress.Loopback, 2001),
					new IPEndPoint(IPAddress.IPv6Loopback, 2001),
				},
			});

			var proxy2 = new SocksServer(new ListenerConfig
			{
				EndPoints =
				{
					new IPEndPoint(IPAddress.Loopback, 2002),
					new IPEndPoint(IPAddress.IPv6Loopback, 2002),
				},
			});

			var proxy3 = new SocksServer(new ListenerConfig
			{
				EndPoints =
				{
					new IPEndPoint(IPAddress.Loopback, 2003),
					new IPEndPoint(IPAddress.IPv6Loopback, 2003),
				},
			});

			var proxy = new SocksServer(new ListenerConfig
			{
				EndPoints =
				{
					new IPEndPoint(IPAddress.Loopback, 2000),
					new IPEndPoint(IPAddress.IPv6Loopback, 2000),
				},
			})
			{
				Forwarder = new SocksForwarder("localhost", 2001)
				{
					NextForwarder = new SocksForwarder("localhost", 2002)
					{
						NextForwarder = new SocksForwarder("localhost", 2003)
						{

						},
					},
				},
			};

			var cli = new Socks4Client("localhost", 2000, "httpbin.org", 80);
			TestConnction(cli);
		}
	}
}
