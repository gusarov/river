using System;
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
	[TestCategory("Integration")]
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
			_server.Run("socks://0.0.0.0:" + _proxyPort);

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
			TestConnction(cli, _host, "/");
		}

		[TestMethod]
		public void Check_30_self_service_http_connect_ip()
		{
			var cli = new HttpProxyClientStream(_proxy, _proxyPort, "127.127.127.127", 80).Track(this);
			TestConnction(cli, _host, "/");
		}

		[TestMethod]
		public void Check_30_self_service_http_get()
		{
			var cli = new HttpProxyClientStream(_proxy, _proxyPort).Track(this);
			TestConnction(cli, _host, "http://_river/");
		}

		[TestMethod]
		public void Check_30_self_service_http_get_ip()
		{
			var cli = new HttpProxyClientStream(_proxy, _proxyPort).Track(this);
			TestConnction(cli, _host, "http://127.127.127.127/");
		}

		[TestMethod]
		public void Check_30_self_service_http_direct()
		{
			var cli = new NullClientStream().Track(this);
			cli.Plug(_proxy, _proxyPort);
			TestConnction(cli, _host, "/");
		}

		[TestMethod]
		public void Check_socks_by_river4_client()
		{

			// default null
			Stream cli = new Socks4ClientStream(_proxy, _proxyPort, _host, 80).Track(this);
			TestConnction(cli, _host);

			// v4a force dns
			cli = new Socks4ClientStream(_proxy, _proxyPort, _host, 80, proxyDns: true).Track(this);
			TestConnction(cli, _host);

			// v4 force ip
			cli = new Socks4ClientStream(_proxy, _proxyPort, _host, 80, proxyDns: false).Track(this);
			TestConnction(cli, _host);

			// IPv4 - native support
			var ip = new IPAddress(new byte[] { 127, 127, 127, 127 });

			cli = new Socks4ClientStream(_proxy, _proxyPort, ip.ToString(), 80).Track(this);
			TestConnction(cli, _host);

			cli = new Socks4ClientStream(_proxy, _proxyPort, ip.ToString(), 80, proxyDns: true).Track(this);
			TestConnction(cli, _host);

			cli = new Socks4ClientStream(_proxy, _proxyPort, ip.ToString(), 80, proxyDns: false).Track(this);
			TestConnction(cli, _host);

			// default null
			cli = new Socks5ClientStream(_proxy, _proxyPort, _host, 80).Track(this);
			TestConnction(cli, _host);

			// v5 force dns
			cli = new Socks5ClientStream(_proxy, _proxyPort, _host, 80, proxyDns: true).Track(this);
			TestConnction(cli, _host);

			// v5 force ip
			cli = new Socks5ClientStream(_proxy, _proxyPort, _host, 80, proxyDns: false).Track(this);
			TestConnction(cli, _host);

			// IPv4 - native support
			cli = new Socks5ClientStream(_proxy, _proxyPort, ip.ToString(), 80).Track(this);
			TestConnction(cli, _host);

			cli = new Socks5ClientStream(_proxy, _proxyPort, ip.ToString(), 80, proxyDns: true).Track(this);
			TestConnction(cli, _host);

			cli = new Socks5ClientStream(_proxy, _proxyPort, ip.ToString(), 80, proxyDns: false).Track(this);
			TestConnction(cli, _host);
		}



	}
}
