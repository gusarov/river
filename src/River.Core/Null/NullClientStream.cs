using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace River
{
	/// <summary>
	/// Just create real direct connection
	/// </summary>
	sealed class NullClientStream : ClientStream
	{
		public override void Route(string targetHost, int targetPort, bool? proxyDns = null)
		{
			// just do nothing, we already connected
			throw new Exception($"Where are you going to route? This is {nameof(NullClientStream)}");
		}
	}
}
