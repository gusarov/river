using Microsoft.VisualStudio.TestTools.UnitTesting;
using River.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace River.Test.Auth
{
	[TestClass]
	public class AuthHashTests
	{
		[TestMethod]
		public void Should_generate_hash_for_password()
		{
			var sut = new AuthHashProcessor();
			var hash = sut.Generate("P@$$word");
			Console.WriteLine(hash);

			Assert.IsTrue(sut.Validate(hash, "P@$$word"));
			Assert.IsFalse(sut.Validate(hash, "wrong"));
		}
	}
}
