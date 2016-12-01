using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace River.Tests
{
	[TestClass]
	public class SocksClientTest
	{
		const string ProxyServerName = "bs";
		const int ProxyServerPort = 1080;

		private static string TestConnction(Stream socks)
		{
			var request = Encoding.ASCII.GetBytes("GET /get HTTP/1.0\r\nConnection: keep-alive\r\n\r\n");
			socks.Write(request, 0, request.Length);

			var sb = new StringBuilder();

			var buf = new byte[1024];
			using (var ms = new MemoryStream())
			{
				int received;
				do
				{
					received = socks.Read(buf, 0, buf.Length);
					ms.Write(buf, 0, received);
				} while (received == buf.Length);
				var response = Encoding.UTF8.GetString(ms.ToArray());
				Console.WriteLine(response);
				sb.AppendLine(response);
				Assert.IsTrue(response.Contains("nginx"));
			}

			socks.Write(request, 0, request.Length);
			using (var ms = new MemoryStream())
			{
				int received;
				do
				{
					received = socks.Read(buf, 0, buf.Length);
					ms.Write(buf, 0, received);
				} while (received == buf.Length);
				var response = Encoding.UTF8.GetString(ms.ToArray());
				Console.WriteLine(response);
				sb.AppendLine(response);
				Assert.IsTrue(response.Contains("nginx"));
			}
			return sb.ToString();
		}

		[TestMethod]
		public void Should_take_data_from_test_server()
		{
			using (var cli = new TcpClient())
			{
				cli.Connect("httpbin.org", 80);
				TestConnction(cli.GetStream());
			}
		}

		[TestMethod]
		public void Should_take_data_from_test_server_via_proxy4()
		{
			using (var socks = new Socks4Client())
			{
				socks.Connect(ProxyServerName, ProxyServerPort, "httpbin.org", 80);
				TestConnction(socks);
			}
		}

		[TestMethod]
		public void Should_take_data_from_test_server_via_prox5y()
		{
			using (var socks = new Socks5Client())
			{
				socks.Connect(ProxyServerName, ProxyServerPort, "httpbin.org", 80);
				TestConnction(socks);
			}
		}
		[TestMethod]
		public void Should_take_data_from_test_server_via_proxy4a_dns()
		{
			using (var socks = new Socks4Client())
			{
				socks.Connect(ProxyServerName, ProxyServerPort, "httpbin.org", 80, true);
				TestConnction(socks);
			}
		}

		[TestMethod]
		public void Should_take_data_from_test_server_via_proxy5_dns()
		{
			using (var socks = new Socks5Client())
			{
				socks.Connect(ProxyServerName, ProxyServerPort, "httpbin.org", 80, true);
				TestConnction(socks);
			}
		}

		[TestMethod]
		public void Should_take_data_from_test_server_via_proxy4_on_river_socks_server()
		{
			using (var server = new SocksServer())
			{
				server.Listen(4545);
				using (var socks = new Socks4Client())
				{
					socks.Connect("localhost", 4545, "httpbin.org", 80, false);
					TestConnction(socks);
				}
			}
		}

		[TestMethod]
		public void Should_take_data_from_test_server_via_proxy4a_on_river_socks_server()
		{
			using (var server = new SocksServer())
			{
				server.Listen(4545);
				using (var socks = new Socks4Client())
				{
					socks.Connect("localhost", 4545, "httpbin.org", 80, true);
					TestConnction(socks);
				}
			}
		}

		[TestMethod]
		public void Should_compare_direct_and_proxy_response()
		{
			string direct;
			using (var cli = new TcpClient())
			{
				cli.Connect("httpbin.org", 80);
				direct= TestConnction(cli.GetStream());
				direct = Regex.Replace(direct, "(?im)^Date: .*$", "");
				File.WriteAllText(Path.GetTempFileName(), direct);
			}
			using (var server = new SocksServer())
			{
				server.Listen(4545);
				using (var socks = new Socks4Client())
				{
					socks.Connect("localhost", 4545, "httpbin.org", 80, true);
					var proxifyed = TestConnction(socks);
					proxifyed = Regex.Replace(proxifyed, "(?im)^Date: .*$", "");
					File.WriteAllText(Path.GetTempFileName(), proxifyed);
					Assert.AreEqual(direct, proxifyed);
				}
			}
		}

	}
}
