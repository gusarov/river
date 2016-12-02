using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace River
{
	public class RiverServer : IDisposable
	{
		protected static readonly Encoding _utf = new UTF8Encoding(false, false);

		private readonly TcpListener _listener;

		public void Dispose()
		{
			try
			{
				_listener?.Stop();
			}
			catch { }
		}

		public RiverServer(int port)
		{
			// listen both ipv6 and ipv4
#if Net45
			_listener = TcpListener.Create(port);
#else
			_listener = new TcpListener(IPAddress.IPv6Any, port);
			_listener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
#endif
			_listener.Start();
			_listener.BeginAcceptTcpClient(NewTcpClient, null);
		}

		private void NewTcpClient(IAsyncResult ar)
		{
			var tcpClient = _listener.EndAcceptTcpClient(ar);
			new RiverServerConnection(tcpClient);
			_listener.BeginAcceptTcpClient(NewTcpClient, null);
		}

		public class RiverServerConnection
		{
			private TcpClient _client;
			private NetworkStream _clientStream;
			private readonly byte[] _readBuffer = new byte[1024*32];
			private int _readBufferPos;

			private TcpClient _clientForward;
			private NetworkStream _clientStreamForward;
			private readonly byte[] _readBufferForward = new byte[1024*32];

			public void Dispose()
			{
				try
				{
					_client?.Close();
					_client = null;
				}
				catch
				{
				}
				try
				{
					_clientStream?.Close();
					_clientStream = null;
				}
				catch
				{
				}
				try
				{
					_clientForward?.Close();
					_clientForward = null;
				}
				catch
				{
				}
				try
				{
					_clientStreamForward?.Close();
					_clientStreamForward = null;
				}
				catch
				{
				}
			}

			public RiverServerConnection(TcpClient client)
			{
				_client = client;
				_clientStream = _client.GetStream();
				_clientStream.BeginRead(_readBuffer, 0, _readBuffer.Length, ReceivedInitiationFromClient, null);
			}

			private static readonly Regex _initiationParser = new Regex(@"&(\w+)=([^&]*)");

			private void ReceivedInitiationFromClient(IAsyncResult ar)
			{
				if (_clientStream == null)
				{
					return;
				}
				try
				{
					var count = _clientStream.EndRead(ar);
					if (count > 0)
					{
						// actual work - I assume that entire message already there
						var request = _utf.GetString(_readBuffer, 0, count);
						var args = new Dictionary<char, string>();
						int a = 0;
						foreach (Match match in _initiationParser.Matches(request))
						{
							var key = match.Groups[1].Value;
							var var = key[++a];
							args[var] = match.Groups[2].Value;
						}
						_clientForward = new TcpClient(args['h'], int.Parse(args['p']));
						_clientForward.NoDelay = true;
						_client.NoDelay = true;
						_clientStreamForward = _clientForward.GetStream();
						_clientStreamForward.BeginRead(_readBufferForward, 0, _readBufferForward.Length, ReceivedStreamFromForward, null);

						// send response
						var response = "HTTP/1.0 200 OK\r\n"
							+ "Connection: keep-alive\r\n"
							+ "Content-Type: text/html\r\n"
							+ "ETag: \"514a3625a6ffd21:0\"\r\n" // special keyword to indicate success
							+ "Content-Length: 0\r\n"
							+ "\r\n";
						var responseBuf = _utf.GetBytes(response);
						_clientStream.Write(responseBuf, 0, responseBuf.Length);

						// continue
						_clientStream.BeginRead(_readBuffer, 0, _readBuffer.Length, ReceivedStreamFromClient, null);
					}
				}
				catch (Exception ex)
				{
					Trace.TraceError("ReceivedFromClient Exception: " + ex);
					Dispose();
				}
			}


			private void ReceivedStreamFromForward(IAsyncResult ar)
			{
				if (_clientStreamForward == null)
				{
					return;
				}

				try
				{
					var count = _clientStreamForward.EndRead(ar);
					if (count > 0)
					{
						// do the job - pack the bytes back to river client

						var fakeHttpResponseString = $"HTTP/1.0 202 OK\r\n"
	+ $"Connection: keep-alive\r\n"
	//+ "User-Agent: Mozilla/5.0 (Windows NT 6.1; Win64; x64; rv:50.0) Gecko/20100101 Firefox/50.0\r\n"
	+ $"Accept: text/html\r\n"
	+ $"Content-Type: text/html\r\n"
	+ $"Cache-Control: no-cache"
	+ $"Pragma: no-cache"
	+ $"Accept-Encoding: gzip, deflate\r\n"
	+ $"Content-Length: {count}\r\n"
	+ "\r\n";
						var fakeHttpResponse = _utf.GetBytes(fakeHttpResponseString);
						var responseBuf = new byte[fakeHttpResponse.Length + count];
						Array.Copy(fakeHttpResponse, responseBuf, fakeHttpResponse.Length); // headers
						for (int i = 0; i < count; i++)
						{
							_readBufferForward[i] = (byte)(_readBufferForward[i] ^ 0xAA);
						}
						Array.Copy(_readBufferForward, 0, responseBuf, fakeHttpResponse.Length, count); // append body

						_clientStream.Write(responseBuf, 0, responseBuf.Length); // write all back to river client

						// continue reading from forward stream
						_clientStreamForward.BeginRead(_readBufferForward, 0, _readBufferForward.Length, ReceivedStreamFromForward, null);
					}
					else
					{
						Dispose();
					}
				}
				catch (Exception ex)
				{
					Trace.TraceError("ReceivedStreamFromForward Exception: " + ex);
					Dispose();
				}
			}

			private void ReceivedStreamFromClient(IAsyncResult ar)
			{
				if (_clientStream == null)
				{
					return;
				}

				try
				{
					var count = _clientStream.EndRead(ar);
					if (count > 0)
					{
						// do the job - decode the stream and forward it
						// header parsing limited to 1024 with char-to-byte encoding
						var responseHeaderString = Encoding.ASCII.GetString(_readBuffer, 0, count > 1024 ? 1024 : count);
						// parse content length and end of header
						var eoh = responseHeaderString.IndexOf("\r\n\r\n") + 4;
						if (eoh > 0)
						{
							var headers = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
							for (int i = 0; i < eoh - 4;)
							{
								var start = i;
								i = responseHeaderString.IndexOf("\r\n", i+1)+2;
								var sp = responseHeaderString.IndexOf(':', start);
								if (sp > i)
								{
									continue; // this is first line
								}
								var headerKey = responseHeaderString.Substring(start, sp - start).Trim();
								var headerValue = responseHeaderString.Substring(sp + 1, i - sp - 1);
								headers[headerKey] = headerValue.Trim();
							}
							string lenStr;
							if (!headers.TryGetValue("Content-Length", out lenStr))
							{
								throw new Exception("Content-Length is mandatory");
							}
							int len = int.Parse(lenStr);
							if (len < count + _readBufferPos - eoh)
							{
								// not complete body received!
								_readBufferPos += count;
								_clientStream.BeginRead(_readBuffer, _readBufferPos, _readBuffer.Length - _readBufferPos, ReceivedStreamFromClient, null);
							}
							else
							{
								// decode the body
								var data = new byte[len];
								Array.Copy(_readBuffer, eoh, data, 0, len);
								for (int i = 0; i < len; i++)
								{
									data[i] = (byte)(data[i] ^ 0xAA);
								}
								// forward
								_clientStreamForward.Write(data);

								// continue
								_readBufferPos = 0;
								_clientStream.BeginRead(_readBuffer, 0, _readBuffer.Length, ReceivedStreamFromClient, null);
							}
						}
						else
						{
							// continue waiting for full chunk
							_readBufferPos += count;
							_clientStream.BeginRead(_readBuffer, _readBufferPos, _readBuffer.Length - _readBufferPos, ReceivedStreamFromClient, null);
						}


					}
					else
					{
						Dispose();
					}
				}
				catch (Exception ex)
				{
					Trace.TraceError("ReceivedStreamFromClient Exception: " + ex);
					Dispose();
				}
			}
		}
	}
}
