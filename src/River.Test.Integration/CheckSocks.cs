using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
		[TestMethod]
		public void Check_socks_by_river4_client()
		{
			// default null
			var cli = new Socks4ClientStream("r.xkip.ru", 11080, "httpbin.org", 80);
			TestConnction(cli);

			// v4a force dns
			cli = new Socks4ClientStream("r.xkip.ru", 11080, "httpbin.org", 80, proxyDns: true);
			TestConnction(cli);

			// v4 force ip
			cli = new Socks4ClientStream("r.xkip.ru", 11080, "httpbin.org", 80, proxyDns: false);
			TestConnction(cli);
		}


		[TestMethod]
		public void Check_socks_by_river5_client()
		{
			// default null
			var cli = new Socks5ClientStream("r.xkip.ru", 11080, "httpbin.org", 80);
			TestConnction(cli);

			// v5 force dns
			cli = new Socks5ClientStream("r.xkip.ru", 11080, "httpbin.org", 80, proxyDns: true);
			TestConnction(cli);

			// v5 force ip
			cli = new Socks5ClientStream("r.xkip.ru", 11080, "httpbin.org", 80, proxyDns: false);
			TestConnction(cli);
		}

	}
}
