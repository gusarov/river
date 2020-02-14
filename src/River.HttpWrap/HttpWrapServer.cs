using River.Generic;
using System;

namespace River.HttpWrap
{
	public class HttpWrapServer : TcpServer<HttpWrapHandler>
	{
		public HttpWrapServer()
		{

		}

		public HttpWrapServer(ServerConfig config)
		{
			Run(config);
		}
	}
}
