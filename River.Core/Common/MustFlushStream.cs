using System;
using System.IO;

namespace River.Common
{
	public class MustFlushStream : SimpleNetworkStream
	{
		private readonly Stream _underlying;

		public MustFlushStream(Stream underlying, int bufferLen = 16*1024)
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
				Array.Copy(buffer, offset, _buffer, _bufferPos, count);
				_bufferPos += count;
			}
			else
			{
				// flush existing
				Flush();

				if (count >= buffer.Length)
				{
					// do not cache large requests
					_underlying.Write(buffer, offset, count);
				}
				else
				{
					// just call one more time. This time buffer is empty;
					Write(buffer, offset, count);
				}
			}
		}

		public override void Flush()
		{
			_underlying.Write(_buffer, 0, _bufferPos);
			_bufferPos = 0;
		}

	}

}
