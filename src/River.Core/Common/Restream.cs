using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace River.Common
{
	/// <summary>
	/// This stream buffers underlying stream reads and allows to reset buffer position to plug the stream into other receiver
	/// After first response this is no longer possible and stream works as direct wrapper
	/// </summary>
	public class ReStream : SimpleNetworkStream
	{
		readonly Stream _underlying;
		byte[] _buffer = new byte[16 * 1024];
		int _bufferLeftPos; // basically, the external reader's position (consumer)
		int _bufferRightPos; // every receive from underlying - puts data on top of it to fill up (producer)
		bool _connecting;
		bool _connected;

		ConnectionState _state;

		enum ConnectionState
		{
			Buffered,
			Connecting,
			Connected,
		}

		public ReStream(Stream underlying)
		{
			_underlying = underlying;
		}

		public void ResetReader()
		{
			if (_state > 0)
			{
				throw new IOException("ReStream already straight!");
			}

			_bufferLeftPos = 0;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			if (_bufferLeftPos == _bufferRightPos)
			{
				if (_state == ConnectionState.Connecting)
				{
					_buffer = null;
					_bufferLeftPos = 0;
					_bufferRightPos = 0;
					_state = ConnectionState.Connected;
				}
				if (_state == ConnectionState.Connected)
				{
					// dirrect
					return _underlying.Read(buffer, offset, count);
				}
				else
				{
					// buffer - top up
					var c = _underlying.Read(_buffer, _bufferRightPos, _buffer.Length - _bufferRightPos);
					_bufferRightPos += c;
				}
			}

			// response from buffer
			var unread = _bufferRightPos - _bufferLeftPos;
			var m = Math.Min(unread, count);

			Array.Copy(_buffer, _bufferLeftPos, buffer, offset, m);
			_bufferLeftPos += m;

			return m;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			if (_state == ConnectionState.Buffered)
			{
				_state = ConnectionState.Connecting; // schedule read buffer flush
			}

			_underlying.Write(buffer, offset, count);
		}
	}
}
