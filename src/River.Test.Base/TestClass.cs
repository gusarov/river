using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using River;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace River.Test
{
	// [TestClass]
	public class TestClass
	{
		static TestClass()
		{
			RiverInit.RegAll();
		}

		object _test;
		protected bool TestInitialized { get; private set; }

		[TestCleanup]
		public void Clean()
		{
			Explode();
		}

		[TestInitialize]
		public void Init()
		{
			TestInitialized = true;
			_test = new object();
			Explode();
		}

		protected void Explode()
		{
			Tracker.Explode(_test);
			Tracker.Explode(this);
		}

		protected static int GetFreePort()
		{
			var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
			var ipe = (IPEndPoint)socket.LocalEndPoint;
			var port = ipe.Port;
			socket.Close();
			return port;
		}

		protected static void WaitFor(Func<bool> condition)
		{
			var sw = Stopwatch.StartNew();
			while (!condition())
			{
				if (sw.Elapsed.TotalSeconds > 3000)
				{
					throw new TimeoutException("WaitFor timed out");
				}
				Thread.Sleep(100);
			}
		}

		public string Host
		{
			get
			{
				return "www.google.com";
			}
		}

		/// <summary>
		/// Test current connection to web server
		/// E.g. you can connect to httpbin.org to do this testing
		/// </summary>
		protected static string TestConnction(Stream client, string host = "www.google.com")
		{
			var expected =
				// "onclick=gbar.logger"; // google.com
				"Location: http://www.google.com/";

			var readBuf = new byte[1024 * 1024];
			var readBufPos = 0;
			var are = new AutoResetEvent(false);
			var connected = true;
			client.BeginRead(readBuf, 0, readBuf.Length, Read, null);
			// bool found = false;
			void Read(IAsyncResult ar)
			{
				var c = client.EndRead(ar);
				if (c == 0)
				{
					connected = false;
					return;
				}
				var line = Encoding.UTF8.GetString(readBuf, readBufPos, c);
				if (line.Contains(expected))
				{
					// found = true;
					are.Set();
				}
				readBufPos += c;
				// var line = Encoding.UTF8.GetString(readBuf, 0, c);
				// Console.WriteLine(">>> " + line);
				client.BeginRead(readBuf, readBufPos, readBuf.Length - readBufPos, Read, null);
			}

			var url = host.Contains("google") ? "ncr" : "";

			var request = Encoding.ASCII.GetBytes($"GET /{url} HTTP/1.1\r\nHost: {host}\r\nConnection: keep-alive\r\n\r\n");
			client.Write(request, 0, request.Length);

			// WaitFor(() => Encoding.UTF8.GetString(ms.ToArray()).Contains(expected) || !connected);

			Assert.IsTrue(are.WaitOneTest(5000));
			Assert.IsTrue(connected);

			client.Write(request, 0, request.Length);

			Assert.IsTrue(are.WaitOneTest(5000));
			Assert.IsTrue(connected);

			return ""; // Encoding.UTF8.GetString(ms.ToArray());
		}
	}

	public static class WaitHandleExt
	{
		public static bool WaitOneTest(this WaitHandle handle, int ms)
		{
			bool r;
			while (!(r = handle.WaitOne(ms)) && Debugger.IsAttached) { };
			return r;
		}
	}
}
