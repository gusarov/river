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
		protected static Trace Trace = River.Trace.Default;

#if DEBUG
		static Encoding _utf8 = new UTF8Encoding(false, false);
#endif

		protected Stream Stream { get; private set; }
		protected byte[] _buffer = new byte[16 * 1024];
		protected int _bufferReceivedCount;
		protected byte[] _bufferTarget = new byte[16 * 1024];
		protected RiverServer Server { get; private set; }
		protected TcpClient Client { get; private set; }

		private Thread _sourceReaderThread;
		private Thread _targetReaderThread;
		private Stream _upstreamClient;
		private DestinationIdentifier _target;
		private bool _isResigned;
		private bool _isReadHandshake = true;
		private object _disposingSync = new object();

		public Handler()
		{
			// StatService.Instance.HandlerAdd(this);
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

			BeginSourceReader();
			ReadMoreHandshake();
		}

#region Dispose

		protected bool IsDisposed { get; private set; }
		string _disposedComment;

		protected bool IsResigned
		{
			get => _isResigned;
			set
			{
				if (_targetReaderThread != null)
				{
					throw new Exception("Can not Resign Handler");
				}
				_isResigned = value;
			}
		}

		public void Dispose()
		{
			Dispose(true);
			// GC.SuppressFinalize(this);
		}

		/*
		~Handler()
		{
			Dispose(false);
		}
		*/

		public override string ToString()
		{
			var b = base.ToString();
			return $"{b} {Source}<=>{Destination} {(IsDisposed ? "Disposed" + _disposedComment : "NotDisposed")}";
		}

		protected virtual void Dispose(bool managed)
		{
			bool isResigned;

			if (IsDisposed)
			{
				return;
			}

			lock (_disposingSync)
			{
				if (IsDisposed)
				{
					return;
				}
				IsDisposed = true;
				isResigned = IsResigned;
			}

			try
			{

				if (isResigned)
				{
					_disposedComment += " Resigned";
					if (Thread.CurrentThread != _sourceReaderThread)
					{
						// Actually should not happen! Only source reader therad should request resignation
						try
						{
							// thread is locked by syncronious read. And I don't want to drop connection!
							_sourceReaderThread?.Abort();
							Trace.WriteLine(TraceCategory.ObjectLive, "Resingning - Aborted");
						}
						catch (Exception ex)
						{
							Trace.TraceError(ex.Message);
						}
						try
						{
							_sourceReaderThread.JoinDebug();
							Trace.WriteLine(TraceCategory.ObjectLive, "Resingning - Joined");
						}
#pragma warning disable CA1031 // Do not catch general exception types
						catch (Exception ex)
						{
							Trace.TraceError(ex.Message);
						}
#pragma warning restore CA1031 // Do not catch general exception types
					}
					else
					{
						Trace.WriteLine(TraceCategory.ObjectLive, "Resingning - From this thread");
						// will exit and close the thread
					}
					_sourceReaderThread = null;
					return; // the other resurces are not touched during this process
				}

				// StatService.Instance.HandlerRemove(this);

				// UPSTREAM
				try
				{
					if (_upstreamClient != null)
					{
						_upstreamClient?.Close();
						_upstreamClient?.Dispose();
						Trace.WriteLine(TraceCategory.ObjectLive, $"{Client?.GetHashCode():X4} Closing Handler - _upstreamClient closed");
					}
				}
				catch (Exception ex)
				{
					Trace.TraceError($"{ex}");
				}
				_upstreamClient = null;
				_targetReaderThread.JoinAbort();
				Trace.WriteLine(TraceCategory.ObjectLive, $"{Client?.GetHashCode():X4} Closing Handler - upstream joined to {_targetReaderThread?.ManagedThreadId}");

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
				_sourceReaderThread.JoinAbort();
			}
			finally
			{
				Trace.WriteLine(TraceCategory.ObjectLive, $"{Client?.GetHashCode():X4} Disposed Handler. {_disposedComment}");
			}
		}

