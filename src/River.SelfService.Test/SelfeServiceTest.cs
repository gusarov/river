using System;
using System.Net;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace River.SelfService.Test
{
	[TestClass]
	public class SelfeServiceTest
	{
		Encoding _utf = new UTF8Encoding(false, false);

		[TestMethod]
		public void Should_respond_with_about_web_page()
		{
			var sut = new RiverSelfService();

			var buf = _utf.GetBytes("GET / HTTP/1.0\r\nHost: _river\r\n\r\n");

			sut.Write(buf, 0, buf.Length);

			var rbuf = new byte[16 * 1024];
			var c = sut.Read(rbuf, 0, rbuf.Length);

			var str = _utf.GetString(rbuf, 0, c);
			Console.WriteLine(str);
		}
	}
}
