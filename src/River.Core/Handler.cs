using River.Common;
using River.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

		/// <summary>
		/// This offset can improve performance of HTTP header reshake / insert
		/// </summary>
		protected virtual int HandshakeStartPos { get => 0; }

		/*
		public void SwitchFrom(Handler handler, RiverServer server, ReStream stream)
		{
			if (handler is null)
			{
				throw new ArgumentNullException(nameof(handler));
			}

			if (server is null)
			{
				throw new ArgumentNullException(nameof(server));
			}

			if (stream is null)
			{
				throw new ArgumentNullException(nameof(stream));
			}

			// you must be sure that previous readers is readed and that is not subscribed yet!!
			// you must be sure there is no forward connection already established
			// you must be sure that previous handler did not sent any response to client yet!!

			// init
			stream.ResetReader();
			Init(server ?? handler.Server, handler.Client, stream);


			// copy fields
			// _buffer = handler._buffer;
			// _bufferReceivedCount = handler._bufferReceivedCount;

			// Client = handler.Client;
			// Server = server ?? handler.Server;
			// Stream = handler.Stream; // what about stream wrapper? like http wrap

			// re-handshake
			// HandshakeHandler();
		}
		*/

		public void Init(RiverServer server, TcpClient client, Stream stream = null)
		{
			Server = server ?? throw new ArgumentNullException(nameof(server));
			Client = client ?? throw new ArgumentNullException(nameof(client));

			// disable Nagle, carefully do write operations to prevent extra TCP transfers
			// efficient write should contain complete packet for corresponding protocol
			Client.Client.NoDelay = true;

			Stream = WrapStream(stream ?? Client.GetStream2());
			ReadMoreHandshake();
		}

		#region Dispose

		protected bool IsDisposed { get; private set; }
		protected bool IsResigned { get; set; }

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

				if (IsResigned) return;
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
			_targetReaderThread.JoinAbort();

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

				Trace.TraceError($"{Source} Handshake... {_bufferReceivedCount} bytes, first 0x{_buffer[HandshakeStartPos]:X2} {_utf8.GetString(_buffer, HandshakeStartPos, 1)} {Preview(_buffer, HandshakeStartPos, _bufferReceivedCount)}");
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
			Stream.BeginRead(_buffer
				, HandshakeStartPos + _bufferReceivedCount
				, _buffer.Length - _bufferReceivedCount - HandshakeStartPos
				, ReceivedHandshake
				, null);
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
			Profiling.Stamp("SourceReceived...");

			if (IsDisposed)
			{
				return;
			}
			try
			{
				var c = Stream.EndRead(ar);
				if (c > 0)
				{
					StatService.Instance.MaxBufferUsage(c, GetType().Name + " src");
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

			Profiling.Stamp("SourceReceived done");
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

		Thread _targetReaderThread;

		void TargetReaderThreadWorker()
		{
			var marker = new object();
			ObjectTracker.Default.Register(_targetReaderThread);
			ObjectTracker.Default.Register(marker);
			try
			{
				while (true)
				{
					var c = _upstreamClient.Read(_bufferTarget, 0, _buffer.Length);
					if (!TargetReceived(c))
					{
						break;
					}
				}
			}
			catch (IOException ex) when (ex.IsConnectionClosing())
			{
			}
			catch (Exception ex)
			{
				Trace.TraceError(ex.ToString());
			}
			Trace.WriteLine(marker + "");
		}

		protected void BeginReadTarget()
		{
			Profiling.Stamp("BeginReadTarget...");
			_targetReaderThread = new Thread(TargetReaderThreadWorker);
			_targetReaderThread.IsBackground = true;
			_targetReaderThread.Start();
			// _upstreamClient.BeginRead(_bufferTarget, 0, _buffer.Length, TargetReceived, null);
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
			Profiling.Stamp($"TargetReceived... from {_upstreamClient.GetType().Name}");

			if (IsDisposed)
			{
				return;
			}
			try
			{
				var c = _upstreamClient.EndRead(ar);
				if (TargetReceived(c))
				{
					_upstreamClient.BeginRead(_bufferTarget, 0, _buffer.Length, TargetReceived, null);
				}
			}
			catch (Exception ex)
			{
				Trace.TraceError("Streaming - received from client: " + ex);
				Dispose();
			}
			Profiling.Stamp("TargetReceived done");
		}

		private bool TargetReceived(int c)
		{
			if (IsDisposed)
			{
				return false;
			}

			if (c > 0)
			{
				StatService.Instance.MaxBufferUsage(c, GetType().Name + " trg");
				Trace.WriteLine($"{Source} <<< {c} bytes <<< {Destination} {Preview(_bufferTarget, 0, c)}");
				Stream.Write(_bufferTarget, 0, c);
				return true;
			}
			else
			{
				Dispose();
				return false;
			}

		}

		static Encoding _utf8 = new UTF8Encoding(false, false);

		private string Preview(byte[] buf, int pos, int cnt)
		{
			var lim = Math.Min(cnt, 32);
			var str = _utf8.GetChars(buf, pos, lim);
			for (int i = 0; i < str.Length; i++)
			{
				if (str[i] < 32) str[i] = '?';
			}
			return new string(str) + (lim < cnt ? "..." : "");
		}

		Stream _upstreamClient;
		DestinationIdentifier _target;



		protected void EstablishUpstream(DestinationIdentifier target)
		{
			Profiling.Stamp("EstablishUpstream...");

			try
			{
				if (target is null)
				{
					throw new ArgumentNullException(nameof(target));
				}

				_target = target;
				Trace.WriteLine($"{Source} Route to {Destination}");

				/*
				string ep;
				if (string.IsNullOrEmpty(target.Host)) {
					ep = target.IPEndPoint.ToString();
				}
				else
				{
					ep = target.Host + ":" + target.Port;
				}

				if (Server.Config.EndPoints.Any(x => x.ToString() == ep))
				{
					//Add random header for http handler - prevent loop for localhost:1080
					// redirrect self-loop to _river;
					ep = "_river" + ":" + target.Port;
				}
				*/

				var ov = Resolver.GetStreamOverride(target);
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
			Profiling.Stamp("Established");
		}

	}
}
