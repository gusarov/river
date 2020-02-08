using System;
using System.Net.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace River.Tests
{
	[TestClass]
	public class TestHttpTransparentGate
	{
		private static RiverServer _river;

		static Random _rnd = new Random();

		int _port = _rnd.Next(short.MaxValue);

		[TestInitialize]
		public void Initialize()
		{
		}

		[TestCleanup]
		public void Cleanup()
		{
			_river?.Dispose();
		}

		[TestMethod]
		public void Should_get_direct()
		{
			using (var cli = new HttpClient())
			{
				var data = cli.GetStringAsync("http://httpbin.org/get").Result;
				Assert.IsTrue(data.Contains("\"Host\": \"httpbin.org\""));
			}
		}

		[TestMethod]
		public void Should_bypass_simple_requests()
		{
			_river = new RiverServer(_port, "127.0.0.1:80");
			// _river = new RiverServer(_port, "httpbin.org");
			using (var cli = new HttpClient())
			{
				var data = cli.GetStringAsync($"http://127.0.0.1:{_port}/").Result;
				Console.WriteLine(data);
				Assert.IsTrue(data.Contains("\"Host\": \"httpbin.org\""));
			}
		}
	}
}
