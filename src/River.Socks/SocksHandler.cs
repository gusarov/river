using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace River.Socks
{
	public class SocksHandler : Handler
	{
		static readonly Encoding _utf = new UTF8Encoding(false, false);

		#region Preallocated

		const int _staticSocsk4Granted = 0;
		const int _staticSocsk4Rejected = 8;
		const int _staticSocsk5Approved = 16;
		static (int from, int cnt, int resp) _staticSocsk5Rejected = (18, 10, 1);
		static byte[] _static = new byte[]
		{
			// Socks4 Response 1
			/* 00 */ 0x00, // null
			/* 01 */ 0x5A, // state = granted
			/* 02 */ 0x00, // port
			/* 03 */ 0x00, // port
			/* 04 */ 0x00, // address
			/* 05 */ 0x00, // address
			/* 06 */ 0x00, // address
			/* 07 */ 0x00, // address

			// Socks4 Response 2
			/* 08 */ 0x00, // null
			/* 09 */ 0x5B, // state = rejected
			/* 10 */ 0x00, // port
			/* 11 */ 0x00, // port
			/* 12 */ 0x00, // address
			/* 13 */ 0x00, // address
			/* 14 */ 0x00, // address
			/* 15 */ 0x00, // address

			// Socks5 Approved - No Auth
			/* 16 */ 0x05, // V5
			/* 17 */ 0x00, // NoAuth

			// Socks5 Response Rejected
			/* 18 */ 0x05, // V5
			/* 19 */ 0x01, // state = granted
			/* 20 */ 0x00, // reserved
			/* 21 */ 0x01, // adr_type ipv4
			/* 22 */ 0x00, // ipv4 1
			/* 23 */ 0x00, // ipv4 2
			/* 24 */ 0x00, // ipv4 3
			/* 25 */ 0x00, // ipv4 4
			/* 26 */ 0x00, // port H
			/* 27 */ 0x00, // port L
		};

		byte[] _preallocated4 = new byte[4];
		byte[] _preallocated16 = new byte[16];

		#endregion

		/*
		private new SocksServer _server => (SocksServer)base._server;

		public SocksHandler()
		{

		}

		public SocksHandler(SocksServer server, TcpClient client)
			: base(server, client)
		{
			_server = server;
		}
		*/

		IPAddress _addressRequested;
		string _dnsNameRequested;
		int _portRequested;
		int _bufferProcessedCount;
		private bool _authenticationNegotiated;

		/// <summary>
		/// This offset can improve performance of HTTP header reshake / insert
		/// </summary>
		// protected override int HandshakeStartPos => 128;
		protected override void HandshakeHandler()
		{
			Profiling.Stamp("HandshakeHandler...");
			// get request from client
			if (EnsureReaded(1))
			{
				//var msg = $"{_bufferReceivedCount} bytes received on thread #{Thread.CurrentThread.ManagedThreadId} port {Client.Client.RemoteEndPoint}";
				// Trace.WriteLine($"Negotiating - v{_buffer[0]} received from client {_bufferReceivedCount} bytes on thread #{Thread.CurrentThread.ManagedThreadId} port {Client.Client.RemoteEndPoint}");

				switch (_buffer[HandshakeStartPos]) // 0
				{
					case 4: // SOCKS4
						#region SOCKS 4
						if (EnsureReaded(8))
						{
							var b = HandshakeStartPos + 1;
							if (_buffer[b++] != 1) // 1
							{
								throw new NotSupportedException("command type not supported");
							}
							_portRequested = (_buffer[b++] << 8) + _buffer[b++]; // 2 & 3
							_addressRequested = null;
							if (_buffer[b] != 0) // #4 - 0 means v4a mode (0.0.0.X)
							{
								// var bufAddress4 = _preallocated4;
								// Array.Copy(_buffer, 4, bufAddress4, 0, 4);
								// var a = ((_buffer[b + 3] << 24 | _buffer[b + 2] << 16 | _buffer[b + 1] << 8 | _buffer[b]) & 4294967295u);
								var a = ((_buffer[b++] | _buffer[b++] << 8 | _buffer[b++] << 16 | _buffer[b++] << 24) & uint.MaxValue);
								_addressRequested = new IPAddress(a);
							}
							else
							{
								b += 4; // ignore
							}
							// read user id, throw it away and look for null
							Debug.Assert(b == 8 + HandshakeStartPos);
							bool nullOk = false;
							for (int i = 0; i < 255; i++)
							{
								// EnsureReaded(b);
								if (_buffer[b++] == 0)
								{
									// _bufferProcessedCount = 8 + i;
									// b += i + 1;
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
								int dnsBegin = b;
								for (int i = 0; i < 255; i++)
								{
									// EnsureReaded(b);
									if (_buffer[b++] == 0)
									{
										dnsName = _utf.GetString(_buffer, dnsBegin, i);
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
#if DEBUG
								if (_addressRequested == null)
								{
									Trace.WriteLine($"Socks v4a Route: {_dnsNameRequested}:{_portRequested}");
								}
								else
								{
									Trace.WriteLine($"Socks v4 Route: {_addressRequested}:{_portRequested}");
								}
#endif
								EstablishUpstream(new DestinationIdentifier
								{
									Host = _dnsNameRequested,
									IPAddress = _addressRequested,
									Port = _portRequested,
								});
								if (b < _bufferReceivedCount)
								{
									// forward the rest of the buffer
									// TODO do not do this when proxy chain expected
									Trace.WriteLine("Streaming - forward the rest >> " + (_bufferReceivedCount - b) + " bytes");
									SendForward(_buffer, b, _bufferReceivedCount - b);
								}
								// _stream.BeginRead(_buffer, 0, _buffer.Length, ReceivedStreaming, null);
							}
							catch (Exception exx)
							{
								Trace.TraceError(exx.ToString());
								ex = exx;
							}

							Profiling.Stamp("Handshake response...");
							var response = ex == null
								? _staticSocsk4Granted
								: _staticSocsk4Rejected;
							Stream.Write(_static, response, 8);
							Stream.Flush();
							Profiling.Stamp("Handshake streaming...");
							if (ex != null)
							{
								Dispose();
							}
							else
							{
								BeginStreaming();
							}
							// NOW Stream is established for forwarding
						}
						#endregion
						break;
					case 5: // SOCKS 5
						#region SOCKS 5
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
									if (authMethodsCount == 0 || !supportsNoAuth) // also, when list is empty, assume NoAuth
									{
										throw new Exception("Client must support NoAuth mode");
									}
									// Remember that we passed this stage
									_bufferProcessedCount = 2 + authMethodsCount;
									_authenticationNegotiated = true;
									// PROVIDE MY CONCLUSION
									Stream.Write(_static, _staticSocsk5Approved, 2); // v5, APPROVED - NO AUTH
								}
								// continue - wait for request
								if (EnsureReaded(_bufferProcessedCount + 4))
								{
									var b = _bufferProcessedCount;

									if (_buffer[b++] != 5)
									{
										throw new Exception("Request v5 expected");
									}
									if (_buffer[b++] != 1)
									{
										throw new Exception("Only Stream Command supported");
									}
									// reserved byte #2 skipped
									b++;
									var addressType = _buffer[b++];
									// _bufferProcessedCount += 4; // we just processed Ver,Cmd,Rsv,AdrType
									bool addressTypeProcessed = false;
									switch (addressType)
									{
										case 1: // IPv4
											if (EnsureReaded(b + 4))
											{
												var a = ((_buffer[b++] | _buffer[b++] << 8 | _buffer[b++] << 16 | _buffer[b++] << 24) & uint.MaxValue);
												_addressRequested = new IPAddress(a);
												/*
												var ipv4 = new byte[4];
												Array.Copy(_buffer, b, ipv4, 0, 4);
												_addressRequested = new IPAddress(ipv4);
												b += 4;
												*/
												addressTypeProcessed = true;
											}
											break;
										case 3: // DNS
											if (EnsureReaded(b + 1))
											{
												var len = _buffer[b++];
												if (EnsureReaded(b + len))
												{
													_dnsNameRequested = _utf.GetString(_buffer, b, len);
													// 256 max, no need to check for overflow
													b += len;
													addressTypeProcessed = true;
												}
											}
											break;
										case 4: // IPv6
											if (EnsureReaded(b + 16))
											{
												// var ipv6 = new byte[16];
												Array.Copy(_buffer, b, _preallocated16, 0, 16);
												_addressRequested = new IPAddress(_preallocated16);
												b += 16;
												addressTypeProcessed = true;
											}
											break;
									}
									if (addressTypeProcessed) // continue
									{
										if (EnsureReaded(b + 2))
										{
											_portRequested = _buffer[b++] * 256 + _buffer[b++];

#if DEBUG
											string adrMsg;
											switch (addressType)
											{
												case 3:
													adrMsg = _dnsNameRequested;
													break;
												default:
													adrMsg = _addressRequested.ToString();
													break;
											}
											Trace.WriteLine($"Socks v5 Route: A{addressType} {adrMsg}:{_portRequested}");
#endif

											Exception ex = null;
											try
											{
												EstablishUpstream(new DestinationIdentifier
												{
													Host = _dnsNameRequested,
													IPAddress = _addressRequested,
													Port = _portRequested,
												});
												if (b < _bufferReceivedCount)
												{
													// forward the rest of the buffer
													Trace.WriteLine("Streaming - forward the rest >> " + (_bufferReceivedCount - _bufferProcessedCount) + " bytes");
													SendForward(_buffer, _bufferProcessedCount, _bufferReceivedCount - _bufferProcessedCount);
												}
												// _stream.BeginRead(_buffer, 0, _buffer.Length, ReceivedStreaming, null);
												BeginReadSource();
												BeginReadTarget();
											}
											catch (Exception exx)
											{
												ex = exx;
											}

											// it appears, not all clients can handle domain name response...
											// Hello to Telegram & QT platform.
											// Let's go with IPv4
											Array.Copy(_static, _staticSocsk5Rejected.from
												, _preallocated16, 0, _staticSocsk5Rejected.cnt);
											_preallocated16[_staticSocsk5Rejected.resp] = ex != null ? (byte)0x01 : (byte)0x00;
											if (!IsDisposed)
											{
												Stream.Write(_preallocated16, 0, _staticSocsk5Rejected.cnt);
											}
										}
									}
								}
							}
						}
						#endregion
						break;
				case (byte)'P': // HTTP PROXY PUT POST PATCH
				case (byte)'G': // HTTP PROXY GET
				case (byte)'D': // HTTP PROXY DELETE
				case (byte)'C': // HTTP PROXY CONNECT
				case (byte)'H': // HTTP PROXY HEAD
				case (byte)'T': // HTTP PROXY TRACE
				case (byte)'O': // HTTP PROXY OPTIONS
						#region HTTP

						// we must wait till entire heder comes
						int eoh;
						var headers = HttpUtils.TryParseHttpHeader(_buffer, HandshakeStartPos, _bufferReceivedCount, out eoh);
						if (headers != null)
						{
							headers.TryGetValue("HOST", out var hostHeader);
							headers.TryGetValue("_url_host", out var host);
							headers.TryGetValue("_url_port", out var port);

							if (string.IsNullOrEmpty(hostHeader))
							{
								_portRequested = string.IsNullOrEmpty(port)
										? 80
										: int.Parse(port, CultureInfo.InvariantCulture);
								_dnsNameRequested = host;
							}
							else
							{
								var hostHeaderSplitter = hostHeader.LastIndexOf(':');
								var hostHeaderHost = hostHeaderSplitter > 0 ? hostHeader.Substring(0, hostHeaderSplitter) : hostHeader;
								var hostHeaderPort = hostHeaderSplitter > 0 ? hostHeader.Substring(hostHeaderSplitter + 1) : "80";

								_portRequested = int.Parse(hostHeaderPort, CultureInfo.InvariantCulture);
								_dnsNameRequested = hostHeaderHost;
							}

							try
							{
								bool extraHeaderExists = false;
								if (headers.ContainsKey(_randomHeader.Value))
								{
									extraHeaderExists = true;
									_dnsNameRequested = "_river"; // stop loop, goto self-service
								}
								EstablishUpstream(new DestinationIdentifier
								{
									Host = _dnsNameRequested,
									Port = _portRequested,
								});

								if (headers["_verb"] == "CONNECT")
								{
									Stream.Write(_utf.GetBytes("HTTP/1.1 200 OK\r\n\r\n")); // ok to CONNECT
																							// for connect - forward the rest of the buffer
									if (_bufferReceivedCount - eoh > 0)
									{
										Trace.WriteLine("Streaming - forward the rest >> " + (_bufferReceivedCount - eoh) + " bytes");
										SendForward(_buffer, eoh, _bufferReceivedCount - eoh);
									}
								}
								else
								{
									// when buffer equal to EOH we can append extra in no time
									// otherwise still must try to append in 2% o times
									if (!extraHeaderExists && (_bufferReceivedCount == eoh || Interlocked.Increment(ref _requestNumber) % 50 == 0))
									{
										// let's add a header to handle proxy loop
										// if there is a loop with body - it will stop after 50 iterations
										var extraHeader = _randomHeaderLine.Value;
										var eohp = HandshakeStartPos + eoh;

										if (_bufferReceivedCount > eoh)
										{
											// Shift Array
											Array.Copy(_buffer, eohp, _buffer, eohp + extraHeader.Length, _bufferReceivedCount - eoh);
										}

										var c = _utf.GetBytes(extraHeader+"\r\n", 0, extraHeader.Length+2, _buffer, eohp - 2);
										_bufferReceivedCount += c - 2;
										if (c != extraHeader.Length + 2)
										{
											throw new Exception("Encoding - length missmatch");
										}
									}
									// otherwise forward entire buffer without change
									SendForward(_buffer, HandshakeStartPos, _bufferReceivedCount);
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
						#endregion
						break;
					default:
						throw new NotSupportedException($"Protocol not supported. First byte is {_buffer[HandshakeStartPos]:X2} {_utf.GetString(_buffer, HandshakeStartPos, 1)}");
				}
			}
		}

		static Random _rnd = new Random();
		static int _requestNumber;
		static Lazy<string> _randomHeader = new Lazy<string>(() => RandomName);
		static Lazy<string> _randomHeaderLine = new Lazy<string>(()=>$"{_randomHeader.Value}: {RandomName}\r\n");

		static string RandomName
		{
			get
			{
				var c = _rnd.Next(5) + 5;
				var s = "";
				for (var i = 0; i < c; i++)
				{
					s += (char)(_rnd.Next('z' - 'a') + 'a');
				}
				return s;
			}
		}

	}
}
