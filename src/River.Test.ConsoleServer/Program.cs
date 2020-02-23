using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using River.Any;
using River.ChaCha;
using River.Http;
using River.Internal;
using River.SelfService;
using River.ShadowSocks;
using River.Socks;

namespace River.Test.ConsoleServer
{
	class Program
	{
		static void Main()
		{
			RiverInit.RegAll();

			var server1 = new AnyProxyServer
			{
				Chain =
				{
					// "ss://chacha20:123@127.0.0.1:8338",
				},
			};
			server1.Run("socks://0.0.0.0:1080");

/*			
			var server2 = new ShadowSocksServer
			{
				Chain =
				{
					// "ss://chacha20:123@127.0.0.1:8338",
				},
			};
			server2.Run("ss://chacha20:123@0.0.0.0:8338");
*/

			Console.ReadLine();
		}

		static void Main2()
		{
			/*
			var cli = new TcpClient("httpbin.org", 80);
			var stream = cli.GetStream2();
			*/

			var step1 = new Socks4ClientStream();
			step1.Plug("127.0.0.1", 1080);
			step1.Route("127.0.0.1", 1081);

			var step2 = new Socks4ClientStream(step1, "127.0.0.1", 1082);
			var step3 = new Socks4ClientStream(step2, "httpbin.org", 80);

			var stream = step3;

			void read()
			{
				var buf = new byte[16 * 1024];
				var c = stream.Read(buf, 0, buf.Length);
				if (c > 0)
				{
					var str = Encoding.UTF8.GetString(buf, 0, c);
					Console.WriteLine(str);
					// Console.WriteLine("\r\n==========<< " + c);
					Task.Run(delegate
					{
						read();
					});
				}
			}
			Task.Run(delegate
			{
				read();
			});

			var buf2 = Encoding.ASCII.GetBytes("GET /\r\n\r\n");
			stream.Write(buf2, 0, buf2.Length);
			Console.ReadLine();
		}

