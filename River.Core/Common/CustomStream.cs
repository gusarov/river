using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace River.Common
{
	public class CustomStream : SimpleNetworkStream
	{
		private readonly Stream _underlying;
		private readonly Action<Stream, byte[], int, int> _send;
		private readonly Func<Stream, byte[], int, int, int> _read;

		public CustomStream(Stream underlying, Action<Stream, byte[], int, int> send, Func<Stream, byte[], int, int, int> read)
		{
			_underlying = underlying;
			_send = send;
			_read = read;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			_send(_underlying, buffer, offset, count);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return _read(_underlying, buffer, offset, count);
		}
	}

}
