using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace River.Socks
{

	public class SocksServer : Server
	{
		private readonly TcpListener _listener;

		public SocksServer(ListenerConfig config)
			: base(config)
		{
			Trace.WriteLine("SocksServer created at " + Thread.CurrentThread.ManagedThreadId);
			// listen both ipv6 and ipv4
			/*
#if Net45
			try
			{
				_listener = TcpListener.Create(port);
			}
			catch (SocketException)
			{
				_listener = new TcpListener(new IPEndPoint(IPAddress.Loopback, port));
			}
#else
			_listener = new TcpListener(IPAddress.IPv6Any, port);
			_listener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
#endif
			_listener.Start();
			_listener.BeginAcceptTcpClient(NewTcpClient, null);
			*/
		}

		protected override Handler CreateHandler(TcpClient client)
		{
			return new SocksHandler(this, client);
		}

		/*
		public long Bandwidth { get; set; } = 1024 * 1024;

		private void NewTcpClient(IAsyncResult ar)
		{
			try
			{
				var client = _listener.EndAcceptTcpClient(ar);
				Trace.WriteLine($"NewTcpClient (thread {Thread.CurrentThread.ManagedThreadId}) from {client.Client.RemoteEndPoint}");
				var inst = Activator.CreateInstance(typeof(T), this, client);
				var th = inst as IThrottable;
				if (th != null)
				{
					th.MaximumBytesPerSecond = Bandwidth;
				}
				_listener.BeginAcceptTcpClient(NewTcpClient, null);
			}
			catch (ObjectDisposedException)
			{
				
			}
		}


		public void Dispose()
		{
			_listener?.Stop();
		}
		*/


	}
}
