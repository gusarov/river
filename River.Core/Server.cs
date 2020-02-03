using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using River.Common;

namespace River
{

	public abstract class Server : IDisposable
	{
		public Forwarder Forwarder { get; set; } // = new NullForwarder();

		List<TcpListener> _tcpListeners = new List<TcpListener>();

		public Server(ServerConfig data)
		{
			lock (_tcpListeners)
			{
				foreach (var item in data.EndPoints)
				{
					var listener = new TcpListener(item);
					_tcpListeners.Add(listener);
					listener.Start();
					AcceptCycleAsync(listener);
				}
			}
		}

		async void AcceptCycleAsync(TcpListener listener)
		{
			/*
			Task.Run(async delegate
			{
				while (!_disposing)
				{
					try
					{
						await AcceptAsync(listener);
					}
					catch (Exception ex)
					{
						Trace.TraceError(ex.ToString());
					}
				}
			});
			*/
			try
			{
				await AcceptAsync(listener);
				await _debuggerTracker.NormalAsync(); // wait till exit from pause
				AcceptCycleAsync(listener);
			}
			catch (Exception ex)
			{
				Console.WriteLine("Stopping due to: " + ex);
				Dispose();
				throw;
			}
		}

		DebuggerTracker _debuggerTracker = new DebuggerTracker();

		protected abstract Handler CreateHandler(TcpClient client);

		async Task AcceptAsync(TcpListener listener)
		{
			var client = await listener.AcceptTcpClientAsync();
			var handler = CreateHandler(client);
		}

		// public Func<ForwarderConnectionData, Forwarder> ForwarderFactory { get; set; }

		protected bool _disposing;

		public void Dispose()
		{
			_disposing = true;
			lock (_tcpListeners)
			{
				foreach (var listener in _tcpListeners)
				{
					try
					{
						listener.Server.Shutdown(SocketShutdown.Both);
						listener.Stop();
					}
					catch { }
				}
				_tcpListeners.Clear();
			}
		}

	}
}
