using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace River
{
	public class Utils
	{
		public const int MaxHeaderSize = 1024 * 8;

		public static IDictionary<string, string> TryParseHttpHeader(byte[] buffer, int pos, int length)
		{
			int eoh;
			string headerString;
			return TryParseHttpHeader(buffer, pos, length, out eoh, out headerString);
		}

		public static IDictionary<string, string> TryParseHttpHeader(byte[] buffer, int pos, int length, out int eoh)
		{
			string headerString;
			return TryParseHttpHeader(buffer, pos, length, out eoh, out headerString);
		}

		private static readonly Regex _requestLineParser = new Regex(@"(?ix)^
(?'v'\w+)\s+ # VERB
(?'u' # URL
  ((?'p'https?):\/\/)? # protocol
  (?'h'[\d_a-z\.-]+)? # host
  (:(?'pr'\d+))? # port
  /?
  [^\s]* # other arguments
)\s+
HTTP/(?'hv'\d\.\d)# http ver

", RegexOptions.Compiled |RegexOptions.ExplicitCapture);

		public static IDictionary<string, string> TryParseHttpHeader(byte[] buffer, int pos, int length, out int eoh, out string headerString)
		{
			headerString = Encoding.ASCII.GetString(buffer, pos, length > MaxHeaderSize ? MaxHeaderSize : length);
			eoh = headerString.IndexOf("\r\n\r\n") + 4;
			if (eoh > 0)
			{
				var headers = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
				for (int i = 0; i < eoh - 4;)
				{
					var start = i;
					i = headerString.IndexOf("\r\n", i + 1) + 2;
					var sp = headerString.IndexOf(':', start);
					if (start == 0)
					{
						// this is first line
						var match = _requestLineParser.Match(headerString, 0, i);
						headers["_verb"] = match.Groups["v"].Value;
						headers["_url"] = match.Groups["u"].Value;
						headers["_url_host"] = match.Groups["h"].Value;
						headers["_url_port"] = match.Groups["hp"].Value;
						headers["_http_ver"] = match.Groups["hv"].Value;
					}
					if (sp <= i)
					{
						var headerKey = headerString.Substring(start, sp - start).Trim();
						var headerValue = headerString.Substring(sp + 1, i - sp - 1).Trim();
						headers[headerKey] = headerValue.Trim();
					}
				}
				return headers;
			}
			return null;
		}
	}
}
