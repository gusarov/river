using River.Generic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace River.Socks
{
	public class ShadowSocksServer : TcpServer<ShadowSocksHandler>
	{
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
