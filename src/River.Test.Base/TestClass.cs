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
using System.Windows.Forms;

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

		[TestMethod]
		public void ZZ_Clean()
		{
			WaitForObjects();
		}

		[TestMethod]
		public void AA_Clean()
		{
			WaitForObjects();
		}

		[TestCleanup]
		public void BaseClean()
		{
			Profiling.Stamp("BaseClean");
			Explode();
		}

		static Dictionary<string, ObjectTracker> _trackers = new Dictionary<string, ObjectTracker>();

		public static void WaitForObjects()
		{
			var list = _trackers.SelectMany(kvp => kvp.Value.Weaks.Select(wr => (wr, kvp.Key))).ToArray();
			_trackers.Clear();

			// snapshot a list of objects created so far
			// some of them might be from concurrent tests
			// var list = new List<WeakReference>(ObjectTracker.Default.Items.Select(x => new WeakReference(x)));
			try
			{
				Console.WriteLine($"Cleaning {list.Count()} objects...");
				WaitFor(() =>
				{
					GC.Collect();
					GC.WaitForPendingFinalizers();
					return list.All(x => x.wr.Target == null);
					// return ObjectTracker.Default.Count == 0;
				});
				Console.WriteLine("All objects are clear");
			}
			catch
			{
				var objs = list.Where(x => x.wr.Target != null).ToArray();
				// var objs = ObjectTracker.Default.Items.Where(x => x != null).ToArray();
				Console.WriteLine($"Objects alive: {objs.Length} ======================");
				foreach (var item in objs)
				{
					Console.WriteLine(item.Key + ": " + item.wr.Target);
				}
				throw;
			}
		}

		Stopwatch _testTime;

		public TestContext TestContext { get; set; }

		[TestInitialize]
		public void BaseInit()
		{
			TestInitialized = true;
			_test = new object();
			// Explode();
			// ObjectTracker.Default.ResetCollection();
			_testTime = Stopwatch.StartNew();
			Profiling.Start();

			ObjectTracker.Default = (ObjectTracker)Activator.CreateInstance(typeof(ObjectTracker), true);
			ObjectTracker.Default.EnableCollection();
			_trackers[TestContext.TestName] = ObjectTracker.Default;
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
				if (sw.Elapsed.TotalSeconds > 3 && !Debugger.IsAttached)
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
			Profiling.Stamp("Test Read...");
			client.BeginRead(readBuf, 0, readBuf.Length, Read, null);
			// bool found = false;
			void Read(IAsyncResult ar)
			{
				var c = client.EndRead(ar);
				Profiling.Stamp("Test Read Done = " + c);
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
				Profiling.Stamp("Test Read...");
				client.BeginRead(readBuf, readBufPos, readBuf.Length - readBufPos, Read, null);
			}

			var request = Encoding.ASCII.GetBytes($"GET {url} HTTP/1.1\r\nHost: {host}{(port==80?"":":"+port)}\r\nConnection: keep-alive\r\n\r\n");
			Profiling.Stamp("Test Write...");
			client.Write(request, 0, request.Length);
			Profiling.Stamp("Test Write Done");

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
