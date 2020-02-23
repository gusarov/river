using Microsoft.VisualStudio.TestTools.UnitTesting;
using River.Socks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace River.Test
{
	[TestClass]
	public class SocksHandlerTest : TestClass
	{
		static SocksServer _socksServer;
		static int _port;

		[ClassInitialize]
		public static void ClassInit(TestContext ctx)
		{
			_port = GetFreePort();
			_socksServer = new Socks.SocksServer();
			_socksServer.Run($"socks://127.0.0.1:{_port}");
		}

		[TestMethod]
		public void Should_10_handle_socks4()
		{
			var cli = new Socks.Socks4ClientStream("localhost", _port, Host, 80).Track(this);
			TestConnction(cli, Host);
		}

		[TestMethod]
		public void Should_10_handle_socks5()
		{
			var cli = new Socks.Socks5ClientStream("localhost", _port, Host, 80).Track(this);
			TestConnction(cli, Host);
		}


	}
}
