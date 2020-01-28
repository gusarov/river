using System;
using System.Net.Sockets;

namespace River
{
	/// <summary>
	/// Handles incomming connection for server.
	/// Created and owned by server.
	/// Handler - is a servant for client. Client is the boss.
	/// Handler must satisfy client as much as possible, compensate all client mistakes and be graceful with him.
	/// </summary>
	public abstract class Handler : IDisposable
	{
		protected TcpClient _client;
		protected NetworkStream _stream;
		protected byte[] _buffer = new byte[1024 * 16];
		protected int _bufferReceivedCount;
		private Server _server;

		public Handler(Server server, TcpClient client)
		{
			_server = server;
			_client = client;

			// disable Nagle, carefully do write operations to prevent extra TCP transfers
			// efficient write should contain complete packet for corresponding rotocol
			_client.Client.NoDelay = true;

			_stream = _client.GetStream();
			_stream.BeginRead(_buffer, _bufferReceivedCount, _buffer.Length, ReceivedHandshake, null);
		}

		protected bool _disposing;

		public void Dispose()
		{
			_disposing = true;
			var client = _client;
			var stream = _stream;
			try
			{
				// graceful tcp shutdown
				client?.Client?.Shutdown(SocketShutdown.Both);
			}
			catch { }
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

		void ReceivedHandshake(IAsyncResult ar)
		{
			try
			{
				int count;
				_bufferReceivedCount += count = _stream.EndRead(ar);

				if (count == 0 || !_client.Connected)
				{
					Dispose();
					return;
				}

				HandshakeHandler();

			}
			catch (Exception ex)
			{
				Trace.TraceError("Handshake: " + ex);
				Dispose();
			}
		}

		/// <summary>
		/// Update incomming headers. Can be called mupltiple times.
		/// To read headers - see the _buffer & _bufferReceivedCount
		/// To resubscribe - call EnsureReaded(n) and return on false
		/// or just ReadMoreHandshake() and return
		/// </summary>
		protected abstract void HandshakeHandler();

		/// <summary>
		/// Incremental header reading
		/// </summary>
		protected bool EnsureReaded(int readed)
		{
			if (_bufferReceivedCount < readed)
			{
				ReadMoreHandshake();
				return false;
			}
			return true;
		}

		protected void ReadMoreHandshake()
		{
			_stream.BeginRead(_buffer, _bufferReceivedCount, _buffer.Length - _bufferReceivedCount, ReceivedHandshake, null);
		}

		ForwardHandler _forwardHandler;

		protected void EstablishForwardConnection(DestinationIdentifier id)
		{
			_forwardHandler = _server.Forwarder.CreateForwardHandler();
			_forwardHandler.EstablishConnection(id);
		}

		protected void SendForward(byte[] buf, int pos, int cnt)
		{
			_forwardHandler.Send(buf, pos, cnt);
		}

	}
}
