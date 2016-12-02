using System;
using System.Net.Sockets;
using System.Text;

namespace River
{
	public class RiverClient : SimpleNetworkStream, IDisposable
	{
		protected TcpClient _client;
		protected NetworkStream _stream;

		public void ConnectRiver(string riverHost, int riverPort, string targetHost, int targetPort)
		{
			if (_client != null)
			{
				throw new Exception("Already been connected");
			}
			_client = new TcpClient(riverHost, riverPort);
			_stream = _client.GetStream();
			// negotiate
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException();
		}
	}
}