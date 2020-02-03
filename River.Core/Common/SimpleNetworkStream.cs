using System;
using System.IO;
using System.Text;

namespace River
{


	public abstract class SimpleNetworkStream : Stream, IDisposable
	{
		protected static readonly Encoding _utf8 = new UTF8Encoding(false, false);

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
	}
}