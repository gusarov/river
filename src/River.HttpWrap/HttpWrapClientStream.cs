using River.ChaCha;
using River.Common;
using River.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace River.HttpWrap
{
	public class HttpWrapClientStream : ClientStream
	{
		static Random _rnd = new Random();

		static string RandomPath
		{
			get
			{
				var c = _rnd.Next(5) + 5;
				var s = "";
				for (var i = 0; i < c; i++)
				{
					s += (char)(_rnd.Next('z' - 'a') + 'a');
				}
				return s;
			}
		}

		string _algo;
		string _pass;
		byte[] _key;
		byte[] _nonce;
		byte[] _bufRq = new byte[1 * 1024];
		byte[] _bufOut = new byte[16 * 1024];
		byte[] _bufIn = new byte[16 * 1024];

		// ChaCha20 _encrypt;
		// ChaCha20 _decrypt;
		string _routeToHost;
		int _routeToPort;

		public HttpWrapClientStream()
		{

		}

		public HttpWrapClientStream(string algo, string pass, string proxyHost, int proxyPort, string targetHost, int targetPort)
		{
			Plug(algo, pass, proxyHost, proxyPort);
			Route(targetHost, targetPort);
		}

		public HttpWrapClientStream(string algo, string pass, Stream stream, string targetHost, int targetPort)
		{
			Plug(new Uri($"hw://{algo}:{pass}@_:0"), stream);
			Route(targetHost, targetPort);
		}

		public void Plug(string algo, string pass, string proxyHost, int proxyPort)
		{
			Plug(new Uri($"hw://{algo}:{pass}@{proxyHost}:{proxyPort}"));
		}

		void Plug(Stream stream)
		{
			Stream = new MustFlushStream(new ChaCha20Stream(new CustomStream(stream, SendHttp, ReceiveHttp), _pass));
			// Stream = new CustomStream(new ChaCha20Stream(stream, _pass), Send, Receive);
			// Stream = new MustFlushStream(new CustomStream(stream, Send, Receive));
		}

		public override void Plug(Uri proxyUri)
		{
			ConfigureUri(proxyUri);
			base.Plug(proxyUri); // base performs regular Tcp connection
			Plug(Client.GetStream2()); // but we are wrapping it here a little bit differently
		}

		public override void Plug(Uri proxyUri, Stream stream)
		{
			ConfigureUri(proxyUri);
			Plug(stream);
		}

		(string algo, string pass) GetUserInfo(Uri uri)
		{
			var info = uri.UserInfo;
			var i = info.IndexOf(':');
			if (i >= 0)
			{
				return (info.Substring(0, i), info.Substring(i + 1));
			}
			return (null, null);
		}

		void ConfigureUri(Uri proxyUri)
		{
			if (proxyUri is null)
			{
				throw new ArgumentNullException(nameof(proxyUri));
			}
			var (user, pass) = GetUserInfo(proxyUri);
			_algo = user;
			_pass = pass;
		}

		void SendHttp(Stream stream, byte[] buf, int pos, int cnt)
		{
			var headers = $@"POST /{RandomPath} HTTP/1.1
Host: {ProxyHost}
Connection: keep-alive
Content-Length: {cnt}

";
			var hb = _utf8.GetBytes(headers, 0, headers.Length, _bufOut, 0);
			Array.Copy(buf, pos, _bufOut, hb, cnt);

			stream.Write(_bufOut, 0, hb + cnt);
		}

		byte[] _readBuf = new byte[16 * 1024];
		bool _readBody;
		int _readContentLength; // HTTP ContentLength of current request
		int _readReceivedContent; // HTTP ContentLength received so far
		int _readFrom;
		int _readTo;

		private int ReceiveHttp(Stream stream, byte[] buf, int pos, int cnt)
		{
			if (!_readBody)
			{
				var eoh = -1;
				IDictionary<string, string> response;
				do
				{
					var c = stream.Read(_readBuf, _readTo, _readBuf.Length - _readTo);
					if (c == 0)
					{
						return 0;
					}
					_readTo += c;
					response = HttpUtils.TryParseHttpHeader(_readBuf, 0, _readTo, out eoh);
				} while (response == null);

				int.TryParse(response["Content-Length"], out _readContentLength);
				_readFrom = eoh; // consider headers as processed
				_readReceivedContent = _readTo - eoh;

				_readBody = true;
			}

			if (_readBody)
			{
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
			// todo
			throw new Exception("Wtf?");
		}

		public override void Route(string targetHost, int targetPort, bool? proxyDns = null)
		{
			_routeToHost = targetHost;
			_routeToPort = targetPort;

			var b = 0;
			// _nonce = Guid.NewGuid().ToByteArray().Take(12).ToArray();
			// _nonce.CopyTo(_bufRq, 0);
			// b += _nonce.Length;
			// var nb = b;

			_bufRq[b++] = 2; // river HttpWrap ver - 2
			_bufRq[b++] = 1; // river HttpWrap cmd - connect
			_bufRq[b++] = 0; // river HttpWrap reserved for flags

			_bufRq[b++] = (byte)(_routeToPort >> 8);
			_bufRq[b++] = (byte)_routeToPort;
			_bufRq[b++] = 1; // adr type dns
			var host = _utf8.GetBytes(_routeToHost);
			_bufRq[b++] = checked((byte)host.Length); // len
			host.CopyTo(_bufRq, b);
			b += host.Length;
			// _encrypt = new ChaCha20(_pass, _nonce);
			// _encrypt.Inplace(_bufRq, nb, b - nb); // inplace encrypt after nonce
			Stream.Write(_bufRq, 0, b);
		}

		public override void Close()
			=> base.Close();


	}
}
