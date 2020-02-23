using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace River
{
	public static class ClientStreamExtensions
	{
		public static void Plug(this ClientStream clientStream, string proxyServer, int port)
		{
			if (clientStream is null)
			{
				throw new ArgumentNullException(nameof(clientStream));
			}

			if (IPAddress.TryParse(proxyServer, out var ip))
			{
				if (ip.AddressFamily == AddressFamily.InterNetworkV6)
				{
					proxyServer = $"[{ip.ToString()}]";
				}
			}

			var uri = new Uri($"tcp://{proxyServer}:{port}");

			Profiling.Stamp("ClientStreamExtensions Plug...");
			clientStream.Plug(uri);
			Profiling.Stamp("ClientStreamExtensions Pluged");
		}
	}
}
