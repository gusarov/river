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
	public class SocksCodesTest
	{
		[TestMethod]
		public void Should_return_proper_strings()
		{
			Assert.AreEqual("OK", SocksError.OK.GetDescription());
			Assert.AreEqual("General SOCKS server failure", SocksError.GeneralSOCKSServerFailure.GetDescription());
			Assert.AreEqual("Connection not allowed by ruleset", SocksError.ConnectionNotAllowedByRuleset.GetDescription());
			Assert.AreEqual("Network unreachable", SocksError.NetworkUnreachable.GetDescription());
			Assert.AreEqual("Host unreachable", SocksError.HostUnreachable.GetDescription());
			Assert.AreEqual("Connection refused", SocksError.ConnectionRefused.GetDescription());
			Assert.AreEqual("TTL expired", SocksError.TTLExpired.GetDescription());
			Assert.AreEqual("Command not supported", SocksError.CommandNotSupported.GetDescription());
			Assert.AreEqual("Address type not supported", SocksError.AddressTypeNotSupported.GetDescription());

		}
	}
}
