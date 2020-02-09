using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace River.SelfService
{
	public class RiverSelfService : Stream
	{
		public override bool CanRead => true;
		public override bool CanSeek => false;
		public override bool CanWrite => true;
		public override long Length => throw new NotSupportedException();

		public override long Position {
			get => throw new NotSupportedException();
			set => throw new NotSupportedException();
		}

		byte[] _request = new byte[16 * 1024];
		int _requestIndex;

		static Encoding _utf = new UTF8Encoding(false, false);

		public override void Write(byte[] buffer, int offset, int count)
		{
			Array.Copy(buffer, offset, _request, _requestIndex, count);
			_requestIndex += count;
			for (var i = 3; i < _requestIndex; i++)
			{
				if (_request[i - 3] == '\r')
				if (_request[i - 2] == '\n')
				if (_request[i - 1] == '\r')
				if (_request[i - 0] == '\n')
				{
					HandleRequest(i);
					Array.Copy(_request, i, _request, 0, i);
					_requestIndex -= i;
				}
			}
		}

		private void HandleRequest(int end)
		{
			var str = _utf.GetString(_request, 0, end);
			Console.WriteLine(str);

			var data = _utf.GetBytes(@"<b>Hello</b>");

			var header = _utf.GetBytes($@"HTTP/1.0 200 OK
Content-Length: {data.Length}
Content-Type: text/html
Connection: keep-alive
Server: river

");
			Array.Copy(header, 0, _readBuf, _readTo, header.Length);
			_readTo += header.Length;
			Array.Copy(data, 0, _readBuf, _readTo, data.Length);
			_readTo += data.Length;
			_auto.Set();
		}

		AutoResetEvent _auto = new AutoResetEvent(false);
		byte[] _readBuf = new byte[16 * 1024];
		int _readFrom;
		int _readTo;

		public override int Read(byte[] buffer, int offset, int count)
		{
			_auto.WaitOne();
			if (_readTo > _readFrom)
			{
				var m = Math.Min(count, _readTo - _readFrom);
				Array.Copy(_readBuf, _readFrom, buffer, offset, m);
				if (_readFrom + m < _readTo)
				{
					var rem = _readTo - _readFrom + m;
					Array.Copy(_readBuf, _readFrom + m, _readBuf, 0, _readTo - _readFrom + m);
					_readFrom = 0;
					_readTo = rem;
				}
				return m;
			}
			return 0;
		}

		public override void Flush() => throw new NotImplementedException();
		public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
		public override void SetLength(long value) => throw new NotImplementedException();
	}
}
