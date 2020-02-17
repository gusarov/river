using River.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

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
			StatService.Instance.HandlerAdd(this);
			ObjectTracker.Default.Register(this);
		}

		public Handler(RiverServer server, TcpClient client)
			: this()
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

		protected bool IsDisposed { get; private set; }

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~Handler()
		{
			Dispose(false);
		}

		public override string ToString()
		{
			var b = base.ToString();
			return $"{b} {(IsDisposed ? "Disposed" : "NotDisposed")}";
		}

		protected virtual void Dispose(bool managed)
		{
			lock (this)
			{
				if (IsDisposed) return;
				IsDisposed = true;
			}

			StatService.Instance.HandlerRemove(this);

			Trace.WriteLine($"{Client?.GetHashCode():X4} Closing Handler...");


			// UPSTREAM
			try
			{
				_upstreamClient?.Dispose();
				_upstreamClient?.Close();
				_upstreamClient = null;
			}
			catch { }

			// SOURCE
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
				// Client = null;
			}
			catch { }
			try
			{
				stream?.Close();
				// Stream = null;
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

				Trace.TraceError($"{Source} Handshake... {_buffer[0]:X2} {_utf8.GetString(_buffer, 0, 1)}");
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
			Stream.BeginRead(_buffer, 0, _buffer.Length, SourceReceived, null);
		}

		void SourceReceived(IAsyncResult ar)
		{
			if (IsDisposed)
			{
				return;
			}
			try
			{
				var c = Stream.EndRead(ar);
				if (c > 0)
				{
					_upstreamClient.Write(_buffer, 0, c);
					Stream.BeginRead(_buffer, 0, _buffer.Length, SourceReceived, null);
				}
				else
				{
					Dispose();
				}
			}
			catch (Exception ex)
			{
				if (!ex.IsConnectionClosing())
				{
					Trace.TraceError("Streaming - received from client: " + ex);
				}
				Dispose();
			}
		}

		protected void SendForward(byte[] buf, int pos = 0, int cnt = -1)
		{
			if (buf is null)
			{
				throw new ArgumentNullException(nameof(buf));
			}

			if (cnt == -1)
			{
				cnt = buf.Length;
			}

			Trace.WriteLine($"{Source} >>> send {cnt} bytes >>> {Destination} {Preview(buf, pos, cnt)}");
			_upstreamClient.Write(buf, pos, cnt);
		}

		protected void BeginReadTarget()
		{
			_upstreamClient.BeginRead(_bufferTarget, 0, _buffer.Length, TargetReceived, null);
		}

		string Source
		{
			get
			{
				return $"{Client.GetHashCode():X4} {Client.Client.RemoteEndPoint}";
			}
		}

		string Destination
		{
			get
			{
				if (_target != null)
				{
					return $"{_target.Host}{_target.IPAddress}:{_target.Port}";
				}
				if (_upstreamClient is ClientStream cs)
				{
					return cs?.Client?.Client?.RemoteEndPoint?.ToString();
				}
				return null;
			}
		}

		private void TargetReceived(IAsyncResult ar)
		{
			if (IsDisposed)
			{
				return;
			}
			try
			{
				var c = _upstreamClient.EndRead(ar);
				if (c > 0)
				{
					Trace.WriteLine($"{Source} <<< {c} bytes <<< {Destination} {Preview(_bufferTarget, 0, c)}");
					Stream.Write(_bufferTarget, 0, c);
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

		static Encoding _utf8 = new UTF8Encoding(false, false);

		private string Preview(byte[] buf, int pos, int cnt)
		{
			var lim = Math.Min(cnt, 32);
			return _utf8.GetString(buf, pos, lim) + (lim < cnt ? "..." : "");
		}

		Stream _upstreamClient;
		DestinationIdentifier _target;



		protected void EstablishUpstream(DestinationIdentifier target)
		{
			var list = new List<IDisposable>();
			try
			{
				if (target is null)
				{
					throw new ArgumentNullException(nameof(target));
				}

				_target = target;
				Trace.WriteLine($"{Source} Route to {Destination}");

				var ov = Resolver.GetStreamOverride(target.Host);
				if (ov != null)
				{
					_upstreamClient = ov;
				}
				else
				{
					foreach (var proxy in Server.Chain)
					{
						var clientType = Resolver.GetClientType(proxy.Uri);
						var clientStream = (ClientStream)Activator.CreateInstance(clientType);
						list.Add(clientStream);
						if (_upstreamClient == null)
						{
							// create a first client connection
							clientStream.Plug(proxy.Uri);
						}
						else
						{
							// route in old client
							((ClientStream)_upstreamClient).Route(proxy.Uri.Host, proxy.Uri.Port);

							// and now wrap to new one
							clientStream.Plug(proxy.Uri, _upstreamClient);
						}
						_upstreamClient = clientStream;
					}
					if (_upstreamClient != null)
					{
						var client = (ClientStream)_upstreamClient;
						client.Route(target.Host ?? target.IPAddress.ToString(), target.Port);
					}
					else
					{
						// dirrect connection
						_upstreamClient = new NullClientStream();
						list.Add(_upstreamClient);
						var host = target.Host ?? target.IPAddress.ToString();
						var port = target.Port;
						((ClientStream)_upstreamClient).Plug(host, port);
					}
				}
			}
			catch
			{
				Dispose();
				throw;
			}
		}

	}
}
