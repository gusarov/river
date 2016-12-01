using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace River
{
	public class SocksServer : IDisposable
	{
		private static readonly Encoding _utf = new UTF8Encoding(false, false);
		private TcpListener _listener;

		public void Listen(int port)
		{
			Trace.WriteLine("SocksServer created at " + Thread.CurrentThread.ManagedThreadId);
			_listener = new TcpListener(IPAddress.Any, port);
			_listener.Start();
			_listener.BeginAcceptTcpClient(NewTcpClient, null);
		}


		private void NewTcpClient(IAsyncResult ar)
		{
			Trace.WriteLine("NewTcpClient called back at " + Thread.CurrentThread.ManagedThreadId);
			var client = _listener.EndAcceptTcpClient(ar);
			new ClientWorker(client);
			_listener.BeginAcceptTcpClient(NewTcpClient, null);
		}

		public void Dispose()
		{
			var listener = _listener;
			listener?.Stop();
		}

		class ClientWorker : IDisposable
		{
			private TcpClient _client;
			private TcpClient _clientForward;
			private NetworkStream _stream;
			private NetworkStream _streamForward;
			private byte[] _buffer = new byte[1024 * 32];
			private byte[] _bufferForwardRead = new byte[1024 * 32];
			private int _bufferReceivedCount;
			private int _bufferProcessedCount;

			public void Dispose()
			{
				//return;
				var clientForward = _clientForward;
				var client = _client;
				var stream = _stream;
				var streamForward = _streamForward;
				try
				{
					client?.Close();
					_client = null;
				}
				catch { }
				try
				{
					stream?.Close();
					_client = null;
				}
				catch { }
				try
				{
					clientForward?.Close();
					_client = null;
				}
				catch { }
				try
				{
					streamForward?.Close();
					_streamForward = null;
				}
				catch { }
			}

			public ClientWorker(TcpClient client)
			{
				_client = client;
				_stream = _client.GetStream();
				_stream.BeginRead(_buffer, _bufferReceivedCount, _buffer.Length, ReceivedHandshake, null);
			}

			/// <summary>
			/// This methods called asynchroniously whenewer new data received from client. To comtinie communication, this method should call BeginRead again
			/// </summary>
			private void ReceivedHandshake(IAsyncResult ar)
			{
				int count;
				_bufferReceivedCount += count = _stream.EndRead(ar);
				Trace.WriteLine("Negotiating - received from client " + count + " bytes on thread #" + Thread.CurrentThread.ManagedThreadId + " port " + _client.Client.RemoteEndPoint);
				// get request from client
				if (EnsureReaded(1))
				{
					switch (_buffer[0])
					{
						case 4:
							if (EnsureReaded(8))
							{
								if (_buffer[1] != 1)
								{
									throw new NotSupportedException("command type not supported");
								}
								int port = _buffer[2]*256 + _buffer[3];
								var bufAddress4 = new byte[4];
								Array.Copy(_buffer, 4, bufAddress4, 0, 4);
								var address4 = new IPAddress(bufAddress4);
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
								if (bufAddress4[0] == 0) // ver 4a mode - read dns name
								{
									// read dns name
									string dnsName = null;
									nullOk = false;
									for (int i = 1; i < 256; i++)
									{
										EnsureReaded(_bufferProcessedCount + i);
										if (_buffer[_bufferProcessedCount + i - 1] == 0)
										{
											nullOk = true;
											dnsName = _utf.GetString(_buffer, _bufferProcessedCount, i - 1);
											_bufferProcessedCount += i;
											break;
										}
									}
									if (!string.IsNullOrWhiteSpace(dnsName))
									{
										address4 = Dns.GetHostAddresses(dnsName).FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
									}
									else
									{
										throw new Exception("Host name not provided");
									}
								}
								if (address4 == null)
								{
									throw new Exception();
								}
								Exception ex = null;
								try
								{
									_clientForward = new TcpClient();
									_clientForward.Connect(address4, port);
									_streamForward = _clientForward.GetStream();
									if (_bufferProcessedCount < _bufferReceivedCount)
									{
										// forward the rest of the buffer
										_streamForward.Write(_buffer, _bufferProcessedCount, _bufferReceivedCount - _bufferProcessedCount);
									}
									_streamForward.BeginRead(_bufferForwardRead, 0, _bufferForwardRead.Length, ReceivedFromForwarder, null);
									_stream.BeginRead(_buffer, 0, _buffer.Length, ReceivedStreaming, null);
								}
								catch (Exception exx)
								{
									ex = exx;
								}
								var response = new byte[]
								{
									0x00, // null
									(ex == null ? (byte)0x5A : (byte)0x5B), // state = granted / rejected
									0x00, // port
									0x00, // port
									0x00, // address
									0x00, // address
									0x00, // address
									0x00, // address
								};
								_stream.Write(response, 0, response.Length);
							}
							break;
						case 5:
							break;
						default:
							throw new NotSupportedException("Socks Version not supported");
					}
				}
			}

			private void ReceivedStreaming(IAsyncResult ar)
			{
				try
				{
					var count = _stream.EndRead(ar);
					Trace.WriteLine("Streaming - received from client " + count + " bytes");
					if (count > 0)
					{
						_streamForward.Write(_buffer, 0, count);
						_stream.BeginRead(_buffer, 0, _buffer.Length, ReceivedStreaming, null);
					}
					else
					{
						Dispose();
					}
				}
				catch(Exception ex)
				{
					Trace.TraceError("Streaming - received from client: " + ex);
					Dispose();
				}
			}

			private void ReceivedFromForwarder(IAsyncResult ar)
			{
				try
				{
					if (_streamForward == null)
					{
						return;
					}
					var count = _streamForward.EndRead(ar);
					Trace.WriteLine("Streaming - received from forward stream " + count + " bytes on thread #" + Thread.CurrentThread.ManagedThreadId);

					// write back to socks client
					if (count != 0)
					{
						_stream.Write(_bufferForwardRead, 0, count);
						// continue async
						_streamForward.BeginRead(_bufferForwardRead, 0, _bufferForwardRead.Length, ReceivedFromForwarder, null);
					}
					else
					{
						Dispose();
					}
				}
				catch (Exception ex)
				{
					Trace.TraceError("Streaming - received from forwarder client: " + ex);
					Dispose();
				}
			}

			/// <summary>
			/// Incremental header reading
			/// </summary>
			private bool EnsureReaded(int readed)
			{
				if (_bufferReceivedCount < readed)
				{
					_stream.BeginRead(_buffer, _bufferReceivedCount, _buffer.Length - _bufferReceivedCount, ReceivedHandshake, null);
					return false;
				}
				return true;
			}
		}

	}
}
