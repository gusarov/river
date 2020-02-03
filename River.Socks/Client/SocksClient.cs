using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace River
{
	public abstract class SocksClient : SimpleNetworkStream, IDisposable
	{
		protected TcpClient _client;
		protected Stream _stream;

		public abstract void Route(string targetHost, int targetPort, bool? proxyDns = null);

		/// <summary>
		/// Plug to a new socket
		/// </summary>
		public virtual void Plug(string proxyHost, int proxyPort)
		{
			if (_stream != null)
			{
				throw new Exception("Already been plugged");
			}
			_client = new TcpClient(proxyHost, proxyPort);
			_stream = _client.GetStream();
		}

		/// <summary>
		/// Plug to existing channel
		/// </summary>
		public virtual void Plug(Stream stream)
		{
			if (_stream != null)
			{
				throw new Exception("Already been plugged");
			}
			_stream = stream;
		}

		protected static byte[] GetPortBytes(int targetPort)
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
			_stream.Flush();
			return _stream.Read(buffer, offset, count);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			_stream.Write(buffer, offset, count);
		}

		public override void Flush()
			=> _stream.Flush();

		public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
			=> _stream.BeginRead(buffer, offset, count, callback, state);

		public override int EndRead(IAsyncResult asyncResult)
			=> _stream.EndRead(asyncResult);

		public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
			=> _stream.BeginWrite(buffer, offset, count, callback, state);

		public override void EndWrite(IAsyncResult asyncResult)
			=> _stream.EndWrite(asyncResult);

		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
			=> _stream.ReadAsync(buffer, offset, count, cancellationToken);

		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
			=> _stream.WriteAsync(buffer, offset, count, cancellationToken);
	}
}
