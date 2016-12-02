using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace River
{
	public class RiverServer : IDisposable
	{
		private readonly TcpListener _listener;

		public void Dispose()
		{
			try
			{
				_listener?.Stop();
			}
			catch { }
		}

		public RiverServer(int port)
		{
			_listener = new TcpListener(IPAddress.Any, port);
			_listener.Start();
			_listener.BeginAcceptTcpClient(NewTcpClient, null);
		}

		private void NewTcpClient(IAsyncResult ar)
		{
			var tcpClient = _listener.EndAcceptTcpClient(ar);
			new RiverServerConnection(tcpClient);
			_listener.BeginAcceptTcpClient(NewTcpClient, null);
		}

		public class RiverServerConnection
		{
			private readonly TcpClient _client;
			private readonly NetworkStream _clientStream;
			private readonly byte[] _readBuffer = new byte[1024*32];

			public void Dispose()
			{
				try
				{
					_client?.Close();
				}
				catch { }
				try
				{
					_clientStream?.Close();
				}
				catch { }
			}

			public RiverServerConnection(TcpClient client)
			{
				_client = client;
				_clientStream = _client.GetStream();
				_clientStream.BeginRead(_readBuffer, 0, _readBuffer.Length, ReceivedFromClient, null);
			}

			private void ReceivedFromClient(IAsyncResult ar)
			{
				try
				{
					var count = _clientStream.EndRead(ar);
					if (count > 0)
					{
						// actual work
						_clientStream.BeginRead(_readBuffer, 0, _readBuffer.Length, ReceivedFromClient, null);
					}
				}
				catch (Exception ex)
				{
					Trace.TraceError("ReceivedFromClient Exception: " + ex);
					Dispose();
				}
			}
		}
	}
}
