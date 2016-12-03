using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace River
{
	public class SocksServer<T> : IDisposable
		where T : SocksServerClientWorker
	{
		private readonly TcpListener _listener;

		public SocksServer(int port)
		{
			Trace.WriteLine("SocksServer created at " + Thread.CurrentThread.ManagedThreadId);
			// listen both ipv6 and ipv4
#if Net45
			_listener = TcpListener.Create(port);
#else
			_listener = new TcpListener(IPAddress.IPv6Any, port);
			_listener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
#endif
			_listener.Start();
			_listener.BeginAcceptTcpClient(NewTcpClient, null);
		}

		private void NewTcpClient(IAsyncResult ar)
		{
			var client = _listener.EndAcceptTcpClient(ar);
			Trace.WriteLine($"NewTcpClient (thread {Thread.CurrentThread.ManagedThreadId}) from {client.Client.RemoteEndPoint}");
			Activator.CreateInstance(typeof (T), this, client);
			_listener.BeginAcceptTcpClient(NewTcpClient, null);
		}

		public void Dispose()
		{
			_listener?.Stop();
		}


	}
}
