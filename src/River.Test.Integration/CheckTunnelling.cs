using Microsoft.VisualStudio.TestTools.UnitTesting;
using River.Any;
using River.Socks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace River.Test.Integration
{
	[TestClass]
	[TestCategory("Integration")]
	public class CheckTunnelling : TestClass
	{
		[TestMethod]
		public void Check_big_complex_tunel()
		{
			// gost_ss -> river_socks -> gost_socks -> river_ss

			var port = GetFreePort();

			var proxy = new AnyProxyServer
			{
				Chain =
				{
					"ss://chacha20:123@rt.xkip.ru:18338",
					"ss://chacha20:123@rt.xkip.ru:18338",
					"ss://chacha20:123@rt.xkip.ru:18338",
					"ss://chacha20:123@rt.xkip.ru:18338",
				},
			}.Track(this);
			proxy.Run("socks://0.0.0.0:" + port);


			var connection = new Socks4ClientStream("127.0.0.1", port, Host, 80).Track(this);
			TestConnction(connection, Host);
		}

	}
}
