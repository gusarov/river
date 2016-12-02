using System;
using System.Net.Sockets;

namespace River
{
	public class SocksServerTunnelClientWorker : SocksServerClientWorker
	{
		private TcpClient _clientForward;
		private NetworkStream _streamForward;
		private byte[] _bufferForwardRead = new byte[1024 * 32];

		public SocksServerTunnelClientWorker(TcpClient client) : base(client)
		{
		}

		protected override void EstablishForwardConnection()
		{
			throw new NotImplementedException();
		}

		protected override void SendForward(byte[] buffer, int pos, int count)
		{
			throw new NotImplementedException();
		}
	}
}