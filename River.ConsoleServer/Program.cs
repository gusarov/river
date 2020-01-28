using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using River.Http;
using River.Socks;

namespace River.ConsoleServer
{
	class Program
	{

		static void Main(string[] args)
		{
			// FireFox => SocksServer => RiverClient => Fiddler => RiverServer => Internet

			// Trace.Listeners.Add(new ConsoleTraceListener());

			var server = new SocksServer(new ListenerConfig
			{
				EndPoints =
				{
					new IPEndPoint(IPAddress.IPv6Loopback, 1080),
					new IPEndPoint(IPAddress.Loopback, 1080),
				},
			})
			{
				Forwarder = new SocksForwarder("127.0.0.1", 1081)
				{

				}
				/*
				Forwarder = new SocksForwarder("RHOP2", 1080)
				{
					NextForwarder = new HttpForwarder("10.7.0.1", 1080)
					{

					}
				}
				*/
			};
			Console.ReadLine();
		}
	}
}
