using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using River.ShadowSocks;
using River.Socks;

namespace River.Test.Integration
{
	/// <summary>
	/// Integration suite supposed to be run against real server after deployment
	/// </summary>
	[TestClass]
	[TestCategory("Integration")]
	public class CheckSocks : TestClass
	{
		const string host = "www.google.com";

		[TestMethod]
		public void Check_socks_by_river4_client()
		{

			// default null
			var cli = new Socks4ClientStream("r.xkip.ru", 11080, host, 80);
			TestConnction(cli, host);

			// v4a force dns
			cli = new Socks4ClientStream("r.xkip.ru", 11080, host, 80, proxyDns: true);
			TestConnction(cli, host);

			// v4 force ip
			cli = new Socks4ClientStream("r.xkip.ru", 11080, host, 80, proxyDns: false);
			TestConnction(cli, host);

			// IPv4 - native support
			var ip = Dns.GetHostAddresses(host)
				.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);

			cli = new Socks4ClientStream("r.xkip.ru", 11080, ip.ToString(), 80);
			TestConnction(cli, host);

			cli = new Socks4ClientStream("r.xkip.ru", 11080, ip.ToString(), 80, proxyDns: true);
			TestConnction(cli, host);

			cli = new Socks4ClientStream("r.xkip.ru", 11080, ip.ToString(), 80, proxyDns: false);
			TestConnction(cli, host);

			// IPv6 - support via v4a host name
			/*
			// my servers's ISP also is not supporting ipv6
			var ip6 = Dns.GetHostAddresses(host)
				.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetworkV6);

			cli = new Socks4ClientStream("r.xkip.ru", 11080, ip6.ToString(), 80);
			TestConnction(cli, host);

			cli = new Socks4ClientStream("r.xkip.ru", 11080, ip6.ToString(), 80, proxyDns: true);
			TestConnction(cli, host);

			cli = new Socks4ClientStream("r.xkip.ru", 11080, ip6.ToString(), 80, proxyDns: false);
			TestConnction(cli, host);
			*/
		}


		[TestMethod]
		public void Check_socks_by_river5_client()
		{

			// default null
			var cli = new Socks5ClientStream("r.xkip.ru", 11080, host, 80);
			TestConnction(cli, host);

			// v5 force dns
			cli = new Socks5ClientStream("r.xkip.ru", 11080, host, 80, proxyDns: true);
			TestConnction(cli, host);

			// v5 force ip
			cli = new Socks5ClientStream("r.xkip.ru", 11080, host, 80, proxyDns: false);
			TestConnction(cli, host);

			// IPv4 - native support
			var ip = Dns.GetHostAddresses(host)
				.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);

			cli = new Socks5ClientStream("r.xkip.ru", 11080, ip.ToString(), 80);
			TestConnction(cli, host);

			cli = new Socks5ClientStream("r.xkip.ru", 11080, ip.ToString(), 80, proxyDns: true);
			TestConnction(cli, host);

			cli = new Socks5ClientStream("r.xkip.ru", 11080, ip.ToString(), 80, proxyDns: false);
			TestConnction(cli, host);
		}

		[TestMethod]
		public void Check_ss_by_river_client()
		{

			// default null
			var cli = new ShadowSocksClientStream("chacha20", "123", "r.xkip.ru", 18338, host, 80);
			TestConnction(cli, host);

			// force dns
			cli = new ShadowSocksClientStream("chacha20", "123", "r.xkip.ru", 18338, host, 80, proxyDns: true);
			TestConnction(cli, host);

			// force ip
			cli = new ShadowSocksClientStream("chacha20", "123", "r.xkip.ru", 18338, host, 80, proxyDns: false);
			TestConnction(cli, host);
		}

		[TestMethod]
		public void Check_ss()
		{
			var server = new ShadowSocksServer();
			server.Run("ss://chacha20:123@0.0.0.0:18338");

			var cli = new ShadowSocksClientStream("chacha20", "123", "127.0.0.1", 18338, host, 80);
			TestConnction(cli, host);

		}

	}
}