#endregion
		/*
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

#if DEBUG
				Trace.TraceError($"{Source} Handshake... {_bufferReceivedCount} bytes, first 0x{_buffer[HandshakeStartPos]:X2} {_utf8.GetString(_buffer, HandshakeStartPos, 1)} {Preview(_buffer, HandshakeStartPos, _bufferReceivedCount)}");
#endif
				HandshakeHandler();
			}
			catch (Exception ex)
			{
				Trace.TraceError("Handshake: " + ex);
				Dispose();
			}
		}
		*/

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

		// [Obsolete]
		protected void ReadMoreHandshake()
		{
			/*
			Stream.BeginRead(_buffer
				, HandshakeStartPos + _bufferReceivedCount
				, _buffer.Length - _bufferReceivedCount - HandshakeStartPos
				, ReceivedHandshake
				, null);
			*/
			// already scheduled. Just exit the call and you find yourself in while(true) source.Read();
		}

		protected void BeginStreaming()
		{
			BeginReadSource();
			BeginReadTarget();
		}

		void SourceReaderThreadWorker()
		{
			Trace.WriteLine(TraceCategory.ObjectLive, $"Starting thread {Thread.CurrentThread.Name}");
			try
			{
				while (!IsDisposed)
				{
					var c = Stream.Read(_buffer, _bufferReceivedCount, _buffer.Length - _bufferReceivedCount);
					Trace.WriteLine(TraceCategory.ObjectLive, $"Unlocked thread {Thread.CurrentThread.Name}");
					if (!SourceReceived(c))
					{
						break;
					}
				}
			}
			catch (Exception ex) when (ex.IsConnectionClosing())
			{
			}
			catch (Exception ex)
			{
				Trace.TraceError(ex.ToString());
			}
			finally
			{
				Dispose();
				Trace.WriteLine(TraceCategory.ObjectLive, $"Closing thread {Thread.CurrentThread.Name}");
			}
		}

		void BeginSourceReader()
		{
			if (IsDisposed) throw new ObjectDisposedException("");
			if (_sourceReaderThread != null) throw new Exception("_sourceReaderThread already exists");

			_sourceReaderThread = new Thread(SourceReaderThreadWorker);
			ObjectTracker.Default.Register(_sourceReaderThread);
			_sourceReaderThread.IsBackground = true;
			_sourceReaderThread.Name = $"Source Reader: {GetType().Name} {Source}";
			_sourceReaderThread.Start();
		}

		protected void BeginReadSource()
		{
			if (IsDisposed) throw new ObjectDisposedException("");

			Profiling.Stamp(TraceCategory.Misc, "BeginReadSource...");

			// lock (_isReaderSync)
			{
				_isReadHandshake = false;
				_bufferReceivedCount = 0;
			}
		}

		/*
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
				if (SourceReceived(c))
				{
					Stream.BeginRead(_buffer, 0, _buffer.Length, SourceReceived, null);
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
		*/

		bool SourceReceived(int c)
		{
			if (IsDisposed)
			{
				return false;
			}
			if (c > 0)
			{
				Profiling.Stamp(TraceCategory.Misc, "SourceReceived...");
				StatService.Instance.MaxBufferUsage(c, GetType().Name + " src");

				if (_isReadHandshake)
				{
					_bufferReceivedCount += c;

					// a handshake must forward the rest of buffer in case extra data
					// TODO automate this

#if DEBUG
					Trace.TraceError($"{Source} Handshake... {_bufferReceivedCount} bytes, first 0x{_buffer[HandshakeStartPos]:X2} {_utf8.GetString(_buffer, HandshakeStartPos, 1)} {Preview(_buffer, HandshakeStartPos, _bufferReceivedCount)}");
#endif


					HandshakeHandler();
					return true;
				}
				else
				{
					_upstreamClient.Write(_buffer, 0, c);
				}

				Profiling.Stamp(TraceCategory.Misc, "SourceReceived done");
				return true;
			}
			else
			{
				Dispose();
			}

			return false;
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

			Trace.WriteLine(TraceCategory.NetworkingData, $"{Source} >>> send {cnt} bytes >>> {Destination} {Preview(buf, pos, cnt)}");
			_upstreamClient.Write(buf, pos, cnt);
		}

		void TargetReaderThreadWorker()
		{
			Trace.WriteLine(TraceCategory.ObjectLive, $"Starting thread {Thread.CurrentThread.ManagedThreadId} {Thread.CurrentThread.Name}");
			try
			{
				while (!IsDisposed)
				{
					var c = _upstreamClient.Read(_bufferTarget, 0, _bufferTarget.Length);
					if (!TargetReceived(c))
					{
						break;
					}
				}
			}
			catch (Exception ex) when (ex.IsConnectionClosing())
			{
			}
			catch (Exception ex)
			{
				Trace.TraceError(ex.ToString());
			}
			finally
			{
				Dispose();
				Trace.WriteLine(TraceCategory.ObjectLive, $"Closing thread {Thread.CurrentThread.ManagedThreadId} {Thread.CurrentThread.Name}");
			}
		}

		protected void BeginReadTarget()
		{
			Profiling.Stamp(TraceCategory.Misc, "BeginReadTarget...");
			_targetReaderThread = new Thread(TargetReaderThreadWorker);
			_targetReaderThread.IsBackground = true;
			_targetReaderThread.Name = $"Target Reader: {GetType().Name} {Destination}";
			_targetReaderThread.Start();
			ObjectTracker.Default.Register(_targetReaderThread);

			// _upstreamClient.BeginRead(_bufferTarget, 0, _bufferTarget.Length, TargetReceived, null);
		}

		string Source
		{
			get
			{
				return $"{Client?.GetHashCode():X4} {Client?.Client?.RemoteEndPoint}";
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

		/*
		private void TargetReceived(IAsyncResult ar)
		{
			if (IsDisposed)
			{
				return;
			}

			Profiling.Stamp($"TargetReceived... from {_upstreamClient?.GetType()?.Name}");
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
		*/

		private bool TargetReceived(int c)
		{
			if (IsDisposed)
			{
				return false;
			}

			if (c > 0)
			{
				StatService.Instance.MaxBufferUsage(c, GetType().Name + " trg");
				Trace.WriteLine(TraceCategory.NetworkingData, $"{Source} <<< {c} bytes <<< {Destination} {Preview(_bufferTarget, 0, c)}");
				Stream.Write(_bufferTarget, 0, c);
				return true;
			}
			else
			{
				Dispose();
				return false;
			}

		}

		private static string Preview(byte[] buf, int pos, int cnt)
		{
			#if DEBUG
			var lim = Math.Min(cnt, 32);
			var str = _utf8.GetChars(buf, pos, lim);
			for (var i = 0; i < str.Length; i++)
			{
				if (str[i] < 32)
				{
					str[i] = '?';
				}
			}
			return new string(str) + (lim < cnt ? "..." : "");
			#else
			return string.Empty;
			#endif
		}

		protected void EstablishUpstream(DestinationIdentifier target)
		{
			if (IsDisposed) throw new ObjectDisposedException("");

			Profiling.Stamp(TraceCategory.Networking, "EstablishUpstream...");

			try
			{
				if (target is null)
				{
					throw new ArgumentNullException(nameof(target));
				}

				_target = target;
				Trace.WriteLine(TraceCategory.Networking, $"{Source} Route to {Destination}");

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
			Profiling.Stamp(TraceCategory.Networking, "Established");
		}



	}
}
