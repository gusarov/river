using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace River
{
	public static partial class TcpClientExt
	{
		private class NetworkStream2 : NetworkStream
		{
			public override string ToString()
			{
				try
				{
					return _socket?.RemoteEndPoint.ToString() + " " +_socket.Connected;
				}
				catch (Exception ex)
				{
					return ex.GetType().Name;
				}
			}

			const SocketFlags MSG_PUSH_IMMEDIATE = (SocketFlags)0x20;

			private readonly Socket _socket;
			private readonly TcpClient _tcpClient;

			internal NetworkStream2(TcpClient client, bool ownSocket) : base(client.Client, ownSocket)
			{
				ObjectTracker.Default.Register(this);
				_tcpClient = client;
				_socket = client.Client;
			}

			public override IAsyncResult BeginRead(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
			{
				// copied from v4.0.30319\mscorlib.dll

				/*
				if (this.m_CleanedUp)
				{
					throw new ObjectDisposedException(base.GetType().FullName);
				}
				*/
				if (!CanRead)
				{
					throw new InvalidOperationException("net_writeonlystream");
				}
				if (buffer == null)
				{
					throw new ArgumentNullException(nameof(buffer));
				}
				if (offset >= 0 && offset <= buffer.Length)
				{
					if (size >= 0 && size <= buffer.Length - offset)
					{
						var streamSocket = _socket;
						if (streamSocket == null)
						{
							throw new IOException("net_io_readfailure: net_io_connectionclosed");
						}
						try
						{
							// MSG_PUSH_IMMEDIATE HERE!!!
							Profiling.Stamp(TraceCategory.NetworkingData, "Socket BeginReceive...");
							return streamSocket.BeginReceive(buffer, offset, size, MSG_PUSH_IMMEDIATE, callback, state);
						}
						catch (Exception ex)
						{
							if (!(ex is ThreadAbortException)
								&& !(ex is StackOverflowException)
								&& !(ex is OutOfMemoryException))
							{
								throw new IOException("net_io_readfailure: " + ex.Message, ex);
							}
							throw;
						}
					}
					throw new ArgumentOutOfRangeException(nameof(size));
				}
				throw new ArgumentOutOfRangeException(nameof(offset));
			}

			public override void Close()
			{
				try
				{
					if (_socket.Connected)
					{
						_socket.Shutdown(SocketShutdown.Both);
					}
				}
				catch { }
				try
				{
					_tcpClient?.Close();
				}
				catch { }
				base.Close();
			}
		}
	}

}
