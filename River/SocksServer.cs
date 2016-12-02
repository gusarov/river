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
			_listener = new TcpListener(IPAddress.Any, port);
			_listener.Start();
			_listener.BeginAcceptTcpClient(NewTcpClient, null);
		}

		private void NewTcpClient(IAsyncResult ar)
		{
			Trace.WriteLine("NewTcpClient called back at " + Thread.CurrentThread.ManagedThreadId);
			var client = _listener.EndAcceptTcpClient(ar);
			Activator.CreateInstance(typeof (T), client);
			_listener.BeginAcceptTcpClient(NewTcpClient, null);
		}

		public void Dispose()
		{
			_listener?.Stop();
		}


	}
}
