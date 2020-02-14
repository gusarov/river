using River.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace River.HttpWrap
{
	public class HttpWrapHandler : Handler
	{
		/// <summary>
		/// This will wrap byte stream into HTTP response request.
		/// Note that Server will send responses even without request, so, statefull HTTP firewal might get suspicious.
		/// </summary>
		protected override Stream WrapStream(Stream stream)
		{
			return new CustomStream(stream, Wrap, Unwrap);
		}

		#region HTTP Stream

		int _headerBegin;
		bool _readBody; // when header is parsed and body parsing is in progress
		byte[] _readBuf = new byte[16 * 1024];
		int _readContentLength;
		int _readReceivedContent;

		private int Unwrap(Stream stream, byte[] buf, int pos, int cnt)
		{
			int readPos = 0;
			if (!_readBody) {
				// HTTP HEADERS
				int eoh;
				IDictionary<string, string> request;
				do {
					// TODO detect end of buffer and provide better exception
					var c = stream.Read(_readBuf, readPos, _readBuf.Length - readPos);
					readPos += c;

					request = HttpUtils.TryParseHttpHeader(_readBuf, _headerBegin, readPos - _headerBegin, out eoh);
				} while (eoh < 1);
				eoh += _headerBegin; // shift to buffer space
				_readReceivedContent = readPos - eoh;
				_readBody = true;
				if (!int.TryParse(request["Content-Length"], out _readContentLength))
				{
					Trace.WriteLine("Content-Length is mandatory for HTTP/1.1 & Keep-Alive");
					Dispose();
				}
			}

			if (_readBody)
			{
				// BODY

				/*
				// for large body - shift an array first
				if (_readPos + len <= _readBuf.Length)
				{
					// small, remaining fit into buf
				}
				else if (len < _readBuf.Length) 
				{
					// ramaining is not fit, but content do - shift array
					if (_readPos > eoh)
					{
						// if we already got something beyond the header
						Array.Copy(_readBuf, eoh, _readBuf, 0, _readPos - eoh);
					}
				}
				else
				{
					// data is too big anyway - does not make sence to shift, must stream
					throw new NotSupportedException("Content too big, should have streaming");
				}
				*/

				if (_readReceivedContent < _readContentLength)
				{
					// let's do 1 read. It might be not enough, but still, one by one
					var c = stream.Read(_readBuf, readPos
						// no more than buffer remained and no more than content remainted
						, Math.Min(_readBuf.Length - readPos, _readContentLength - _readReceivedContent));
					_readReceivedContent += c;
					readPos += c;
				}

				// decrypt to reader

			}
			return 0;
		}

		private void Wrap(Stream stream, byte[] buf, int pos, int cnt)
		{
		}

		#endregion

		protected override void HandshakeHandler()
		{
			int eoh;
			var request = HttpUtils.TryParseHttpHeader(_buffer, 0, _bufferReceivedCount, out eoh);
			if (eoh >= 0)
			{
				// we don't actually care here, no reason to do any negotiation in HTTP level,
				// HTTP should not have a river traces to avoid firewall rules
				if (request.TryGetValue("Content-Length", out var len))
				{
					// now should have a whole request here
					
				}
			}
			else
			{
				ReadMoreHandshake();
			}
		}

	}
}
