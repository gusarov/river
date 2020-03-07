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

		protected override void ParseConfigCore(ServerConfig config)
		{
			var algo = config["user"];
			Password = config["password"];
		}

		public string Password { get; set; }

	}
}
