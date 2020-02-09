using System;
using System.Net;
using System.Net.Sockets;

namespace River.Test
{
	public class DemoTcpServer : IDisposable
	{
		public int Port => ((IPEndPoint)_server.LocalEndpoint).Port;

		TcpListener _server;

		public DemoTcpServer()
		{
			_server = new TcpListener(IPAddress.Loopback, 0);
			_server.Start();
			_server.BeginAcceptTcpClient(AcceptingTcpClient, null);
		}

		void AcceptingTcpClient(IAsyncResult ar)
		{
			try
			{
				var client = _server.EndAcceptTcpClient(ar);
				Handler(client);
				_server.BeginAcceptTcpClient(AcceptingTcpClient, null);
			}
			catch (Exception ex)
			{
				System.Diagnostics.Trace.TraceError(ex.ToString());
			}
		}

		async void Handler(TcpClient client)
		{
			var stream = client.GetStream();
			var buf = new byte[16 * 1024];
			while (!_disposed)
			{
				var c = await stream.ReadAsync(buf, 0, buf.Length);
				if (c == 0) Dispose();
				for (var i = 0; i < c; i++)
				{
					buf[i] ^= 37;
				}
				stream.Write(buf, 0, c);
			}
		}

		bool _disposed;

		~DemoTcpServer()
		{
			Dispose(false);
		}

		protected virtual void Dispose(bool managed)
		{
			_disposed = true;
			_server.Stop();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}
