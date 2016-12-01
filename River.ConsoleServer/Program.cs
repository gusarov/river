using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace River.ConsoleServer
{
	class Program
	{
		static void Main(string[] args)
		{
			Trace.Listeners.Add(new ConsoleTraceListener());
			new SocksServer().Listen(1080);
			Console.ReadLine();
		}
	}
}
