using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace River.Tests
{
	[TestClass]
	public class TestTunnel
	{
		const string _mouth = "dimadiv.westeurope.cloudapp.azure.com";
		const int _port = 80;

		private static SocksServerToRiverClient _tunnel;

		[ClassInitialize]
		public static void ClassInitialize(TestContext tc)
		{
			_tunnel = new SocksServerToRiverClient(4235, _mouth + ":80", new IPEndPoint(IPAddress.Parse("10.161.88.23"), 0));
		}

		[ClassCleanup]
		public static void ClassCleanup()
		{
			_tunnel?.Dispose();
		}

		[TestMethod]
		public void Should_simple_get()
		{
			using (var cli = new HttpClient())
			{

				var data = cli.GetStringAsync("http://httpbin.org/get").Result;

				Assert.Inconclusive();
			}
		}
	}
}
