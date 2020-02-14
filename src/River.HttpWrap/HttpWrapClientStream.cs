using River.ChaCha;
using River.Common;
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
		bool _negotiated;
		byte[] _key;
		byte[] _nonce;
		byte[] _bufRq = new byte[1 * 1024];
		byte[] _bufOut = new byte[16 * 1024];
		byte[] _bufIn = new byte[16 * 1024];
		Encoding _utf8 = new UTF8Encoding(false, false);
		ChaCha20 _encrypt;
		ChaCha20 _decrypt;
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
			Stream = new MustFlushStream(new CustomStream(stream, Send, Receive));
		}

		private int Receive(Stream arg1, byte[] arg2, int arg3, int arg4) => throw new NotImplementedException();

		public override void Plug(Uri proxyUri)
		{
			ConfigureUri(proxyUri);
			base.Plug(proxyUri); // base performs regular Tcp connection
			Plug(Client.GetStream()); // but we are wrapping it here a little bit differently
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

		void Send(Stream stream, byte[] buf, int pos, int cnt)
		{
			var b = 0;
			if (!_negotiated)
			{
				_negotiated = true;
				_nonce = Guid.NewGuid().ToByteArray().Take(12).ToArray();
				_nonce.CopyTo(_bufRq, 0);
				b += _nonce.Length;
				var nb = b;

				_bufRq[b++] = 2; // river HttpWrap ver - 2
				_bufRq[b++] = 1; // river HttpWrap cmd - connect
				_bufRq[b++] = 0; // river HttpWrap reserved for flags

				_bufRq[b++] = (byte)(_routeToPort >> 8);
				_bufRq[b++] = (byte)_routeToPort;
				var host = _utf8.GetBytes(_routeToHost);
				_bufRq[b++] = checked((byte)host.Length); // len
				host.CopyTo(_bufRq, b);
				b += host.Length;
				_encrypt = new ChaCha20(_pass, _nonce);
				_encrypt.Inplace(_bufRq, nb, b); // inplace encrypt after nonce
			}

			Array.Copy(buf, pos, _bufRq, b, cnt);
			_encrypt.Inplace(_bufRq, b, cnt); // inplace encrypt
			b += cnt;

			var headers = $@"POST /{RandomPath} HTTP/1.1
Host: {ProxyHost}
Connection: keep-alive
Content-Length: {b}

";
			var hb = _utf8.GetBytes(headers, 0, headers.Length, _bufOut, 0);
			Array.Copy(_bufRq, 0, _bufOut, hb, b);

			stream.Write(_bufOut, 0, hb + b);
		}

		public override void Route(string targetHost, int targetPort, bool? proxyDns = null)
		{
			_routeToHost = targetHost;
			_routeToPort = targetPort;
		}


	}
}
