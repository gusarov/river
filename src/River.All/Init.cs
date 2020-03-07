using River.Any;
using River.Http;
using River.Internal;
using River.SelfService;
using River.ShadowSocks;
using River.Socks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace River
{
	public static class RiverInit
	{
		public static void RegAll()
		{
			Resolver.RegisterOverride("_river", x => new RiverSelfService());

			Resolver.RegisterSchema<SocksServer, Socks4ClientStream>("socks4");
			Resolver.RegisterSchema<SocksServer, Socks5ClientStream>("socks5");
			Resolver.RegisterSchema<ShadowSocksServer, ShadowSocksClientStream>("ss");
			Resolver.RegisterSchema<HttpProxyServer, HttpProxyClientStream>("http");
			Resolver.RegisterSchemaServer<SocksServer>("socks");
			Resolver.RegisterSchemaServer<AnyProxyServer>("any");
		}
	}
}
