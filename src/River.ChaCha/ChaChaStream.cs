using CSChaCha20;
using River.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace River.ChaCha
{
	public class ChaChaStream : CustomStream
	{
		private readonly byte[] _key;

		ChaCha20B _chachaEncrypt;
		ChaCha20B _chachaDecrypt;

		public ChaChaStream(Stream underlying, string password)
			: this(underlying, ChaCha20B.Kdf(password))
		{

		}

		public ChaChaStream(Stream underlying, byte[] key)
			: base(underlying)
		{
			_key = key;
			_chachaEncrypt = new ChaCha20B(_key, _nonceLen);
			Init(Send, Read);
		}

		const int _nonceLen = 8;
		byte[] _readBuffer = new byte[16 * 1024];
		byte[] _encryptBuffer = new byte[16 * 1024];
		bool _icSent;
		bool _icReceived;

		int Read(Stream underlying, byte[] buf, int pos, int cnt)
		{
			var r = underlying.Read(_readBuffer, 0, cnt);
			if (r == 0) return 0;
			var ro = 0;

			if (!_icReceived)
			{
				var remoteNonce = new byte[_nonceLen];
				Array.Copy(_readBuffer, 0, remoteNonce, 0, _nonceLen);
				_icReceived = true;
				r -= _nonceLen;
				ro += _nonceLen;
				_chachaDecrypt = new ChaCha20B(_key, remoteNonce);
			}
			_chachaDecrypt.Crypt(_readBuffer, ro, buf, pos, r);
			Trace.WriteLine("DEC << " + Utils.Utf8.GetString(buf, pos, Math.Max(r, 40)));
			return r;
		}

		void Send(Stream underlying, byte[] buf, int pos, int cnt)
		{
			Trace.WriteLine("ENC >> " + Utils.Utf8.GetString(buf, pos, Math.Max(cnt, 40)));

			_chachaEncrypt.Crypt(buf, pos, _encryptBuffer, _icSent ? 0 : _nonceLen, cnt);
			if (!_icSent)
			{
				_chachaEncrypt.Nonce.CopyTo(_encryptBuffer, 0); // crypt been done with a shift to left this space for nonce
				cnt += _nonceLen;
				_icSent = true;
			}
			underlying.Write(_encryptBuffer, 0, cnt);
		}


	}
}
