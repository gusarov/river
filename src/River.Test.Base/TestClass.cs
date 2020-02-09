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

		protected int GetFreePort()
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

		/// <summary>
		/// Test current connection to web server
		/// E.g. you can connect to httpbin.org to do this testing
		/// </summary>
		protected static string TestConnction(Stream client)
		{
			var ms = new MemoryStream();
			var readBuf = new byte[16 * 1024];
			client.BeginRead(readBuf, 0, readBuf.Length, Read, null);
			void Read(IAsyncResult ar)
			{
				var c = client.EndRead(ar);
				if (c == 0) return;
				var line = Encoding.UTF8.GetString(readBuf, 0, c);
				Console.WriteLine(">>> " + line);
				ms.Write(readBuf, 0, c);
				client.BeginRead(readBuf, 0, readBuf.Length, Read, null);
			}


			var request = Encoding.ASCII.GetBytes("GET /get HTTP/1.0\r\nConnection: keep-alive\r\n\r\n");
			client.Write(request, 0, request.Length);

			WaitFor(() => Encoding.UTF8.GetString(ms.ToArray()).Contains("gunicorn"));

			ms = new MemoryStream();

			client.Write(request, 0, request.Length);
			WaitFor(() => Encoding.UTF8.GetString(ms.ToArray()).Contains("gunicorn"));

			return Encoding.UTF8.GetString(ms.ToArray());
		}
	}
}
