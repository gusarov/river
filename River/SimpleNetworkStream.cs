using System;
using System.IO;
using System.Text;

namespace River
{
	public abstract class SimpleNetworkStream : Stream, IDisposable
	{
		protected static readonly Encoding _utf8 = new UTF8Encoding(false, false);


		public override void Flush()
		{
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		public override bool CanRead { get; } = true;
		public override bool CanSeek { get; } = false;
		public override bool CanWrite { get; } = true;
		public override long Length
		{
			get { throw new NotSupportedException(); }
		}

		public override long Position
		{
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
		}
	}
}