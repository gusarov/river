using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace River.Test
{
	[TestClass]
	public class ExceptionalTest : TestClass
	{
		[TestMethod]
		[ExpectedException(typeof(SocketException))]
		public void Should_not_allow_to_start_with_the_same_port()
		{
			var port = GetFreePort();

			var any1 = new Any.AnyProxyServer("http://0.0.0.0:" + port).Track(this);

			var any2 = new Any.AnyProxyServer("http://0.0.0.0:" + port).Track(this);
		}
	}
}
