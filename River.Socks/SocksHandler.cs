using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace River.Socks
{
	public class SocksHandler : Handler
	{
		protected static readonly Encoding _utf = new UTF8Encoding(false, false);
		private readonly SocksServer _server;

		public SocksHandler(SocksServer server, TcpClient client)
			: base(server, client)
		{
			_server = server;
		}

		IPAddress _addressRequested;
		string _dnsNameRequested;
		int _portRequested;
		int _bufferProcessedCount;
		private bool _authenticationNegotiated;

		protected override void HandshakeHandler()
		{
			// get request from client
			if (EnsureReaded(1))
			{
				Trace.WriteLine($"Negotiating - v{_buffer[0]} received from client {_bufferReceivedCount} bytes on thread #{Thread.CurrentThread.ManagedThreadId} port {_client.Client.RemoteEndPoint}");

				switch (_buffer[0])
				{
					case 4: // SOCKS4
						if (EnsureReaded(8))
						{
							if (_buffer[1] != 1)
							{
								throw new NotSupportedException("command type not supported");
							}
							_portRequested = _buffer[2] * 256 + _buffer[3];
							if (_buffer[4] != 0) // 0 means v4a mode (0.0.0.X)
							{
								var bufAddress4 = new byte[4];
								Array.Copy(_buffer, 4, bufAddress4, 0, 4);
								_addressRequested = new IPAddress(bufAddress4);
							}
							// read user id, throw it away and look for null
							bool nullOk = false;
							for (int i = 1; i < 256; i++)
							{
								EnsureReaded(8 + i);
								if (_buffer[7 + i] == 0)
								{
									_bufferProcessedCount = 8 + i;
									nullOk = true;
									break;
								}
							}
							if (!nullOk)
							{
								throw new Exception("End of user id string not found within 256 chars");
							}

							if (_addressRequested == null) // ver 4a mode - read dns name
							{
								// read dns name
								string dnsName = null;
								for (int i = 1; i < 256; i++)
								{
									EnsureReaded(_bufferProcessedCount + i);
									if (_buffer[_bufferProcessedCount + i - 1] == 0)
									{
										dnsName = _utf.GetString(_buffer, _bufferProcessedCount, i - 1);
										_bufferProcessedCount += i;
										break;
									}
								}
								if (!string.IsNullOrWhiteSpace(dnsName))
								{
									_dnsNameRequested = dnsName;
								}
								else
								{
									throw new Exception("Host name not provided");
								}
							}
							Exception ex = null;
							try
							{
								EstablishUpstream(new DestinationIdentifier
								{
									Host = _dnsNameRequested,
									IPAddress = _addressRequested,
									Port = _portRequested,
								});
								if (_bufferProcessedCount < _bufferReceivedCount)
								{
									// forward the rest of the buffer
									// TODO do not do this when proxy chain expected
									Trace.WriteLine("Streaming - forward the rest >> " + (_bufferReceivedCount - _bufferProcessedCount) + " bytes");
									SendForward(_buffer, _bufferProcessedCount, _bufferReceivedCount - _bufferProcessedCount);
								}
								_stream.BeginRead(_buffer, 0, _buffer.Length, ReceivedStreaming, null);
							}
							catch (Exception exx)
							{
								Trace.TraceError(exx.ToString());
								ex = exx;
							}
							var response = new byte[]
							{
									0x00, // null
									(ex == null ? (byte) 0x5A : (byte) 0x5B), // state = granted / rejected
									0x00, // port
									0x00, // port
									0x00, // address
									0x00, // address
									0x00, // address
									0x00, // address
							};
							_stream.Write(response, 0, response.Length);
							_stream.Flush();
							if (ex != null)
							{
								Dispose();
							}
							// NOW Stream is established for forwarding
						}
						break;
					case 5: // SOCKS 5
						if (EnsureReaded(2))
						{
							var authMethodsCount = _buffer[1];
							int requestStartedAt = 2 + authMethodsCount;
							if (EnsureReaded(2 + authMethodsCount))
							{
								if (!_authenticationNegotiated)
								{
									bool supportsNoAuth = false;
									for (int i = 0; i < authMethodsCount; i++)
									{
										if (_buffer[2 + i] == 0x00) // code of no auth
										{
											supportsNoAuth = true;
										}
									}
									_bufferProcessedCount = 2 + authMethodsCount;
									if (authMethodsCount == 0 || !supportsNoAuth) // also, when list is empty, assume NoAuth
									{
										throw new Exception("Client must support NoAuth mode");
									}
									// PROVIDE MY CONCLUSION
									_stream.Write(0x05, 0x00); // v5, APPROVED - NO AUTH
									_authenticationNegotiated = true;
								}
								// continue - wait for reques
								if (EnsureReaded(_bufferProcessedCount + 4))
								{
									if (_buffer[_bufferProcessedCount + 0] != 5)
									{
										throw new Exception("Request v5 expected");
									}
									if (_buffer[_bufferProcessedCount + 1] != 1)
									{
										throw new Exception("Only Stream Command supported");
									}
									// reserved byte 2 skipped
									var addressType = _buffer[_bufferProcessedCount + 3];
									_bufferProcessedCount += 4; // we just processed Ver,Cmd,Rsv,AdrType
									bool addressTypeProcessed = false;
									switch (addressType)
									{
										case 1: // IPv4
											if (EnsureReaded(_bufferProcessedCount + 4))
											{
												var ipv4 = new byte[4];
												Array.Copy(_buffer, _bufferProcessedCount, ipv4, 0, 4);
												_addressRequested = new IPAddress(ipv4);
												_bufferProcessedCount += 4;
												addressTypeProcessed = true;
											}
											break;
										case 3: // DNS
											if (EnsureReaded(_bufferProcessedCount + 1))
											{
												var len = _buffer[_bufferProcessedCount];
												if (EnsureReaded(_bufferProcessedCount + 1 + len))
												{
													_dnsNameRequested = _utf.GetString(_buffer, _bufferProcessedCount + 1, len);
													// 256 max, no need to check for overflow
												}
												_bufferProcessedCount += 1 + len;
												addressTypeProcessed = true;
											}
											break;
										case 4: // IPv6
											if (EnsureReaded(_bufferProcessedCount + 16))
											{
												var ipv6 = new byte[16];
												Array.Copy(_buffer, _bufferProcessedCount, ipv6, 0, 16);
												_addressRequested = new IPAddress(ipv6);
												_bufferProcessedCount += 16;
												addressTypeProcessed = true;
											}
											break;
									}
									if (addressTypeProcessed) // continue
									{
										if (EnsureReaded(_bufferProcessedCount + 2))
										{
											_portRequested = _buffer[_bufferProcessedCount] * 256 + _buffer[_bufferProcessedCount + 1];
											_bufferProcessedCount += 2;

											Exception ex = null;
											try
											{
												EstablishUpstream(new DestinationIdentifier
												{
													Host = _dnsNameRequested,
													IPAddress = _addressRequested,
													Port = _portRequested,
												});
												if (_bufferProcessedCount < _bufferReceivedCount)
												{
													// forward the rest of the buffer
													Trace.WriteLine("Streaming - forward the rest >> " + (_bufferReceivedCount - _bufferProcessedCount) + " bytes");
													SendForward(_buffer, _bufferProcessedCount, _bufferReceivedCount - _bufferProcessedCount);
												}
												_stream.BeginRead(_buffer, 0, _buffer.Length, ReceivedStreaming, null);
											}
											catch (Exception exx)
											{
												ex = exx;
											}
											// var response = new byte[_bufferProcessedCount - requestStartedAt];
											// Array.Copy(_buffer, requestStartedAt, response, 0, response.Length);
											// response[1] = (ex == null ? (byte)0x00 : (byte)0x01); // state = granted / rejected
											// do not expose multihoming or whatever
											// for (int i = 4; i < response.Length; i++)
											// {
											// 	response[i] = 0;
											// }

											// it appears, not all clients can handle domain name response... Hello to Telegram.
											// Let's go with IPv4
											var response = new byte[]
											{
													0x05, // ver
													(ex == null ? (byte) 0x00 : (byte) 0x01), // state = granted / rejected
													0x00, // null
													0x01, // adr_type ipv4
													0x00, // ipv4
													0x00, // ipv4
													0x00, // ipv4
													0x00, // ipv4
													0x00, // port
													0x00, // port
											};
											_stream.Write(response, 0, response.Length);
										}
									}
								}
							}
						}
						break;
					/*
				case (byte)'P': // HTTP PROXY PUT POST PATCH
				case (byte)'G': // HTTP PROXY GET
				case (byte)'D': // HTTP PROXY DELETE
				case (byte)'C': // HTTP PROXY CONNECT
				case (byte)'H': // HTTP PROXY HEAD
				case (byte)'T': // HTTP PROXY TRACE
				case (byte)'O': // HTTP PROXY OPTIONS
								// we must wait till entire heder comes
					int eoh;
					var headers = Utils.TryParseHttpHeader(_buffer, 0, _bufferReceivedCount, out eoh);
					if (headers != null)
					{
						string hostHeader;
						headers.TryGetValue("HOST", out hostHeader);
						string host;
						headers.TryGetValue("_url_host", out host);
						string port;
						headers.TryGetValue("_url_port", out port);

						int hostHeaderSplitter = hostHeader.IndexOf(':');
						string hostHeaderHost = hostHeaderSplitter > 0 ? hostHeader.Substring(0, hostHeaderSplitter) : hostHeader;
						string hostHeaderPort = hostHeaderSplitter > 0 ? hostHeader.Substring(hostHeaderSplitter + 1) : "80";

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
							EstablishForwardConnection();
							if (headers["_verb"] == "CONNECT")
							{
								_stream.Write(_utf.GetBytes("200 OK\r\n\r\n")); // ok to CONNECT
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
							_stream.BeginRead(_buffer, 0, _buffer.Length, ReceivedStreaming, null);
						}
						catch (Exception ex)
						{
							// write response
							Dispose();
						}
					}
					else
					{
						ReadMore();
					}
					break;
					*/
					default:
						throw new NotSupportedException("Socks Version not supported");
				}
			}
		}

		private void ReceivedStreaming(IAsyncResult ar)
		{
			if (_disposing)
			{
				return;
			}
			try
			{
				var count = _stream.EndRead(ar);
				Trace.WriteLine("Streaming - received from client >> " + count + " bytes");
				if (count > 0 && _client.Connected)
				{
					SendForward(_buffer, 0, count);
					_stream.BeginRead(_buffer, 0, _buffer.Length, ReceivedStreaming, null);
				}
				else
				{
					Dispose();
				}
			}
			catch (Exception ex)
			{
				Trace.TraceError("Streaming - received from client: " + ex);
				Dispose();
			}
		}

	}
}
