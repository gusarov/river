using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace River
{
	public abstract class SocksServerClientWorker : IDisposable
	{
		private static readonly Encoding _utf = new UTF8Encoding(false, false);

		private TcpClient _client;
		protected NetworkStream _stream;
		private byte[] _buffer = new byte[1024 * 32];
		private int _bufferReceivedCount;
		private int _bufferProcessedCount;
		protected IPAddress[] _addressesRequested;
		protected string _dnsNameRequested;
		protected int _portRequested;

		public virtual void Dispose()
		{
			var client = _client;
			var stream = _stream;
			try
			{
				client?.Close();
				_client = null;
			}
			catch { }
			try
			{
				stream?.Close();
				_stream = null;
			}
			catch { }
		}

		public SocksServerClientWorker(TcpClient client)
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
			Trace.WriteLine("Negotiating - received from client " + count + " bytes on thread #" +
			                Thread.CurrentThread.ManagedThreadId + " port " + _client.Client.RemoteEndPoint);
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
							_portRequested = _buffer[2]*256 + _buffer[3];
							var bufAddress4 = new byte[4];
							Array.Copy(_buffer, 4, bufAddress4, 0, 4);
							_addressesRequested = bufAddress4[0] == 0 ? null : new[] {new IPAddress(bufAddress4)};
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
								EstablishForwardConnection();
								if (_bufferProcessedCount < _bufferReceivedCount)
								{
									// forward the rest of the buffer
									SendForward(_buffer, _bufferProcessedCount, _bufferReceivedCount - _bufferProcessedCount);
								}
								_client.Client.NoDelay = true;
								_stream.BeginRead(_buffer, 0, _buffer.Length, ReceivedStreaming, null);
							}
							catch (Exception exx)
							{
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
						}
						break;
					case 5:
						break;
					default:
						throw new NotSupportedException("Socks Version not supported");
				}
			}
		}

		protected abstract void EstablishForwardConnection();

		protected abstract void SendForward(byte[] buffer, int pos, int count);

		private void ReceivedStreaming(IAsyncResult ar)
		{
			if (_stream == null)
			{
				return;
			}
			try
			{
				var count = _stream.EndRead(ar);
				Trace.WriteLine("Streaming - received from client " + count + " bytes");
				if (count > 0)
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