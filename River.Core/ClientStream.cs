using River.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace River
{
	public abstract class ClientStream : SimpleNetworkStream
	{
		protected TcpClient Client { get; private set; }
		protected Stream Stream { get; set; }

		/// <summary>
		/// Negotiate to establish stream
		/// </summary>
		public abstract void Route(string targetHost, int targetPort, bool? proxyDns = null);

		/// <summary>
		/// Plug to a new socket
		/// </summary>
		public virtual void Plug(Uri uri)
		{
			if (uri is null)
			{
				throw new ArgumentNullException(nameof(uri));
			}

			var proxyHost = uri.Host;
			var proxyPort = uri.Port;

			if (Stream != null)
			{
				throw new Exception("Already been plugged");
			}
			Client = new TcpClient(proxyHost, proxyPort);
			Client.Client.NoDelay = true;
			Stream = Client.GetStream();
		}

		/// <summary>
		/// Plug to existing channel
		/// </summary>
		public virtual void Plug(Uri uri, Stream stream)
		{
			if (Stream != null)
			{
				throw new Exception("Already been plugged");
			}
			/*
			if (!(stream is MustFlushStream))
			{
				stream = new MustFlushStream(stream);
			}
			*/
			Stream = stream;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return Stream.Read(buffer, offset, count);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			Stream.Write(buffer, offset, count);
			Stream.Flush();
		}

		public override void Flush()
			=> Stream.Flush();

		public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
			=> Stream.BeginRead(buffer, offset, count, callback, state);

		public override int EndRead(IAsyncResult asyncResult)
			=> Stream.EndRead(asyncResult);

		public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
			=> Stream.BeginWrite(buffer, offset, count, callback, state);

		public override void EndWrite(IAsyncResult asyncResult)
			=> Stream.EndWrite(asyncResult);

		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
			=> Stream.ReadAsync(buffer, offset, count, cancellationToken);

		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
			=> Stream.WriteAsync(buffer, offset, count, cancellationToken);
	}
}
