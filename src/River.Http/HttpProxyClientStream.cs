using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace River.Http
{
	public class HttpProxyClientStream : ClientStream
	{
		public HttpProxyClientStream()
		{

		}

		public HttpProxyClientStream(string proxyHost, int proxyPort, string targetHost, int targetPort)
		{
			Plug(proxyHost, proxyPort);
			Route(targetHost, targetPort);
		}

		public HttpProxyClientStream(Stream stream, string targetHost, int targetPort)
		{
			Plug(null, stream);
			Route(targetHost, targetPort);
		}

		public void Plug(string host, int port)
		{
			ClientStreamExtensions.Plug(this, host, port);
		}

		public override void Route(string targetHost, int targetPort, bool? proxyDns = null)
		{
			// HTTP Proxy is transparent, nothing to do here. Header must include full server name
		}

	}
}
