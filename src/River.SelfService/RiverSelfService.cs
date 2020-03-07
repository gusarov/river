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
	public partial class RiverSelfService : SimpleNetworkStream
	{

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
				Profiling.Stamp(TraceCategory.Performance, "SS Writer - HandleRequest");

				var str = _utf.GetString(_request, 0, end);
				// Console.WriteLine(str);

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
					}
				}
ETag: {_etag}
*/

				var url = str.Substring(is1 + 1, is2 - is1 - 1).Trim();

				var resp = GetResponse(url.Trim(), out var code, out var msg, out var contentType);

				var headerStr = $@"HTTP/1.1 {code} {msg}
Content-Length: {resp.Length}
Content-Type: {contentType}
Connection: keep-alive
Server: river

";
				// Console.WriteLine(url);
				// Console.WriteLine(headerStr);
				// Console.WriteLine(_utf.GetString(buf, 0, Math.Min(128, c)));

				// var buf = new byte[16 * 1024];
				// ASCII bytes count match to buffer
				// var buf = new byte[headerStr.Length + resp.Length];

				if (_readFrom != 0)
				{
					throw new Exception("_readFrom != 0");
				}

				if (_readTo != 0)
				{
					throw new Exception("_readTo != 0");
				}

				if (_readBuf.Length < headerStr.Length + resp.Length)
				{
					// entire byte blobs are already there anyway - just resize a transfer buffer
					// there is no streaming done anyway
					_readBuf = new byte[headerStr.Length + resp.Length];
				}

				// header
				var hc = _ascii.GetBytes(headerStr, 0, headerStr.Length, _readBuf, _readTo);
				_readTo += hc;

				// resp
				Array.Copy(resp, 0, _readBuf, _readTo, resp.Length);
				_readTo += resp.Length;

				_readerUnlock.Set();
				// _readerFinished.WaitOne();

				/*
				int transferred = 0;
				do
				{
					Array.Copy(buf, 0, _readBuf, _readTo, c);
					_readTo += c;
					transferred += code;
					Profiling.Stamp("SS Writer - Handled, unlocking");
					_auto.Set();
				} while (transferred < resp.Length);
				*/
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		DateTime _lastInfoObjectsTime;
		string _lastInfoObjectsData;

		string GetStatsPage()
		{
			var now = DateTime.UtcNow;
			if ((now - _lastInfoObjectsTime).TotalSeconds > 6)
			{
				_lastInfoObjectsData = GetStatsPageCore();
				_lastInfoObjectsTime = now;
			}
			return _lastInfoObjectsData;
		}

		string GetHomePage()
		{
			var ver = Assembly.GetExecutingAssembly().GetName().Version;
			return $@"<b>Hello</b><br/>
This is a River server v{ver}<br/>
<a href=stat>Statistics</a><br/>
<img src='break_firewall_512.png' /><br/>";
		}

		const string _header = @"
<nav>
  <a href='/'>Home</a> |
  <a href='/stat'>Statistics</a> |
  <a href='/admin'>Admin</a> |
</nav>
<p>
";
		const string _footer = @"";

		byte[] GetPage(Func<string> getter)
		{
			var page = _header + getter() + _footer;
			var data = _utf.GetBytes(page);
			return data;
		}

		byte[] GetResponse(string url, out int code, out string msg, out string contentType)
		{
			url = url.ToUpperInvariant();
			if (url.Contains("://"))
			{
				url = new Uri(url).PathAndQuery;
			}
			code = 200;
			msg = "OK";
			contentType = "text/html";

			try
			{
				if (url == "/")
				{
					return GetPage(GetHomePage);
				}
				else if (url == "/STAT" || url == "/STATS")
				{
					return GetPage(GetStatsPage);
				}
				else if (url == "/ADMIN")
				{
					return GetPage(GetAdminPage);
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
					var iconName = asm.GetManifestResourceNames().FirstOrDefault(x => x.ToUpperInvariant().Contains(fileName));
					if (iconName == null)
					{
						var existing = asm.GetManifestResourceNames().ToArray();
						goto L404;
					}
					using (var stream = asm.GetManifestResourceStream(iconName))
					{
						var buf = new byte[stream.Length];
						var c = stream.Read(buf, 0, buf.Length);
						return buf;
					}
				}
			L404:
				code = 404;
				msg = "Not Found";
				var data2 = _utf.GetBytes($@"<b>Page not found: {url}</b>");
				return data2;
			}
			catch (Exception ex)
			{
				code = 500;
				msg = "Server Error";
				var data = _utf.GetBytes($@"<b>{code} {msg}</b><br/><b>{ex.GetType().Name}: {ex.Message}</b>");
				return data;
			}
		}

		readonly ManualResetEvent _readerUnlock = new ManualResetEvent(false);
		readonly AutoResetEvent _readerFinished = new AutoResetEvent(false);

		byte[] _readBuf = new byte[1 * 1024];
		int _readFrom;
		int _readTo;

		public override int Read(byte[] buffer, int offset, int count)
		{
			try
			{
				Profiling.Stamp("SS Reader Wait");
				var b = _readerUnlock.WaitOne();
				if (!b) return 0;
				Profiling.Stamp("SS Reader Unlocked");
				if (_readTo > _readFrom)
				{
					var m = Math.Min(count, _readTo - _readFrom);
					Array.Copy(_readBuf, _readFrom, buffer, offset, m);
					_readFrom += m;
					if (_readFrom < _readTo)
					{
						// var rem = _readTo - _readFrom;
						// Array.Copy(_readBuf, _readFrom, _readBuf, 0, _readTo - _readFrom);
						// _readFrom = 0;
						// _readTo = rem;
					}
					else
					{
						_readFrom = 0;
						_readTo = 0;
						_readerUnlock.Reset();
						_readerFinished.Set();
					}
					return m;
				}
				return 0;
			}
			catch
			{
				// reset
				_readFrom = 0;
				_readTo = 0;
				_readerUnlock.Reset();
				_readerFinished.Set();

				Dispose();
				throw;
			}
			finally
			{
			}
		}

		public override void Close()
		{
			base.Close();
			_readerUnlock.Set();
			_readerFinished.Set();
		}
	}
}
