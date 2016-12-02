using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace River.ConsoleServer
{
	class Program
	{
		static void Main(string[] args)
		{
			// FireFox => SocksServer => RiverClient => Fiddler => RiverServer => Internet

/*
			int w, c;
			ThreadPool.GetMaxThreads(out w, out c);
			ThreadPool.SetMaxThreads(1024, 1024);
*/
			Trace.Listeners.Add(new ConsoleTraceListener());

			var riverServer = new RiverServer(810);
			var server = new SocksServerToRiverClient(1081, "127.0.0.1", 810);

			//var server = new SocksProxyServer(1081);

			int prevThreads = 0;
			while (true)
			{
				var threads = Process.GetCurrentProcess().Threads.Count;
				if (threads != prevThreads)
				{
					Console.WriteLine("Threads count: " + threads);
					prevThreads = threads;
				}
				Thread.Sleep(200);
			}
		}
	}
}
