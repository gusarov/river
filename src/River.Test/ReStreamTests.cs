using Microsoft.VisualStudio.TestTools.UnitTesting;
using River.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace River.Test
{
	[TestClass]
	public class ReStreamTests
	{
		[TestMethod]
		public void Should_10_read_from_underlying()
		{
			var ms = new MemoryStream(new byte[] { 1, 2, 3 });
			var sut = new ReStream(ms);

			var buf = new byte[1024];
			var a = sut.Read(buf, 1, buf.Length - 1);

			Assert.AreEqual(3, a);
			Assert.AreEqual(0, buf[0]);
			Assert.AreEqual(1, buf[1]);
			Assert.AreEqual(2, buf[2]);
			Assert.AreEqual(3, buf[3]);
		}

		[TestMethod]
		public void Should_10_read_twice()
		{
			var ms = new MemoryStream(new byte[] { 1, 2, 3 });
			var sut = new ReStream(ms);

			var buf = new byte[1024];
			var a = sut.Read(buf, 1, 2);

			Assert.AreEqual(2, a);
			Assert.AreEqual(0, buf[0]);
			Assert.AreEqual(1, buf[1]);
			Assert.AreEqual(2, buf[2]);
			Assert.AreEqual(0, buf[4]);

			a = sut.Read(buf, 3, 10);

			Assert.AreEqual(1, a);
			Assert.AreEqual(0, buf[0]);
			Assert.AreEqual(1, buf[1]);
			Assert.AreEqual(2, buf[2]);
			Assert.AreEqual(3, buf[3]);
			Assert.AreEqual(0, buf[4]);
		}

		[TestMethod]
		public void Should_10_reread()
		{
			var ms = new MemoryStream(new byte[] { 1, 2, 3 });
			var sut = new ReStream(ms);

			var buf = new byte[1024];
			var a = sut.Read(buf, 1, 2);

			Assert.AreEqual(2, a);
			Assert.AreEqual(0, buf[0]);
			Assert.AreEqual(1, buf[1]);
			Assert.AreEqual(2, buf[2]);
			Assert.AreEqual(0, buf[4]);

			a = sut.Read(buf, 3, 10);

			Assert.AreEqual(1, a);
			Assert.AreEqual(0, buf[0]);
			Assert.AreEqual(1, buf[1]);
			Assert.AreEqual(2, buf[2]);
			Assert.AreEqual(3, buf[3]);
			Assert.AreEqual(0, buf[4]);

			sut.ResetReader();

			a = sut.Read(buf, 10, 10);

			Assert.AreEqual(3, a);
			Assert.AreEqual(1, buf[10]);
			Assert.AreEqual(2, buf[11]);
			Assert.AreEqual(3, buf[12]);

		}


		[TestMethod]
		[ExpectedException(typeof(IOException))]
		public void Should_20_not_allow_reread_after_write()
		{
			var msR = new MemoryStream(new byte[] { 1, 2, 3, 4 });
			var msW = new MemoryStream();
			var cs = new CustomStream(null, (_, b, p, c) => msW.Write(b, p, c), (_, b, p, c) => msR.Read(b, p, c));

			var sut = new ReStream(cs);


			var buf = new byte[1024];
			var a = sut.Read(buf, 1, 3);

			sut.Write(10, 20, 30, 40);

			sut.ResetReader();


		}

		[TestMethod]
		public void Should_20_read_buf_during_connecting_after_write()
		{
			var msR = new MemoryStream(new byte[] { 1, 2, 3 });
			var msW = new MemoryStream();
			var cs = new CustomStream(null, (_, b, p, c) => msW.Write(b, p, c), (_, b, p, c) => msR.Read(b, p, c));

			var sut = new ReStream(cs);

			var buf = new byte[1024];
			var a = sut.Read(buf, 1, 2); // 1 byte left in a buffer!!

			Assert.AreEqual(2, a);
			Assert.AreEqual(0, buf[0]);
			Assert.AreEqual(1, buf[1]);
			Assert.AreEqual(2, buf[2]);
			Assert.AreEqual(0, buf[3]);
			Assert.AreEqual(0, buf[4]);


			sut.Write(10, 20, 30, 40);

			msR = new MemoryStream(new byte[] { 4 });

			a = sut.Read(buf, 3, 2); // read the last byte from buffer, not from the end of stream

			Assert.AreEqual(1, a); // just use current remained buffer first, then continue to read from underlying
			Assert.AreEqual(0, buf[0]);
			Assert.AreEqual(1, buf[1]);
			Assert.AreEqual(2, buf[2]);
			Assert.AreEqual(3, buf[3]);
			Assert.AreEqual(0, buf[4]);
			Assert.AreEqual(0, buf[5]);

			a = sut.Read(buf, 4, 2); // read the last byte from buffer, not from the end of stream

			Assert.AreEqual(1, a); // remained underlying
			Assert.AreEqual(0, buf[0]);
			Assert.AreEqual(1, buf[1]);
			Assert.AreEqual(2, buf[2]);
			Assert.AreEqual(3, buf[3]);
			Assert.AreEqual(4, buf[4]);
			Assert.AreEqual(0, buf[5]);

		}
	}

}
