using Microsoft.VisualStudio.TestTools.UnitTesting;
using River.Any;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace River.Test
{
	[TestClass]
	public class AnyHandlerTest : TestClass
	{
		static AnyProxyServer _anyServer;
		static int _port;

		[ClassInitialize]
		public static void ClassInit(TestContext ctx)
		{
			_port = GetFreePort();
			_anyServer = new AnyProxyServer();
			_anyServer.Run($"any://chacha20:123test@127.0.0.1:{_port}");
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

		[TestMethod]
		public void Should_15_handle_http_connect()
		{
			var cli = new Http.HttpProxyClientStream("localhost", _port, Host, 80).Track(this);
			TestConnction(cli, Host);
		}

		[TestMethod]
		public void Should_15_handle_http_get()
		{
			Profiling.Stamp("Creating HttpProxyClientStream...");
			var cli = new Http.HttpProxyClientStream("localhost", _port).Track(this);
			Profiling.Stamp("Creating HttpProxyClientStream TestConnection...");
			TestConnction(cli, Host);
		}

		[TestMethod]
		public void Should_20_handle_http_wrap()
		{
			var cli = new HttpWrap.HttpWrapClientStream("chacha20", "123test", "localhost", _port, Host, 80).Track(this);
			TestConnction(cli, Host);
		}
	}
}
