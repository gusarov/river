using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace River
{
	public static class Resolver
	{
		static List<(Regex regex, Func<string, Stream> fact)> _overriders = new List<(Regex, Func<string, Stream>)>();

		static Dictionary<string, Type> _schemasClient
			= new Dictionary<string, Type>(StringComparer.CurrentCultureIgnoreCase);

		static Dictionary<string, Type> _schemasServer
			= new Dictionary<string, Type>(StringComparer.CurrentCultureIgnoreCase);

		public static void RegisterOverride(string hostPattern, Func<string, Stream> fact)
		{
			_overriders.Add((new Regex(hostPattern, RegexOptions.Compiled | RegexOptions.ExplicitCapture), fact));
		}

		public static void RegisterSchema<TServer, TClient>(string schema)
		{
			_schemasClient.Add(schema, typeof(TClient));
			_schemasServer.Add(schema, typeof(TServer));
		}

		public static void RegisterSchemaClient<TClient>(string schema)
		{
			_schemasClient.Add(schema, typeof(TClient));
		}

		public static void RegisterSchemaServer<TServer>(string schema)
		{
			_schemasServer.Add(schema, typeof(TServer));
		}

		public static Type GetClientType(Uri uri)
		{
			if (uri is null)
			{
				throw new ArgumentNullException(nameof(uri));
			}

			_schemasClient.TryGetValue(uri.Scheme, out var type);
			return type;
		}

		public static Type GetServerType(Uri uri)
		{
			if (uri is null)
			{
				throw new ArgumentNullException(nameof(uri));
			}

			_schemasServer.TryGetValue(uri.Scheme, out var type);
			return type;
		}

		public static Stream GetStreamOverride(string hostName)
		{
			
			if (!string.IsNullOrEmpty(hostName))
			{
				foreach (var ov in _overriders)
				{
					if (ov.regex.IsMatch(hostName))
					{
						return ov.fact(hostName);
					}
				}
			}
			return null;
		}
	}
}
