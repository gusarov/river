using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace River.Http
{
	public class HttpProxyHandler : Handler
	{
		static Encoding _utf8 = new UTF8Encoding(false, false);

		int _portRequested;
		string _dnsNameRequested;

		protected override void HandshakeHandler()
		{
			// get request from client
			if (EnsureReaded(1))
			{
				// we must wait till entire heder comes
				int eoh;
				var headers = HttpUtils.TryParseHttpHeader(_buffer, 0, _bufferReceivedCount, out eoh);
				if (headers != null)
				{
					headers.TryGetValue("HOST", out var hostHeader);
					headers.TryGetValue("_url_host", out var host);
					headers.TryGetValue("_url_port", out var port);

					var hostHeaderSplitter = hostHeader.IndexOf(':');
					var hostHeaderHost = hostHeaderSplitter > 0 ? hostHeader.Substring(0, hostHeaderSplitter) : hostHeader;
					var hostHeaderPort = hostHeaderSplitter > 0 ? hostHeader.Substring(hostHeaderSplitter + 1) : "80";

					if (string.IsNullOrEmpty(hostHeader))
					{
						_portRequested = string.IsNullOrEmpty(port) ? 80 : int.Parse(port);
						_dnsNameRequested = host;
					}
					else
					{
						_portRequested = int.Parse(hostHeaderPort);
						_dnsNameRequested = hostHeaderHost;
					}

					try
					{
						EstablishUpstream(new DestinationIdentifier
						{
							Host = _dnsNameRequested,
							Port = _portRequested,
						});

						if (headers["_verb"] == "CONNECT")
						{
							Stream.Write(_utf8.GetBytes("200 OK\r\n\r\n")); // ok to CONNECT
							// for connect - forward the rest of the buffer
							if (_bufferReceivedCount - eoh > 0)
							{
								Trace.WriteLine("Streaming - forward the rest >> " + (_bufferReceivedCount - eoh) + " bytes");
								SendForward(_buffer, eoh, _bufferReceivedCount - eoh);
							}
						}
						else
						{
							// otherwise forward entire buffer without change
							SendForward(_buffer, 0, _bufferReceivedCount);
						}
						BeginStreaming();
					}
					catch (Exception ex)
					{
						// write response
						Dispose();
					}
				}
				else
				{
					ReadMoreHandshake();
				}
			}
		}
	}
}
