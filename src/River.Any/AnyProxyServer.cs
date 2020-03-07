using River.Generic;
using System;

namespace River.Any
{
	public class AnyProxyServer : TcpServer<AnyHandler>
	{
		public AnyProxyServer()
		{

		}

		public AnyProxyServer(ServerConfig config)
		{
			Run(config);
		}

		protected override void ParseConfigCore(ServerConfig config)
		{
			_socks.ParseConfig(config);
			_http.ParseConfig(config);
			_httpWrap.ParseConfig(config);
		}

		internal Socks.SocksServer _socks = new Socks.SocksServer();
		internal Http.HttpProxyServer _http = new Http.HttpProxyServer();
		internal HttpWrap.HttpWrapServer _httpWrap = new HttpWrap.HttpWrapServer();
	}
}
