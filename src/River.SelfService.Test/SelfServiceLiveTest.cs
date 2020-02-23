using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using River;
using River.Http;
using River.Socks;
using River.Test;

namespace River.SelfService.Test
{
	/// <summary>
	/// Integration suite supposed to be run against real server after deployment
	/// </summary>
	[TestClass]
	public class SelfServiceLiveTest : TestClass
	{
		const string _host = "_river";
		static string _proxy = "127.0.0.1"; // "r.xkip.ru";
		static int _proxyPort;
		static SocksServer _server;

		[ClassInitialize]
		public static void ClassInit(TestContext ctx)
		{
			Resolver.RegisterOverride("_river", x => new RiverSelfService());

			_proxyPort = GetFreePort();
			_server = new SocksServer();
			_server.Run(new ServerConfig
			{
				EndPoints =
				{
					new IPEndPoint(IPAddress.Loopback, _proxyPort),
					new IPEndPoint(IPAddress.IPv6Loopback, _proxyPort),
				},
			});

			// ObjectTracker.Default.ResetCollection();
		}

		[ClassCleanup]
		public static void ClassClean()
		{
			_server.Dispose();
		}

		[TestMethod]
		public void Check_10_basic()
		{
			var cli = new Socks4ClientStream(_proxy, _proxyPort, _host, 80, true).Track(this);
			TestConnction(cli, _host);
		}

		[TestMethod]
		public void Check_20_basic_by_ip()
		{
			var cli = new Socks4ClientStream(_proxy, _proxyPort, "127.127.127.127", 80, false).Track(this);
			TestConnction(cli, _host);
		}


		[TestMethod]
		public void Check_30_self_service_http_connect()
		{
			var cli = new HttpProxyClientStream(_proxy, _proxyPort, _host, 80).Track(this);
			TestConnction(cli, _host, url: "/");
		}

		[TestMethod]
		public void Check_30_self_service_http_connect_ip()
		{
			var cli = new HttpProxyClientStream(_proxy, _proxyPort, "127.127.127.127", 80).Track(this);
			TestConnction(cli, _host, url: "/");
		}

		[TestMethod]
		public void Check_30_self_service_http_get()
		{
			var cli = new HttpProxyClientStream(_proxy, _proxyPort).Track(this);
			TestConnction(cli, _host, url: "http://_river/");
		}

		[TestMethod]
		public void Check_30_self_service_http_get_ip()
		{
			var cli = new HttpProxyClientStream(_proxy, _proxyPort).Track(this);
			TestConnction(cli, _host, url: "http://127.127.127.127/");
		}

		[TestMethod]
		public void Check_30_self_service_http_direct()
		{
			var cli = new NullClientStream().Track(this);
			cli.Plug(_proxy, _proxyPort);
			TestConnction(cli, _host, url: "/");
		}

		[TestMethod] //
		public void Check_40_self_service_localhost_http_direct_ip()
		{
			var cli = new NullClientStream().Track(this);
			cli.Plug(_proxy, _proxyPort);
			TestConnction(cli, "127.0.0.1", "_river", _proxyPort, "/");
		}

		[TestMethod]
		public void Check_40_self_service_localhost_http_direct_ip6()
		{
			var cli = new NullClientStream().Track(this);
			cli.Plug(_proxy, _proxyPort);
			TestConnction(cli, "::1", "_river", _proxyPort, "/");
		}

		[TestMethod]
		public void Check_40_self_service_localhost_http_direct_ip6b()
		{
			var cli = new NullClientStream().Track(this);
			cli.Plug(_proxy, _proxyPort);
			TestConnction(cli, "[::1]", "_river", _proxyPort, "/");
		}

		[TestMethod]
		public void Check_40_self_service_localhost_http_direct_name()
		{
			var cli = new NullClientStream().Track(this);
			cli.Plug(_proxy, _proxyPort);
			TestConnction(cli, "localhost", "_river", _proxyPort, "/");
		}

		[TestMethod]
		public void Check_socks_by_river4_client()
		{
			Stream cli;
			IPAddress ip = default;
			ip = new IPAddress(new byte[] { 127, 127, 127, 127 });

			
			using (Scope("Socks4ClientStream - default null"))
			{
				cli = new Socks4ClientStream(_proxy, _proxyPort, _host, 80).Track(this);
				TestConnction(cli, _host);
			}

			using (Scope("Socks4ClientStream - v4a force dns"))
			{
				cli = new Socks4ClientStream(_proxy, _proxyPort, _host, 80, proxyDns: true).Track(this);
				TestConnction(cli, _host);
			}

			using (Scope("Socks4ClientStream - v4 force i"))
			{
				cli = new Socks4ClientStream(_proxy, _proxyPort, _host, 80, proxyDns: false).Track(this);
				TestConnction(cli, _host);
			}

			// IPv4 - native support
			ip = new IPAddress(new byte[] { 127, 127, 127, 127 });

			using (Scope("Socks4ClientStream 4"))
			{
				cli = new Socks4ClientStream(_proxy, _proxyPort, ip.ToString(), 80).Track(this);
				TestConnction(cli, _host);
			}

			using (Scope("Socks4ClientStream 5"))
			{
				cli = new Socks4ClientStream(_proxy, _proxyPort, ip.ToString(), 80, proxyDns: true).Track(this);
				TestConnction(cli, _host);
			}

			using (Scope("Socks4ClientStream 6"))
			{
				cli = new Socks4ClientStream(_proxy, _proxyPort, ip.ToString(), 80, proxyDns: false).Track(this);
				TestConnction(cli, _host);
			}
			

			using (Scope("Socks5ClientStream"))
			{
				// default null
				cli = new Socks5ClientStream(_proxy, _proxyPort, _host, 80).Track(this);
				TestConnction(cli, _host);
			}

			using (Scope("Socks5ClientStream proxyDns: true"))
			{
				// v5 force dns
				cli = new Socks5ClientStream(_proxy, _proxyPort, _host, 80, proxyDns: true).Track(this);
				TestConnction(cli, _host);
			}

			using (Scope("Socks5ClientStream proxyDns: false - v5 force ip"))
			{
				cli = new Socks5ClientStream(_proxy, _proxyPort, _host, 80, proxyDns: false).Track(this);
				TestConnction(cli, _host);
			}

			using (Scope("Socks5ClientStream - IPv4 - native support"))
			{
				cli = new Socks5ClientStream(_proxy, _proxyPort, ip.ToString(), 80).Track(this);
				TestConnction(cli, _host);
			}

			using (Scope("Socks5ClientStream proxyDns true"))
			{
				cli = new Socks5ClientStream(_proxy, _proxyPort, ip.ToString(), 80, proxyDns: true).Track(this);
				TestConnction(cli, _host);
			}

			using (Scope("Socks5ClientStream proxyDns false"))
			{
				cli = new Socks5ClientStream(_proxy, _proxyPort, ip.ToString(), 80, proxyDns: false).Track(this);
				TestConnction(cli, _host);
			}
		}



	}
}
