using River.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace River.Http
{
	public class HttpProxyServer : TcpServer<HttpProxyHandler>
	{
		public HttpProxyServer()
		{

		}

		public HttpProxyServer(ServerConfig config)
		{
			Run(config);
		}
	}
}
