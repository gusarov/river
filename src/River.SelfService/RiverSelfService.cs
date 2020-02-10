using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
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
			try
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
			catch
			{
				Dispose();
				throw;
			}
		}

		// static string _etag = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();

		private void HandleRequest(int end)
		{
			try
			{
				var str = _utf.GetString(_request, 0, end);
				Console.WriteLine(str);

				var is1 = str.IndexOf(' ');
				var is2 = str.IndexOf(' ', is1 + 1);
				/*
				var inmh = "\nIF-NONE-MATCH: ";
				var inm = str.ToUpperInvariant().IndexOf(inmh);
				if (inm > 0)
				{
					inm += inmh.Length;
				}
				*/
				var ie = str.IndexOf('\r');
				if (is2 < 0) is2 = ie; // HTTP 0.9

/*

				if (inm > 0)
				{
					var inme = str.IndexOf('\r', inm);
					var etag = str.Substring(inm, inme - inm).Trim();
					if (etag.ToUpperInvariant() == _etag.ToUpperInvariant())
					{
						/*
						header = $@"HTTP/1.0 304 {msg}
Content-Type: {contentType}
Connection: keep-alive
Server: river
ETag: {_etag}

";
*/
					}
				}
*/

				var url = str.Substring(is1 + 1, is2 - is1 - 1).Trim();

				var buf = new byte[16 * 1024];
				var c = GetResponse(url.Trim(), buf, 0, buf.Length, out var code, out var msg, out var contentType);
				var headerStr = $@"HTTP/1.0 {code} {msg}
Content-Length: {c}
Content-Type: {contentType}
Connection: keep-alive
Server: river
ETag: {_etag}

";

				Console.WriteLine(url);
				Console.WriteLine(headerStr);
				Console.WriteLine(_utf.GetString(buf, 0, Math.Min(128, c)));
				var header = _utf.GetBytes(headerStr);
				Array.Copy(header, 0, _readBuf, _readTo, header.Length);
				_readTo += header.Length;

				Array.Copy(buf, 0, _readBuf, _readTo, c);
				_readTo += c;
				_auto.Set();
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		int GetResponse(string url, byte[] buf, int pos, int cnt, out int code, out string msg, out string contentType)
		{
			url = url.ToLowerInvariant();
			code = 200;
			msg = "OK";
			contentType = "text/html";

			try
			{
				if (url == "/")
				{
					var ver = Assembly.GetExecutingAssembly().GetName().Version;
					var data1 = _utf.GetBytes($@"<b>Hello</b><br/>This is a River server v{ver}<br/><img src='break_firewall_512.png' />");
					data1.CopyTo(buf, pos);
					return data1.Length;
				}
				else if (url.Contains('.')) // file
				{
					var fileName = url.Substring(1);

					switch (Path.GetExtension(fileName))
					{
						case ".ico":
							contentType = "image/x-icon";
							break;
						case ".png":
							contentType = "image/png";
							break;
					}

					var asm = Assembly.GetExecutingAssembly();
					var iconName = asm.GetManifestResourceNames().FirstOrDefault(x => x.Contains(fileName));
					if (iconName == null)
					{
						var existing = asm.GetManifestResourceNames().ToArray();
						goto L404;
					}
					using (var stream = asm.GetManifestResourceStream(iconName))
					{
						var c = stream.Read(buf, pos, cnt);
						return c;
					}
				}
			L404:
				code = 404;
				msg = "Not Found";
				var data2 = _utf.GetBytes($@"<b>Page not found</b>");
				data2.CopyTo(buf, pos);
				return data2.Length;
			}
			catch (Exception ex)
			{
				code = 500;
				msg = "Server Error";
				var data = _utf.GetBytes($@"<b>{ex.GetType().Name}: {ex.Message}</b>");
				data.CopyTo(buf, pos);
				return data.Length;
			}
		}



		AutoResetEvent _auto = new AutoResetEvent(false);
		byte[] _readBuf = new byte[16 * 1024];
		int _readFrom;
		int _readTo;

		public override int Read(byte[] buffer, int offset, int count)
		{
			try
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
					else
					{
						_readFrom = 0;
						_readTo = 0;
					}
					return m;
				}
				return 0;
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		public override void Flush() => throw new NotImplementedException();
		public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
		public override void SetLength(long value) => throw new NotImplementedException();
	}
}