		static void Main3(string[] args)
		{
			var cli = new ShadowSocksClientStream();
			cli.Plug(new Uri("ss://chacha20:abc@RHOP2:8338"));
			cli.Plug("RHOP2", 8338);
			cli.Route("10.7.1.1", 8338);

			var cli2 = new ShadowSocksClientStream();
			cli2.Plug(new Uri("ss://chacha20:abc@_:0"), cli);
			cli2.Route("10.7.0.1", 80);

			void read()
			{
				var buf = new byte[1024 * 1024];
				var c = cli2.Read(buf, 0, buf.Length);
				if (c > 0)
				{
					var str = Encoding.UTF8.GetString(buf, 0, c);
					Console.WriteLine(str);
					Console.WriteLine("\r\n==========<< " + c);
					Task.Run(delegate
					{
						read();
					});
				}
			}
			Task.Run(delegate
			{
				read();
			});

			cli2.Write(Encoding.ASCII.GetBytes(@"GET / HTTP/1.1
Host: httpbin.org
Keep-Alive: true

"));


			/*
			Thread.Sleep(1000);
			Console.WriteLine("============");
			*/
			Console.ReadLine();

			cli2.Write(Encoding.ASCII.GetBytes(@"GET / HTTP/1.1
Host: httpbin.org

"));

			Console.ReadLine();

			cli2.Write(Encoding.ASCII.GetBytes("GET /\r\n\r\n"));
			/*
			var buf = new byte[1024 * 1024];
			var c = cli2.Read(buf, 0, buf.Length);
			var str = Encoding.UTF8.GetString(buf, 0, c);
			Console.WriteLine(str);
			if (str.Contains("THIS IS SUPER PRIVATE SITE"))
			{
				Console.WriteLine("THIS WORKS!!!");
			}
			Console.ReadLine();
			*/
		}

		static void Main4(string[] args)
		{
			var cli1 = new ShadowSocksClientStream();
			cli1.Plug(new Uri($"ss://c:123@127.0.0.1:8338"));
			cli1.Plug("127.0.0.1", 8338);
			cli1.Route("httpbin.org", 80);

			// var cli2 = new Socks4Client();
			// cli2.Plug("127.0.0.1", 1079);
			// cli2.Route("httpbin.org", 80);

			var cli = cli1;

			void read()
			{
				var buf = new byte[1024 * 1024];
				var c = cli.Read(buf, 0, buf.Length);
				if (c > 0)
				{
					var str = Encoding.UTF8.GetString(buf, 0, c);
					Console.WriteLine(str);
					Console.WriteLine("\r\n==========");
					Task.Run(delegate
					{
						read();
					});
				}
			}
			Task.Run(delegate
			{
				read();
			});


			

			cli.Write(Encoding.ASCII.GetBytes(@"GET / HTTP/1.1
Host: httpbin.org
Keep-Alive: true

"));


			Thread.Sleep(1000);
			Console.WriteLine("============");
			Console.ReadLine();

			cli.Write(Encoding.ASCII.GetBytes(@"GET / HTTP/1.1
Host: httpbin.org

"));

			Console.ReadLine();
		}

		static void Main5(string[] args)
		{
			// FireFox => SocksServer => RiverClient => Fiddler => RiverServer => Internet

			// Trace.Listeners.Add(new ConsoleTraceListener());
			new Socks4ClientStream();
			new Socks5ClientStream();

			new ShadowSocksClientStream();

			var server = new ShadowSocksServer
			{
				Chain =
				{
					// "socks4://rhop2:1080",
					// "socks5://10.7.1.1:1080",
				},
			};
			server.Run("ss://chacha20:123@0.0.0.0:8338");

			// connect to this server and ask super secret web site behind 2 private lan
			/*
			var cli = new Socks4ClientStream("127.0.0.1", 1080, "10.7.0.1", 80);

			var buf = new byte[16 * 1024];
			void read()
			{
				var c = cli.Read(buf, 0, buf.Length);
				if (c > 0)
				{
					var str = Encoding.UTF8.GetString(buf, 0, c);
					Console.WriteLine(str);
					Console.WriteLine("\r\n==========");
					Task.Run(delegate
					{
						read();
					});
				}
			}
			Task.Run(delegate
			{
				read();
			});

			var req = Encoding.ASCII.GetBytes(@"GET / HTTP/1.1
Host: httpbin.org
Keep-Alive: true

");
			cli.Write(req);
			*/
			Console.ReadLine();
		}


		static void Main6()
		{
			/*
			var ss = new ShadowSocksServer();
			ss.Run("ss://chacha20:123@0.0.0.0:18338");
			*/

			string host = "www.google.com";
			// default null
			var cli = new ShadowSocksClientStream("chacha20", "123", "127.0.0.1", 18338, host, 80);
			TestConnction(cli, host);

			Console.ReadLine();
		}

		static string TestConnction(Stream client, string host = "www.google.com")
		{
			var expected = "onclick=gbar.logger"; // google.com

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


			var request = Encoding.ASCII.GetBytes($"GET / HTTP/1.1\r\nHost: {host}\r\nConnection: keep-alive\r\n\r\n");
			client.Write(request, 0, request.Length);

			// WaitFor(() => Encoding.UTF8.GetString(ms.ToArray()).Contains(expected) || !connected);
			IsTrue(are.WaitOne(5000), "wait1");
			IsTrue(connected, "connected1");

			client.Write(request, 0, request.Length);

			IsTrue(are.WaitOne(5000), "wait2");
			IsTrue(connected, "connected2");

			return ""; // Encoding.UTF8.GetString(ms.ToArray());
		}

		static void IsTrue(bool cond, string msg = null)
		{
			var b = Console.ForegroundColor;
			if (!cond)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("IsTrue failed: " + msg);
			}
			else
			{
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine("IsTrue pass");
			}
			Console.ForegroundColor = b;
		}


		static void Main9()
		{
			var ss = new HttpProxyServer();
			ss.Run("http://0.0.0.0:8080");

			Console.ReadLine();
		}

		static void Main10()
		{
			/*
			Console.WriteLine("Take snapshot1");
			// Console.ReadLine();
			var test = new ChainTests();
			test.Init();
			test.Should_chain_3_socks();
			try
			{
				test.Clean();
			}
			catch { }
			Console.WriteLine("Take snapshot2");
			// Console.ReadLine();
			*/
			
			var test = new WrapHttpTests();
			test.BaseInit();
			test.Should_wrap_http();
			try
			{
				test.BaseClean();
			}
			catch { }

		}
	}
}
