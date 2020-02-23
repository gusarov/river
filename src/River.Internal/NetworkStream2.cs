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
			const SocketFlags MSG_PUSH_IMMEDIATE = (SocketFlags)0x20;

			private readonly Socket _socket;

			internal NetworkStream2(Socket socket, bool ownSocket) : base(socket, ownSocket)
			{
				ObjectTracker.Default.Register(this);
				_socket = socket;
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
							Profiling.Stamp("Socket BeginReceive...");
							return streamSocket.BeginReceive(buffer, offset, size, MSG_PUSH_IMMEDIATE, callback, state);
						}
						catch (Exception ex)
						{
							if (!(ex is ThreadAbortException) && !(ex is StackOverflowException) && !(ex is OutOfMemoryException))
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
		}
	}

}
