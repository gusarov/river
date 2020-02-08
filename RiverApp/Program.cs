using River;
using River.ShadowSocks;
using River.Socks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiverApp
{
	class Program
	{
		static void Main(string[] args)
		{
			Resolver.RegisterSchema<SocksServer, Socks4ClientStream>("socks4");
			Resolver.RegisterSchema<SocksServer, Socks5ClientStream>("socks5");
			Resolver.RegisterSchema<SocksServer, ShadowSocksClientStream>("ss");
			Resolver.RegisterSchemaServer<SocksServer>("socks");

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
