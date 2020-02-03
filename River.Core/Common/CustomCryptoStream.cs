using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace River.Common
{
	public class CustomCryptoStream : SimpleNetworkStream
	{
		private readonly Stream _underlying;
		private readonly Action<Stream, byte[], int, int> _encrypt;
		private readonly Func<Stream, byte[], int, int, int> _decrypt;

		public CustomCryptoStream(Stream underlying, Action<Stream, byte[], int, int> encrypt, Func<Stream, byte[], int, int, int> decrypt)
		{
			_underlying = underlying;
			_encrypt = encrypt;
			_decrypt = decrypt;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			_encrypt(_underlying, buffer, offset, count);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return _decrypt(_underlying, buffer, offset, count);
		}
	}

}
