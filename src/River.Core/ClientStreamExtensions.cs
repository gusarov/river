using System;
using System.Collections.Generic;
using System.Linq;
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

			clientStream.Plug(new Uri($"{proxyServer}:{port}"));
		}
	}
}
