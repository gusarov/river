using River.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace River.Any
{
	public class AnyHandler : Handler
	{
		new AnyProxyServer Server => (AnyProxyServer)base.Server;

		ReStream _restream;

		protected override Stream WrapStream(Stream stream)
		{
			return _restream = new ReStream(stream);
		}

		protected override void HandshakeHandler()
		{
			switch (_buffer[0])
			{
				case 4:
				case 5:
					SwitchToHandler<Socks.SocksHandler>(Server._socks);
					break;

				case (byte)'P': // HTTP PROXY PUT POST PATCH
				case (byte)'G': // HTTP PROXY GET
				case (byte)'D': // HTTP PROXY DELETE
				case (byte)'C': // HTTP PROXY CONNECT
				case (byte)'H': // HTTP PROXY HEAD
				case (byte)'T': // HTTP PROXY TRACE
				case (byte)'O': // HTTP PROXY OPTIONS
					{
						var request = HttpUtils.TryParseHttpHeader(_buffer, 0, _bufferReceivedCount);
						if (request != null)
						{
							if (request.Keys.Where(x => !x.StartsWith("_")).Count() == 3
								&& request["_verb"].ToUpperInvariant() == "POST"
								&& request["_http_ver"] == "1.1"
								&& request.ContainsKey("Host")
								&& request.ContainsKey("Content-Length")
								&& request.ContainsKey("Connection")
								)
							{
								SwitchToHandler<HttpWrap.HttpWrapHandler>(Server._httpWrap);
							}
							else
							{
								SwitchToHandler<Http.HttpProxyHandler>(Server._http);
							}
						}
						else
						{
							ReadMoreHandshake();
						}
					}
					break;
				default:
					throw new Exception("Not supported");
			}

			IsResigned = true;
			return;
		}

		void SwitchToHandler<T>(RiverServer server) where T : Handler, new()
		{
			var handler = new T();
			_restream.ResetReader();
			handler.Init(server, Client, _restream);
			// handler.SwitchFrom(this, server);
		}
	}
}
