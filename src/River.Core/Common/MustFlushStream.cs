using River.Internal;
using System;
using System.IO;

namespace River.Common
{
	/// <summary>
	/// A stream that you must flush to promote write. Basically a write buffer.
	/// </summary>
	public class MustFlushStream : SimpleNetworkStream
	{
		private readonly Stream _underlying;

		public MustFlushStream(Stream underlying, int bufferLen = 16 * 1024)
		{
			_underlying = underlying;
			_buffer = new byte[bufferLen];
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return _underlying.Read(buffer, offset, count);
		}

		byte[] _buffer;
		int _bufferPos;
		public override void Write(byte[] buffer, int offset, int count)
		{
			if (count < (_buffer.Length - _bufferPos))
			{
				// it perfectly fits to the remained buffer
				Array.Copy(buffer, offset, _buffer, _bufferPos, count);
				_bufferPos += count;
				StatService.Instance.MaxBufferUsage(_bufferPos, GetType().Name);
			}
			else
			{
				// flush existing first
				Flush();

				if (count >= buffer.Length)
				{
					// too large - do not cache
					_underlying.Write(buffer, offset, count);
				}
				else
				{
					// just call one more time. This time buffer is empty after flush
					Write(buffer, offset, count);
				}
			}
		}

		public override void Flush()
		{
			_underlying.Write(_buffer, 0, _bufferPos);
			_bufferPos = 0;
		}

		public override void Close()
		{
			base.Close();
			_underlying.Close();
		}

	}

}
