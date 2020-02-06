using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace River
{
	public static class Resolver
	{
		static Dictionary<string, (Type client, Type server)> _schemas
			= new Dictionary<string, (Type, Type)>(StringComparer.CurrentCultureIgnoreCase);

		public static void RegisterSchema<TClient, TServer>(string schema)
		{
			_schemas.Add(schema, (typeof(TClient), typeof(TServer)));
		}

		public static Type GetClientType(Uri uri)
		{
			_schemas.TryGetValue(uri.Scheme, out var types);
			return types.client;
		}

		public static Type GetServerType(Uri uri)
		{
			_schemas.TryGetValue(uri.Scheme, out var types);
			return types.server;
		}
	}
}
