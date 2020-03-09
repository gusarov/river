using River.Generic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace River.ShadowSocks
{
	public class PortForwardingServer : TcpServer<PortForwardingHandler>
	{
		public PortForwardingServer()
		{

		}

		public PortForwardingServer(ServerConfig config)
		{
			Run(config);
		}

		protected override void RunCore(ServerConfig config)
		{
			if (config is null)
			{
				throw new ArgumentNullException(nameof(config));
			}

			var uri = config.Uri;
			var path = uri.LocalPath.TrimStart('/');
			var i = path.IndexOf(':');
			TargetHost = path.Substring(0, i);
			TargetPort = int.Parse(path.Substring(i + 1));
			base.RunCore(config);
		}

		public string TargetHost { get; set; }
		public int TargetPort { get; set; }
	}
}
