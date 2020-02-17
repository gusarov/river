using River.Internal;
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

		public HttpProxyClientStream(string proxyHost, int proxyPort)
		{
			Plug(proxyHost, proxyPort);
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

		byte[] _readBuf = new byte[16 * 1024];

		public override void Route(string targetHost, int targetPort, bool? proxyDns = null)
		{
			var request = _utf8.GetBytes($@"CONNECT {targetHost}:{targetPort} HTTP/1.1
Host: {targetHost}:{targetPort}

");
			Write(request, 0, request.Length);

			var readed = 0;
			IDictionary<string, string> response;
			int eoh;
			do
			{
				var c = Stream.Read(_readBuf, readed, _readBuf.Length - readed);
				if (c == 0)
				{
					Close();
					throw new ConnectionClosingException();
				}
				readed += c;
				response = HttpUtils.TryParseHttpHeader(_readBuf, 0, readed, out eoh);
			} while (eoh < 0);

			// the response is not paresed here yet. If there is an error - will be disconnected anyway
			// but we must forward back everything beyond 
		}

	}
}
