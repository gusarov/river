using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using River.HttpWrap;
using River.Socks;
using River.Test;

namespace River.Test
{

	[TestClass]
	public class ChainTests : TestClass
	{
		public ChainTests()
		{
			
		}

		[TestMethod]
		// [Timeout(5000)]
		public void Should_chain_3_socks()
		{
			_ = typeof(Socks4ClientStream); // to load the type

			var port1 = GetFreePort();
			var proxy1 = new SocksServer(new ServerConfig
			{
				EndPoints =
				{
					new IPEndPoint(IPAddress.Loopback, port1),
					new IPEndPoint(IPAddress.IPv6Loopback, port1),
				},
			});

			var port2 = GetFreePort();
			var proxy2 = new SocksServer(new ServerConfig
			{
				EndPoints =
				{
					new IPEndPoint(IPAddress.Loopback, port2),
					new IPEndPoint(IPAddress.IPv6Loopback, port2),
				},
			});

			var port3 = GetFreePort();
			var proxy3 = new SocksServer(new ServerConfig
			{
				EndPoints =
				{
					new IPEndPoint(IPAddress.Loopback, port3),
					new IPEndPoint(IPAddress.IPv6Loopback, port3),
				},
			});

			var port0 = GetFreePort();
			var proxy = new SocksServer(new ServerConfig
			{
				EndPoints =
				{
					new IPEndPoint(IPAddress.Loopback, port0),
					new IPEndPoint(IPAddress.IPv6Loopback, port0),
				},
			})
			{
				Chain = {
					"socks4://127.0.0.1:" + port1,
					// "socks4://127.0.0.1:" + port2,
					// "socks4://127.0.0.1:" + port3,
				},
			};

			var cli = new Socks4ClientStream("localhost", port0, "www.google.com", 80);
			TestConnction(cli, "www.google.com");

			cli.Dispose();
			proxy1.Dispose();
			proxy2.Dispose();
			proxy3.Dispose();
			proxy.Dispose();
		}

	}
}
