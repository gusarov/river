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

		public override void Run(ServerConfig config)
		{
			if (config is null)
			{
				throw new ArgumentNullException(nameof(config));
			}

			var algo = config["user"];
			Password = config["password"];
			base.Run(config);
		}

		public string Password { get; set; }

	}
}
