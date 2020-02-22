using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using River;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Linq;
using System.Collections.Generic;

namespace River.Test
{
	// [TestClass]
	public class TestClass
	{
		static TestClass()
		{
			RiverInit.RegAll();
			ObjectTracker.Default.EnableCollection();
		}

		object _test;
		protected bool TestInitialized { get; private set; }

		[TestCleanup]
		public void BaseClean()
		{
			Explode();

			// snapshot a list of objects created so far
			// some of them might be from concurrent tests
			// var list = new List<WeakReference>(ObjectTracker.Default.Items.Select(x => new WeakReference(x)));
			var list = ObjectTracker.Default.Weaks;

			try
			{
				Console.WriteLine("Cleaning...");
				WaitFor(() =>
				{
					GC.Collect();
					GC.WaitForPendingFinalizers();
					return list.All(x => x.Target == null);
					// return ObjectTracker.Default.Count == 0;
				});
				Console.WriteLine("All objects are clear");
			}
			catch
			{
				var objs = list.Select(x => x.Target).Where(x => x != null).ToArray();
				// var objs = ObjectTracker.Default.Items.Where(x => x != null).ToArray();
				Console.WriteLine($"Objects alive: {objs.Length} ======================");
				foreach (var item in objs)
				{
					Console.WriteLine(item);
				}
				throw;
			}
		}

		Stopwatch _testTime;

		[TestInitialize]
		public void BaseInit()
		{
			TestInitialized = true;
			_test = new object();
			Explode();
			ObjectTracker.Default.ResetCollection();
			_testTime = Stopwatch.StartNew();
		}

		protected void Explode()
		{
			Tracker.Explode(_test);
			Tracker.Explode(this);

			/*
			// DO NOT DO THIS - YOU HAVE TO MAKE SURE THAT DISPOSE IS PROPAGATED FROM TRACKER OBJECTS ONLY
			foreach (var item in ObjectTracker.Default.Items.ToArray())
			{
				if (item is IDisposable disp)
				{
					try
					{
						disp?.Dispose();
					}
					catch { }
				}
			}
			*/
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
				if (sw.Elapsed.TotalSeconds > 3)
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

		protected static string TestConnction(Stream client, string host = "www.google.com", int port = 80, string url = "/")
		{
			return TestConnction(client, host, host, port, url);
		}

		/// <summary>
		/// Test current connection to web server
		/// E.g. you can connect to httpbin.org to do this testing
		/// </summary>
		protected static string TestConnction(Stream client, string host, string expectedHost, int port, string url)
		{
			var expected =
				// "onclick=gbar.logger"; // google.com
				"Location: http://www.google.com/";
			
			switch (expectedHost)
			{
				case "_river":
					expected = "Server: river";
					break;
				case "www.google.com":
					url = "/ncr";
					break;
				default:
					break;
			}

			var readBuf = new byte[1024 * 1024];
			var readBufPos = 0;
			var are = new AutoResetEvent(false);
			var connected = true;
			var sw = Stopwatch.StartNew();
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
					Console.WriteLine("Wait Done: " + sw.ElapsedMilliseconds);
				}
				readBufPos += c;
				// var line = Encoding.UTF8.GetString(readBuf, 0, c);
				// Console.WriteLine(">>> " + line);
				client.BeginRead(readBuf, readBufPos, readBuf.Length - readBufPos, Read, null);
			}

			var request = Encoding.ASCII.GetBytes($"GET {url} HTTP/1.1\r\nHost: {host}{(port==80?"":":"+port)}\r\nConnection: keep-alive\r\n\r\n");
			client.Write(request, 0, request.Length);

			// WaitFor(() => Encoding.UTF8.GetString(ms.ToArray()).Contains(expected) || !connected);

			sw = Stopwatch.StartNew();
			Assert.IsTrue(are.WaitOneTest(5000));
			Assert.IsTrue(connected);

			client.Write(request, 0, request.Length);

			sw = Stopwatch.StartNew();
			Assert.IsTrue(are.WaitOneTest(5000));
			Assert.IsTrue(connected);

			return ""; // Encoding.UTF8.GetString(ms.ToArray());
		}

		class ScopeMon : IDisposable
		{
			public ScopeMon(string name, TestClass test)
			{
				_scopeTime = Stopwatch.StartNew();
				_name = name;
				_test = test;
			}
			Stopwatch _scopeTime;
			private readonly string _name;
			private readonly TestClass _test;

			public void Dispose()
			{
				_scopeTime.Stop();
				Console.WriteLine($"[{_test._testTime.ElapsedMilliseconds:0000}] {_scopeTime.ElapsedMilliseconds}ms {_name}  ");
				Console.WriteLine();
			}
		}

		protected IDisposable Scope(string name)
		{
			return new ScopeMon(name, this);
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
