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

			for (int i = 0; i < args.Length; i++)
			{
				switch (args[i].ToUpperInvariant())
				{
					case "-L":
						{
							// Listen
							var listener = args[++i];
							// Resolver.GetClientStreamType
							break;
						}
					default:
						break;
				}
			}
		}
	}
}
