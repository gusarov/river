using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace River.Tests
{
	[TestClass]
	public class BinaryAcceptanceTests
	{
		private static RiverServer _riverMouth;
		private static SocksServerToRiverClient _riverSource;

		[ClassInitialize]
		public static void ClassInitialize(TestContext tc)
		{
			_riverMouth = new RiverServer(300);
			_riverSource = new SocksServerToRiverClient(301, "127.0.0.1:300");
		}

		[ClassCleanup]
		public static void ClassCleanup()
		{
			_riverSource?.Dispose();
			_riverMouth?.Dispose();
		}

		private Socks4Client _socksClient;

		[TestInitialize]
		public void TestInit()
		{
			_socksClient = new Socks4Client("127.0.0.1", 301, "127.0.0.1", 79);
		}

		[TestCleanup]
		public void TestCleanup()
		{
			_socksClient?.Dispose();
		}

		[TestMethod]
		public void Should_receive_512K_of_data()
		{
			_socksClient.WriteByte(1);
			// wait for approve test mode 1
			var state = _socksClient.ReadByte();
			Assert.AreEqual(1, state, "Test mode 1 should be ready");

			var data = new byte[2048];
			for (int i = 0; i < data.Length; i++)
			{
				data[i] = (byte)(i % 256);
			}

			int totalReported = 0;

			ThreadPool.QueueUserWorkItem(delegate
			{
				// reader
				try
				{
					while (true)
					{
						var buf = new byte[4];
						_socksClient.Read(buf, 0, 4);
						var count = BitConverter.ToInt32(buf, 0);
						totalReported += count;
					}
				}
				catch
				{
					
				}
			});

			for (int i = 0; i < 256; i++)
			{
				_socksClient.Write(data);
			}
			while (1024 * 512 != totalReported)
			{
				Console.WriteLine($"Waiting {1024 * 512} response: " + totalReported);
				Thread.Sleep(1000);
			}
			Assert.AreEqual(1024*512, totalReported, "State is good");
		}

		[TestMethod]
		public void Should_receive_1K_of_data_as_single_buffer()
		{
			_socksClient.WriteByte(1);
			// wait for approve test mode 1
			var state = _socksClient.ReadByte();
			Assert.AreEqual(1, state, "Test mode 1 should be ready");

			var data = new byte[1024];
			for (int i = 0; i < data.Length; i++)
			{
				data[i] = (byte)(i % 256);
			}

			_socksClient.Write(data);
			var buf = new byte[4];
			_socksClient.Read(buf, 0, 4);
			var count = BitConverter.ToInt32(buf, 0);
			Assert.AreEqual(1024, count, "Count is wrong");
		}
	}
}
