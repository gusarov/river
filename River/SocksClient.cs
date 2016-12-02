using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace River
{
	public abstract class SocksClient : SimpleNetworkStream, IDisposable
	{

		protected TcpClient _client;
		protected NetworkStream _stream;

		public abstract void Connect(string proxyHost, int proxyPort, string targetHost, int targetPort, bool? proxyDns = null);

		protected void ConnectBase(string proxyHost, int proxyPort)
		{
			if (_client != null)
			{
				throw new Exception("Already been connected");
			}
			_client = new TcpClient(proxyHost, proxyPort);
			_stream = _client.GetStream();
		}

		protected static byte[] GetProtBytes(int targetPort)
		{
			var portBuf = BitConverter.GetBytes(checked((ushort)targetPort));
			if (BitConverter.IsLittleEndian)
			{
				portBuf = new[] { portBuf[1], portBuf[0], };
			}
#if DEBUG
			if (portBuf.Length != 2)
			{
				throw new Exception("Fatal: portBuf must be 2 bytes");
			}
#endif
			return portBuf;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return _stream.Read(buffer, offset, count);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			_stream.Write(buffer, offset, count);
		}

	}
}
