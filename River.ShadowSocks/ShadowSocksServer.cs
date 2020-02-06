using River.Generic;
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
		public ShadowSocksServer(string password)
		{
			Password = password;
		}

		public ShadowSocksServer(string password, ServerConfig config) : base(config)
		{
			Password = password;
		}

		public string Password { get; set; }
	}
}
