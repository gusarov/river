﻿using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using River.Socks;

namespace River.Test.Api
{
	[TestClass]
	public class SocksTests : NetworkTest
	{
		[TestMethod]
		public void Should_socks4_have_a_ctor_with_proxy_and_host()
		{
			var server = new DemoTcpServer();
			var proxyPort = GetFreePort();
			var proxy = new SocksServer("socks://0.0.0.0:" + proxyPort);
			var proxyClient = new Socks4ClientStream("127.0.0.1", proxyPort, "127.0.0.1", server.Port);

			var data = new byte[] { 1, 2, 3, 4 };
			proxyClient.Write(data);
			var buf = new byte[16 * 1024];
			var d = proxyClient.Read(buf, 0, buf.Length);

			Assert.AreEqual(4, d, "Should read 4 bytes in a single packet");
			// demo server is XOR 37
			CollectionAssert.AreEqual(data.Select(x => (byte)(x ^ 37)).ToArray(), buf.Take(d).ToArray());
		}

		[TestMethod]
		public void Should_socks5_have_a_ctor_with_proxy_and_host()
		{
			var server = new DemoTcpServer();
			var proxyPort = GetFreePort();
			var proxy = new SocksServer("socks://0.0.0.0:" + proxyPort);
			var proxyClient = new Socks5ClientStream("127.0.0.1", proxyPort, "127.0.0.1", server.Port);

			var data = new byte[] { 1, 2, 3, 4 };
			proxyClient.Write(data);
			var buf = new byte[16 * 1024];
			var d = proxyClient.Read(buf, 0, buf.Length);

			Assert.AreEqual(4, d, "Should read 4 bytes in a single packet");
			// demo server is XOR 37
			CollectionAssert.AreEqual(data.Select(x => (byte)(x ^ 37)).ToArray(), buf.Take(d).ToArray());
		}
	}

	public class NetworkTest
	{
		protected int GetFreePort()
		{
			var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
			var ipe = (IPEndPoint)socket.LocalEndPoint;
			var port = ipe.Port;
			socket.Close();
			return port;
		}
	}
}
