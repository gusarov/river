using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
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

        /*

        [TestMethod]
        public void MyTestMethod()
        {
            MessageBox.Show("test");
        }

        [AssemblyInitialize()]
        public static void AssemblyInit(TestContext context)
        {
            // MessageBox.Show("AssemblyInit " + context.TestName);
        }

        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {
            // MessageBox.Show("ClassInit " + context.TestName);
        }

        [TestInitialize()]
        public void Initialize()
        {
            // MessageBox.Show("TestMethodInit");
        }

        [TestCleanup()]
        public void Cleanup()
        {
            // MessageBox.Show("TestMethodCleanup");
        }

        [ClassCleanup()]
        public static void ClassCleanup()
        {
            MessageBox.Show("ClassCleanup");
            Assert.Fail();
        }

        [AssemblyCleanup()]
        public static void AssemblyCleanup()
        {
            MessageBox.Show("AssemblyCleanup");
            Assert.Fail();
        }

        [TestMethod()]
        [ExpectedException(typeof(System.DivideByZeroException))]
        public void DivideMethodTest()
        {
            DivideClass.DivideMethod(0);
        }
        */
    }
}
