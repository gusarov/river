using River.ChaCha;
using River.Common;
using River.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace River.HttpWrap
{
	public class HttpWrapHandler : Handler
	{
		new HttpWrapServer Server => (HttpWrapServer)base.Server;

		/// <summary>
		/// This will wrap byte stream into HTTP response request.
		/// Note that Server will send responses even without request, so, statefull HTTP firewal might get suspicious.
		/// </summary>
		protected override Stream WrapStream(Stream stream)
		{
			return new ChaCha20Stream(new CustomStream(stream, Wrap, Unwrap), Server.Password);
		}

		#region HTTP Stream

		int _headerBegin;

		bool _readBody; // when header is parsed and body parsing is in progress
		byte[] _readBuf = new byte[16 * 1024];
		int _readContentLength; // HTTP ContentLength of current request
		int _readReceivedContent; // HTTP ContentLength received so far
		int _readTo; // right pointer of the buffer, indicates fill
		int _readFrom; // left pointer of the buffer, indicates consumption

		private int Unwrap(Stream stream, byte[] buf, int pos, int cnt)
		{
			if (!_readBody) {
				// HTTP HEADERS
				int eoh;
				IDictionary<string, string> request;
				do {
					// TODO detect end of buffer and provide better exception
					var c = stream.Read(_readBuf, _readTo, _readBuf.Length - _readTo);
					if (c == 0)
					{
						return 0;
					}
					_readTo += c;

					request = HttpUtils.TryParseHttpHeader(_readBuf, _headerBegin, _readTo - _headerBegin, out eoh);
				} while (eoh < 1);
				eoh += _headerBegin; // shift to buffer space
				_readFrom = eoh; // consider headers as processed
				_readReceivedContent = _readTo - eoh;
				_readBody = true;
				if (!int.TryParse(request["Content-Length"], out _readContentLength))
				{
					Trace.WriteLine(TraceCategory.NetworkingData, "Content-Length is mandatory for HTTP/1.1 & Keep-Alive");
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
					// TODO check buffer boundaries, might need a shift or reset
					// let's do 1 read. It might be not enough, but still, one by one
					var c = stream.Read(_readBuf, _readTo
						// no more than buffer remained and no more than content remainted
						, Math.Min(_readBuf.Length - _readTo, _readContentLength - _readReceivedContent));
					_readReceivedContent += c;
					_readTo += c;
					// PLEASE NOTE: readTo might already been promoted further than the end of current body!
					// During the header retrival, where no content length been limited.
				}

				// whatever been readed so far must be released, but stream and handler state must be ready to continue handle current batch till ContentLength end

				// [HDR][... _readFrom^ ... _readTo^]

				// and we have to respect cnt! requested by caller

				var len = Math.Min(cnt, _readTo - _readFrom);
				Array.Copy(_readBuf, _readFrom, buf, pos, len);
				_readFrom += len;
				if (_readTo == _readFrom)
				{
					_readTo = 0;
					_readFrom = 0;
				}
				if (_readReceivedContent == _readContentLength)
				{
					_readReceivedContent = 0;
					_readContentLength = 0;
					_readBody = false; // go back to header parsing state // TODO if there is buffer remained - add this to the beginning or use _headerBegin
				}
				return len;
			}
			throw new Exception("Wtf?");
			return 0;
		}

		private void Wrap(Stream stream, byte[] buf, int pos, int cnt)
		{
			var headers = _utf8.GetBytes($@"HTTP/1.1 200 OK
Content-Length: {cnt}

");
			stream.Write(headers, 0, headers.Length);
			stream.Write(buf, pos, cnt);
		}

		#endregion

		int _portRequested;
		string _hostRequested;
		static Encoding _utf8 = new UTF8Encoding(false, false);

		protected override void HandshakeHandler()
		{
			var b = 0;
			if (EnsureReaded(3))
			{
				if (_buffer[b++] != 2) throw new Exception($"ver {_buffer[b-1]} not supported");
				if (_buffer[b++] != 1) throw new Exception($"cmd {_buffer[b - 1]} not supported");
				b++; // reserved

				_portRequested = _buffer[b++] * 256 + _buffer[b++];

				if (_buffer[b++] != 1) throw new Exception("only dns adr type supported");
				var adrLen = _buffer[b++];
				if (EnsureReaded(b + adrLen))
				{
					_hostRequested = _utf8.GetString(_buffer, b, adrLen);
					b += adrLen;

					EstablishUpstream(new DestinationIdentifier
					{
						Host = _hostRequested,
						Port = _portRequested,
					});
					if (_bufferReceivedCount > b)
					{
						SendForward(_buffer, b, _bufferReceivedCount - b);
					}
					BeginStreaming();
				}
			}
		}

	}
}
