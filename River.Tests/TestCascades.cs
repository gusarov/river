using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace River.Tests
{
	[TestClass]
	public class TestCascades
	{
		[TestMethod]
		public void Should_build_a_chain_from_3_protos()
		{
			var first = new SocksServer(2001);
			var second = new SocksServer(2002);
			Assert.Inconclusive();
		}
	}
}
