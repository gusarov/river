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
		private bool _isDisposing;

		public void Dispose()
		{
			_isDisposing = true;
			try
			{
				_listener?.Stop();
			}
			catch
			{
			}
		}

		internal string _bypassForward;

		public RiverServer(int port, string bypass = null)
		{
			_bypassForward = bypass;

			// listen both ipv6 and ipv4
#if Net45
			_listener = TcpListener.Create(port);
#else
			_listener = new TcpListener(IPAddress.IPv6Any, port);
			_listener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
#endif
			// allow specific ip binding to override traffic
			// _listener.ExclusiveAddressUse = true;
			_listener.Start();
			_listener.BeginAcceptTcpClient(NewTcpClient, null);
		}

		private void NewTcpClient(IAsyncResult ar)
		{
			try
			{
				var tcpClient = _listener.EndAcceptTcpClient(ar);
				new RiverServerConnection(this, tcpClient);
			}
			catch (ObjectDisposedException)
			{
			}
			catch (Exception ex)
			{
				Trace.TraceError("River Server NewTcpClient: " + ex);
			}
			if (!_isDisposing)
			{
				_listener.BeginAcceptTcpClient(NewTcpClient, null);
			}
		}

		public class RiverServerConnection
		{
			private bool _disposed;
			private TcpClient _client;
			private NetworkStream _clientStream;
			private readonly byte[] _readBuffer = new byte[1024 * 128];
			private int _readBufferPos;

			private TcpClient _clientForward;
			private NetworkStream _clientStreamForward;
			private readonly byte[] _readBufferForward = new byte[1024 * 16];

			public void Dispose()
			{
				_disposed = true;
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

			RiverServer _server;

			public RiverServerConnection(RiverServer server, TcpClient client)
			{
				Trace.WriteLine("River connection from: " + client.Client.RemoteEndPoint);
				_server = server;
				_client = client;
				_clientStream = _client.GetStream();
				_clientStream.BeginRead(_readBuffer, 0, _readBuffer.Length, ReceivedInitiationFromClient, null);
			}

			private static readonly Regex _initiationParser = new Regex(@"&(\w+)=([^&]*)");
			private static readonly Regex _initiationHostHeaderParser = new Regex(@"\r\nHost: (?'hh'[^\r\n]+)");

			bool _isBypass;

			private void ReceivedInitiationFromClient(IAsyncResult ar)
			{
				if (_disposed)
				{
					return;
				}
				try
				{
					var count = _clientStream.EndRead(ar);
					if (count > 0
#if CC
|| _client.Connected
#endif
						)
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
						if (!args.ContainsKey('c') || !args.ContainsKey('h') || !args.ContainsKey('p') || !args.ContainsKey('v'))
						{
							// send response
							if (string.IsNullOrEmpty(_server._bypassForward))
							{
								// 404 :)
								var errResponse = "HTTP/1.0 404 Not Found\r\n"
												+ "Content-Type: text/html\r\n"
												+ "\r\n:)";
								var errResponseBuf = _utf.GetBytes(errResponse);
								_clientStream.Write(errResponseBuf, 0, errResponseBuf.Length);
								Dispose();
								Trace.WriteLine("Bad initial request");
							}
							else if (_server._bypassForward.StartsWith("R:")) // use "R:https://+" to provide permanent SSL upgrade
							{
								// Redirrect
								// allow + as a placeholder for host header
								var hh = _initiationHostHeaderParser.Match(request).Groups["hh"].Value;
								var target = _server._bypassForward.Substring(2).Replace("+", hh);
								var errResponse = "HTTP/1.1 301 Moved Permanently\r\n"
												+ $"Location: {target}\r\n"
												+ "X-XSS-Protection: 1; mode=block\r\n"
												+ "\r\n"
												+ "\r\n"
												;
								var errResponseBuf = _utf.GetBytes(errResponse);
								_clientStream.Write(errResponseBuf, 0, errResponseBuf.Length);
								Dispose();
								Trace.WriteLine("Request redirrected to: " + target);
							}
							else
							{
								// BYPASS
								var bypass = _server._bypassForward;
								Trace.WriteLine($"connecting to bypass: {bypass}");
								_clientForward = new TcpClient();
								string bServer;
								int bPort;
								if (bypass.Contains(':'))
								{
									var bParts = bypass.Split(':');
									bServer = bParts[0];
									bPort = int.Parse(bParts[1]);
								}
								else
								{
									bServer = bypass;
									bPort = 80;
								}
								_isBypass = true;
								_clientForward.Connect(bServer, bPort);

								_clientForward.NoDelay = true;
								_client.NoDelay = true;
								_clientStreamForward = _clientForward.GetStream();
								_clientStreamForward.BeginRead(_readBufferForward, 0, _readBufferForward.Length, ReceivedStreamFromForward, null);

								// repeat request
								_clientStreamForward.Write(_readBuffer, 0, count);

								// continue
								_clientStream.BeginRead(_readBuffer, 0, _readBuffer.Length, ReceivedStreamFromClient, null);
							}
						}
						else
						{
							// GOOD - FORWARD
							Trace.WriteLine($"connecting to {args['h']}:{args['p']}...");
							//var localEndpoint = new IPEndPoint(IPAddress.Parse("192.168.137.57"), 0);
							_clientForward = new TcpClient( /*localEndpoint*/);
							_clientForward.Connect(args['h'], int.Parse(args['p']));


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
				}
				catch (Exception ex)
				{
					Trace.TraceError("ReceivedFromClient Exception: " + ex);
					Dispose();
				}
			}

			private void ReceivedStreamFromForward(IAsyncResult ar)
			{
				if (_disposed)
				{
					return;
				}

				try
				{
					var count = _clientStreamForward.EndRead(ar);
					if (count > 0
#if CC
						|| _clientForward.Connected
#endif
						)
					{
						if (_isBypass)
						{
							_clientStream.Write(_readBufferForward, 0, count); // write back to river client
						}
						else
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

							Trace.WriteLine($"<< back to {_client.Client.RemoteEndPoint} << from {_clientForward.Client.RemoteEndPoint} {count} bytes");
							_clientStream.Write(responseBuf, 0, responseBuf.Length); // write all back to river client
						}

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
				if (_disposed)
				{
					return;
				}

				try
				{
					var count = _clientStream.EndRead(ar);
					if (count > 0
#if CC
						|| _client.Connected
#endif
						)
					{
						ReceivedStreamFromClient(count);
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

			private void ReceivedStreamFromClient(int count)
			{
				if (_isBypass)
				{
					_clientStreamForward.Write(_readBuffer, 0, count);
					_clientStream.BeginRead(_readBuffer, 0, _readBuffer.Length, ReceivedStreamFromClient, null);
					return;
				}

				// do the job - decode the stream and forward it
				int eoh;
				var headers = Utils.TryParseHttpHeader(_readBuffer, 0, count + _readBufferPos, out eoh);
				if (headers != null)
				{
					string lenStr;
					if (!headers.TryGetValue("Content-Length", out lenStr))
					{
						throw new Exception("Content-Length is mandatory");
					}
					int len = int.Parse(lenStr);
					if (len + Utils.MaxHeaderSize >= _readBuffer.Length)
					{
						throw new Exception($"ReceivedStreamFromClient: This package {len} with headers {Utils.MaxHeaderSize} is larger than receiving buffer {_readBuffer.Length}");
					}
					if (len > count + _readBufferPos - eoh)
					{
						// not complete body received! Wait for more data.
						_readBufferPos += count;
						_clientStream.BeginRead(_readBuffer, _readBufferPos, _readBuffer.Length - _readBufferPos, ReceivedStreamFromClient, null);
					}
					else
					{
						// decode the body
						byte[] data = new byte[len];
						Array.Copy(_readBuffer, eoh, data, 0, len);
						for (int i = 0; i < len; i++)
						{
							data[i] = (byte)(data[i] ^ 0xAA);
						}
						Trace.WriteLine($">> from {_client.Client.RemoteEndPoint} >> to {_clientForward.Client.RemoteEndPoint} {len} bytes");
						// forward
						_clientStreamForward.Write(data);

						// detect more data available
						// process remaining part of message
						if (len + eoh < _readBufferPos + count)
						{
							Array.Copy(_readBuffer, len + eoh, _readBuffer, 0, _readBufferPos + count - len - eoh);
							var len2 = _readBufferPos + count - len - eoh;
							_readBufferPos = 0;
							Trace.WriteLine($"ReceivedStreamFromClient - have extra {len2} bytes, call ReceivedStreamFromClient");
							ReceivedStreamFromClient(len2);
						}
						else
						{
							// continue
							_readBufferPos = 0;
							_clientStream.BeginRead(_readBuffer, 0, _readBuffer.Length, ReceivedStreamFromClient, null);
						}
					}
				}
				else
				{
					// continue waiting for full chunk
					Trace.WriteLine($">> not complete, reading some more...");
					_readBufferPos += count;
					_clientStream.BeginRead(_readBuffer, _readBufferPos, _readBuffer.Length - _readBufferPos, ReceivedStreamFromClient, null);
				}

			}
		}
	}
}
