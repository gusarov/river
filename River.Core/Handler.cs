using River.Internal;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

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
		protected Stream Stream { get; private set; }

		protected byte[] _buffer = new byte[16 * 1024];
		protected int _bufferReceivedCount;

		protected byte[] _bufferTarget = new byte[16 * 1024];

		protected RiverServer Server { get; private set; }
		protected TcpClient Client { get; private set; }

		public Handler()
		{

		}

		public Handler(RiverServer server, TcpClient client)
		{
			Init(server, client);
		}

		protected virtual Stream WrapStream(Stream stream)
		{
			return stream;
		}

		public void Init(RiverServer server, TcpClient client)
		{
			Server = server ?? throw new ArgumentNullException(nameof(server));
			Client = client ?? throw new ArgumentNullException(nameof(client));

			// disable Nagle, carefully do write operations to prevent extra TCP transfers
			// efficient write should contain complete packet for corresponding rotocol
			Client.Client.NoDelay = true;

			Stream = WrapStream(Client.GetStream());
			Stream.BeginRead(_buffer, _bufferReceivedCount, _buffer.Length, ReceivedHandshake, null);
		}

		#region Dispose

		protected bool Disposing { get; private set; }

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~Handler()
		{
			Dispose(false);
		}

		protected virtual void Dispose(bool managed)
		{
			Disposing = true;
			var client = Client;
			var stream = Stream;
			try
			{
				// graceful tcp shutdown
				client?.Client?.Shutdown(SocketShutdown.Both);
			}
			catch { }
			try
			{
				client?.Close();
				Client = null;
			}
			catch { }
			try
			{
				stream?.Close();
				Stream = null;
			}
			catch { }
		}

		#endregion

		void ReceivedHandshake(IAsyncResult ar)
		{
			try
			{
				int count;
				_bufferReceivedCount += count = Stream.EndRead(ar);

				if (count == 0 || !Client.Connected)
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
			Stream.BeginRead(_buffer, _bufferReceivedCount, _buffer.Length - _bufferReceivedCount, ReceivedHandshake, null);
		}

		protected void BeginStreaming()
		{
			BeginReadSource();
			BeginReadTarget();
		}

		protected void BeginReadSource()
		{
			Stream.BeginRead(_buffer, 0, _buffer.Length, Received, null);
		}

		void Received(IAsyncResult ar)
		{
			if (Disposing)
			{
				return;
			}
			try
			{
				var c = Stream.EndRead(ar);
				_upstreamClient.Write(_buffer, 0, c);
				if (c > 0)
				{
					Stream.BeginRead(_buffer, 0, _buffer.Length, Received, null);
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

		protected void SendForward(byte[] buf, int pos, int cnt)
		{
			_upstreamClient.Write(buf, pos, cnt);
		}

		protected void BeginReadTarget()
		{
			_upstreamClient.BeginRead(_bufferTarget, 0, _buffer.Length, TargetReceived, null);
		}

		private void TargetReceived(IAsyncResult ar)
		{
			if (Disposing)
			{
				return;
			}
			try
			{
				var c = _upstreamClient.EndRead(ar);
				Stream.Write(_bufferTarget, 0, c);
				if (c > 0)
				{
					_upstreamClient.BeginRead(_bufferTarget, 0, _buffer.Length, TargetReceived, null);
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

		ClientStream _upstreamClient;

		protected void EstablishUpstream(DestinationIdentifier target)
		{
			if (target is null)
			{
				throw new ArgumentNullException(nameof(target));
			}

			foreach (var proxy in Server.Chain)
			{
				var clientType = Resolver.GetClientType(proxy.Uri);
				var clientStream = (ClientStream)Activator.CreateInstance(clientType);
				if (_upstreamClient == null)
				{
					// create a first client connection
					clientStream.Plug(proxy.Uri);
				}
				else
				{
					// route in old client
					_upstreamClient.Route(proxy.Uri.Host, proxy.Uri.Port);

					// and now wrap to new one
					clientStream.Plug(proxy.Uri, _upstreamClient);
				}
				_upstreamClient = clientStream;
			}
			if (_upstreamClient != null)
			{
				_upstreamClient.Route(target.Host ?? target.IPAddress.ToString(), target.Port);
			}
			else
			{
				// dirrect connection
				_upstreamClient = new NullClientStream();
				var host = target.Host ?? target.IPAddress.ToString();
				var port = target.Port;
				var uri = new Uri($"{host}:{port}");
				_upstreamClient.Plug(uri);
			}

			// BeginReadSource();
			// BeginReadTarget();
		}

	}
}
