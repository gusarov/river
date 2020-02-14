using River;
using River.Internal;
using River.ShadowSocks;
using River.Socks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiverApp
{
	class Program
	{
		static void Main(string[] args)
		{ 
			RiverInit.RegAll();

			var servers = new List<(RiverServer, Uri)>();
			var forwarders = new List<string>();

			for (var i = 0; i < args.Length; i++)
			{
				switch (args[i].ToUpperInvariant())
				{
					case "-L":
						{
							// Listen
							var listener = args[++i];
							var uri = new Uri(listener);
							var serverType = Resolver.GetServerType(uri);
							if (serverType == null) {
								throw new Exception($"Server type {uri.Scheme} is unknown");
							}
							var server = (RiverServer)Activator.CreateInstance(serverType);
							servers.Add((server, uri));
							break;
						}
					case "-F":
						{
							// Forward
							var proxy = args[++i];
							forwarders.Add(proxy);
							break;
						}
					case "-EVENTLOG":
						{
							Console.WriteLine("Generting event log...");
							if (int.TryParse(args[++i], out var eventId))
							{
								using (var eventLog = new EventLog("Application"))
								{
									eventLog.Source = "Application";
									eventLog.WriteEntry("EventLogTriggeer", EventLogEntryType.Information, eventId);
								}
							}
							break;
						}

					default:
						break;
				}
			}

			foreach (var (server, uri) in servers)
			{
				foreach (var fwd in forwarders)
				{
					server.Chain.Add(fwd);
				}
				server.Run(uri);
			}

			Console.WriteLine("Press any key to stop . . .");
			Console.ReadLine();
		}
	}
}
