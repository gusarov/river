using River.Generic;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace River.Socks
{
	public class SocksServer : TcpServer<SocksHandler>
	{
		public SocksServer()
		{

		}

		public SocksServer(ServerConfig config)
		{
			Run(config);
		}
	}
}
