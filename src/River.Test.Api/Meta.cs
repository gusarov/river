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
		[Ignore]
		public void Should_not_have_too_small_timeout_on_agent()
		{
			Thread.Sleep(7000);
		}

		static object _leak;

		[TestMethod]
		[Ignore]
		public void Should_fail_because_of_leak()
		{
			_leak = new object();
			ObjectTracker.Default.Register(_leak);
		}
	}
}
