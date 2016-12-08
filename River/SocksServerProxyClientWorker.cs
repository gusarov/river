using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace River
{
	public class SocksServerProxyClientWorker : SocksServerClientWorker
	{
		private TcpClient _clientForward;
		private NetworkStream _streamForward;
		private byte[] _bufferForwardRead = new byte[1024 * 32];

		public override void Dispose()
		{
			var clientForward = _clientForward;
			var streamForward = _streamForward;
			try
			{
				clientForward?.Close();
				_clientForward = null;
			}
			catch { }
			try
			{
				streamForward?.Close();
				_streamForward = null;
			}
			catch { }
			base.Dispose();
		}

		public SocksServerProxyClientWorker(SocksServer<SocksServerProxyClientWorker> server, TcpClient client) : base(client)
		{
		}

		protected override void EstablishForwardConnection()
		{
			var addressesRequested = _addressesRequested;
			if (addressesRequested == null && _dnsNameRequested != null)
			{
				addressesRequested = Dns.GetHostAddresses(_dnsNameRequested);
			}
			if (addressesRequested == null)
			{
				throw new Exception("Not resolved");
			}
			_clientForward = new TcpClient();
			_clientForward.Connect(addressesRequested, _portRequested);
			_streamForward = _clientForward.GetStream();
			_clientForward.Client.NoDelay = true;
			_streamForward.BeginRead(_bufferForwardRead, 0, _bufferForwardRead.Length, ReceivedFromForwarder, null);
		}

		private void ReceivedFromForwarder(IAsyncResult ar)
		{
			try
			{
				if (_streamForward == null)
				{
					return;
				}
				var count = _streamForward.EndRead(ar);
				Trace.WriteLine("Streaming - received from forward stream " + count + " bytes on thread #" + Thread.CurrentThread.ManagedThreadId);

				// write back to socks client
				if (count != 0
#if CC
					|| _clientForward.Connected
#endif
					)
				{
					_stream.Write(_bufferForwardRead, 0, count);
					// continue async
					_streamForward.BeginRead(_bufferForwardRead, 0, _bufferForwardRead.Length, ReceivedFromForwarder, null);
				}
				else
				{
					Dispose();
				}
			}
			catch (Exception ex)
			{
				Trace.TraceError("Streaming - received from forwarder client: " + ex);
				Dispose();
			}
		}

		protected override void SendForward(byte[] buffer, int pos, int count)
		{
			_streamForward.Write(buffer, pos, count);
		}
	}
}