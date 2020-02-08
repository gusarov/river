using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using River.Socks;

namespace River.V2.Tests
{

	public class HttpBinTest
	{
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

	public static class Tracker
	{
		static ConcurrentDictionary<object, List<IDisposable>> _dic = new ConcurrentDictionary<object, List<IDisposable>>();

		public static T Track<T>(this T item, object state) where T : IDisposable
		{
			var list = _dic.GetOrAdd(state, _ => new List<IDisposable>());
			list.Add(item);
			return item;
		}

		public static void Explode(object state)
		{
			if (_dic.TryGetValue(state, out var list))
			{
				foreach (var item in list)
				{
					try
					{
						item.Dispose();
					}
					catch { }
				}
			}
			_dic.TryRemove(state, out var _);
		}
	}

	[TestClass]
	public class ChainTests : HttpBinTest
	{
		[TestCleanup]
		public void Clean()
		{
			Tracker.Explode(this);
		}

		[TestMethod]
		public void Should_chain_3_socks()
		{
			_ = typeof(Socks4ClientStream); // to load the type

			var proxy1 = new SocksServer(new ServerConfig
			{
				EndPoints =
				{
					new IPEndPoint(IPAddress.Loopback, 2001),
					new IPEndPoint(IPAddress.IPv6Loopback, 2001),
				},
			}).Track(this);

			var proxy2 = new SocksServer(new ServerConfig
			{
				EndPoints =
				{
					new IPEndPoint(IPAddress.Loopback, 2002),
					new IPEndPoint(IPAddress.IPv6Loopback, 2002),
				},
			}).Track(this);

			var proxy3 = new SocksServer(new ServerConfig
			{
				EndPoints =
				{
					new IPEndPoint(IPAddress.Loopback, 2003),
					new IPEndPoint(IPAddress.IPv6Loopback, 2003),
				},
			}).Track(this);

			var proxy = new SocksServer(new ServerConfig
			{
				EndPoints =
				{
					new IPEndPoint(IPAddress.Loopback, 2000),
					new IPEndPoint(IPAddress.IPv6Loopback, 2000),
				},
			})
			{
				Chain = {
					"socks4://127.0.0.1:2001",
					"socks4://127.0.0.1:2002",
					"socks4://127.0.0.1:2003",
				},
			}.Track(this);

			var cli = new Socks4ClientStream("localhost", 2000, "httpbin.org", 80).Track(this);
			TestConnction(cli);
		}
	}
}
