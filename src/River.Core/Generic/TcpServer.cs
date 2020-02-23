using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace River.Generic
{
	public class TcpServer<THandler> : RiverServer where THandler : Handler, new()
	{
		List<TcpListener> _tcpListeners = new List<TcpListener>();

		protected override void RunCore(ServerConfig config)
		{
			if (config is null)
			{
				throw new ArgumentNullException(nameof(config));
			}
			Trace.WriteLine($"Running {GetType().Name} :{config.EndPoints.First().Port}...");

			lock (_tcpListeners)
			{
				foreach (var item in config.EndPoints)
				{
					var listener = new TcpListener(item);
					_tcpListeners.Add(listener);
					listener.Start();
					AcceptCycleAsync(listener);
				}
			}
		}

		async Task AcceptAsync(TcpListener listener)
		{
			var client = await listener.AcceptTcpClientAsync();
			CreateHandler(client);
		}

		async void AcceptCycleAsync(TcpListener listener)
		{
			try
			{
				await AcceptAsync(listener);
				await DebuggerTracker.EnsureNoDebuggerAsync(); // wait till exit from pause to avoid flooding debugger by threads
				AcceptCycleAsync(listener);
			}
			catch (ObjectDisposedException)
			{
				Dispose();
			}
			catch (Exception ex)
			{
				Console.WriteLine("Stopping due to: " + ex);
				Dispose();
				// throw;
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

			lock (_tcpListeners)
			{
				foreach (var listener in _tcpListeners)
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
					try
					{
						listener.Stop();
					}
#pragma warning disable CA1031 // Do not catch general exception types
					catch { }
#pragma warning restore CA1031 // Do not catch general exception types
				}
				_tcpListeners.Clear();
			}
		}
	}
}
