using Microsoft.VisualStudio.TestTools.UnitTesting;
using River.Socks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace River.Test
{
	[TestClass]
	public class SocksHandlerTest : TestClass
	{
		static int _port;

		[ClassInitialize]
		public static void ClassInit(TestContext ctx)
		{
			_port = GetFreePort();
			var socksServer = new Socks.SocksServer().Track(_testStaticScope);
			socksServer.Run($"socks://127.0.0.1:{_port}");
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
			Console.WriteLine("ProcessName: " + Process.GetCurrentProcess().ProcessName);
		}


	}
}
