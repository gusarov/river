using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace River.Generic
{
	public class TcpServer<THandler> : RiverServer where THandler : Handler, new()
	{
		static Trace Trace = River.Trace.Default;

		class ListenerEntry
		{
			public Thread Thread { get; set; }
			public TcpListener TcpListener { get; set; }
		}

		List<ListenerEntry> _tcpListenerEntries = new List<ListenerEntry>();

		public override string ToString()
		{
			return $"{GetType().Name}:{Config?.EndPoints?.FirstOrDefault()?.Port}";
		}

		protected override void RunCore(ServerConfig config)
		{
			if (config is null)
			{
				throw new ArgumentNullException(nameof(config));
			}
			Trace.WriteLine(TraceCategory.Networking, $"Running {GetType().Name} :{config.EndPoints.First().Port}...");

			lock (_tcpListenerEntries)
			{
				foreach (var item in config.EndPoints)
				{
					var listener = new TcpListener(item);
					listener.Start();

					var thread = new Thread(AcceptWorker);
					ObjectTracker.Default.Register(thread);
					thread.IsBackground = true;
					thread.Name = "Listener for " + item;

					var entry = new ListenerEntry
					{
						TcpListener = listener,
						Thread = thread,
					};

					thread.Start(entry);

					_tcpListenerEntries.Add(entry);
				}
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		void CreateHandlerNoRet(TcpListener listener)
		{
			var client = listener.AcceptTcpClient();
			client.Configure();
			_ = CreateHandler(client);
		}

		void AcceptWorker(object entryObject)
		{
			var entry = (ListenerEntry)entryObject;
			try
			{
				while (!IsDisposed)
				{
					CreateHandlerNoRet(entry.TcpListener);
					// wait till exit from pause to avoid flooding debugger by threads
					while (!DebuggerTracker.IsNoDebugger())
					{
						Console.WriteLine($"Waiting for finishing debugger...");
						Thread.Sleep(2000);
					}
				}
			}
			catch (ObjectDisposedException)
			{
				Dispose();
			}
			catch (SocketException ex) when (ex.IsConnectionClosing())
			{
				Dispose();
			}
			catch (Exception ex)
			{
				Console.WriteLine("Stopping due to: " + ex);
				Dispose();
			}
		}

		protected override Handler CreateHandlerCore(TcpClient client)
		{
			var handler = new THandler();
			handler.Init(this, client);
			return handler;
		}

		protected override void Dispose(bool managed)
		{
			base.Dispose(managed);

			ListenerEntry[] entries;
			lock (_tcpListenerEntries)
			{
				entries = _tcpListenerEntries.ToArray();
				_tcpListenerEntries.Clear();
			}

			foreach (var entry in entries)
			{
				/*
				try
				{
					listener.Server.Shutdown(SocketShutdown.Both);
				}
#pragma warning disable CA1031 // Do not catch general exception types
				catch { }
#pragma warning restore CA1031 // Do not catch general exception types
				*/
#pragma warning restore CA1031 // Do not catch general exception types

				try
				{
					entry?.TcpListener?.Stop();
				}
#pragma warning disable CA1031 // Do not catch general exception types
				catch { }
#pragma warning restore CA1031 // Do not catch general exception types

				try
				{
					entry?.Thread.JoinAbort();
				}
#pragma warning disable CA1031 // Do not catch general exception types
				catch { }
#pragma warning restore CA1031 // Do not catch general exception types
			}

		}
	}
}
