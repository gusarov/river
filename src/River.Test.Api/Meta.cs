using System.IO;
using System.Linq;
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
	}
}
