using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace River
{
	static class Resolver
	{
		static Dictionary<string, Type> _schemas = new Dictionary<string, Type>();

		public static void RegisterSchema<T>(string schema)
		{
			_schemas.Add(schema, typeof(T));
		}

		public static Type GetClientStreamType(Uri uri)
		{
			_schemas.TryGetValue(uri.Scheme, out var type);
			return type;
		}
	}
}
