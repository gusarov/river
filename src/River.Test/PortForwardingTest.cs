using Microsoft.VisualStudio.TestTools.UnitTesting;
using River.HttpWrap;
using River.ShadowSocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace River.Test
{
	[TestClass]
	public class PortForwardingTest : TestClass
	{

		[TestMethod]
		public void Should_forward_port()
		{
			var port = GetFreePort();
			new PortForwardingServer($"tcp://0.0.0.0:{port}/www.google.com:80").Track();

			var tcpCli = new TcpClient("127.0.0.1", port).Track();
			var cli = tcpCli.GetStream();
			TestConnction(cli, Host);
		}

		[TestMethod]
		public void Should_forward_port_via_chain()
		{
			Assert.Inconclusive();
			// HOW TO PROVE?
		}

	}
}
