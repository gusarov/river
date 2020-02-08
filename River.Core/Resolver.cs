using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace River
{
	public static class Resolver
	{
		static Dictionary<string, Type> _schemasClient
			= new Dictionary<string, Type>(StringComparer.CurrentCultureIgnoreCase);

		static Dictionary<string, Type> _schemasServer
			= new Dictionary<string, Type>(StringComparer.CurrentCultureIgnoreCase);

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
	}
}
