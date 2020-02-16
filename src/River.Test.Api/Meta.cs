using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using River.Socks;

namespace River.Test.Api
{
	[TestClass]
	public class Meta : TestClass
	{
		[TestMethod]
		public void Should_initialize_test_class()
		{
			Assert.IsTrue(TestInitialized);
		}

		[TestMethod]
		public void Should_not_have_too_small_timeout_on_agent()
		{
			Thread.Sleep(7000);
		}
	}
}
