using River.Internal;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace River
{


	public abstract class SimpleNetworkStream : Stream
	{
		public SimpleNetworkStream()
		{
			ObjectTracker.Default.Register(this);
		}

		// protected static readonly Encoding _utf8 = new UTF8Encoding(false, false);

		// optional, so, let's provide empty body
		public override void Flush()
		{

		}

		public sealed override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		public sealed override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		public sealed override bool CanRead { get; } = true;
		public sealed override bool CanSeek { get; } = false;
		public sealed override bool CanWrite { get; } = true;
		public sealed override long Length
		{
			get { throw new NotSupportedException(); }
		}

		public sealed override long Position
		{
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
		}

		/*
		public sealed override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) =>
			base.BeginWrite(buffer, offset, count, callback, state);

		public sealed override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) =>
			base.BeginRead(buffer, offset, count, callback, state);

		public sealed override bool CanTimeout => base.CanTimeout;

		public sealed override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) =>
			base.CopyToAsync(destination, bufferSize, cancellationToken);
		*/

		public bool IsDisposed { get; private set; }

		public override void Close()
		{
			IsDisposed = true;
			base.Close();
		}

		public override string ToString()
		{
			var b = base.ToString();
			return $"{b} {(IsDisposed ? "Disposed" : "NotDisposed")}";
		}
	}
}