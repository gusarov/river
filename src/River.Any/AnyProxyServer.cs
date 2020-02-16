using River.Generic;
using River.Socks;
using System;

namespace River.Any
{
	public class AnyProxyServer : SocksServer
	{
		public AnyProxyServer()
		{

		}

		public AnyProxyServer(ServerConfig config)
			: base(config)
		{
		}
	}
}
