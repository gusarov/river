using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace River.Test
{
	[TestClass]
	public class EnumDecoratorTest
	{
		public enum TestEnum
		{
			None,
			SomeValue,
			SomeAdditionalValue,

			[System.ComponentModel.Description("Some SOCKS value")]
			SomeCustomValue,

			TTLExpiredTwice,
			GeneralSOCKSServerFailure,
			ThisIsABlueHome,
			ThisIsA,
			ThisIsSOCKS,
			ABlueHome,
		}

		[TestMethod]
		public void Should_provide_default_description()
		{
			Assert.AreEqual("None", TestEnum.None.GetDescription());
			Assert.AreEqual("Some value", TestEnum.SomeValue.GetDescription());
			Assert.AreEqual("Some additional value", TestEnum.SomeAdditionalValue.GetDescription());
		}

		[TestMethod]
		public void Should_provide_customized_description()
		{
			Assert.AreEqual("Some SOCKS value", TestEnum.SomeCustomValue.GetDescription());
		}

		[TestMethod]
		public void Should_handle_undeclared_cases()
		{
			Assert.AreEqual("TTL expired twice", TestEnum.TTLExpiredTwice.GetDescription());
			Assert.AreEqual("General SOCKS server failure", TestEnum.GeneralSOCKSServerFailure.GetDescription());
			Assert.AreEqual("This is a blue home", TestEnum.ThisIsABlueHome.GetDescription());
			// Assert.AreEqual("This is a", TestEnum.ThisIsA.GetDescription());
			Console.WriteLine(TestEnum.ThisIsA.GetDescription());
			Assert.AreEqual("This is SOCKS", TestEnum.ThisIsSOCKS.GetDescription());
			Assert.AreEqual("A home", TestEnum.ABlueHome.GetDescription());
		}

	}
}
