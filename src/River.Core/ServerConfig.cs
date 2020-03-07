using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Web;

namespace River
{
	public class ServerConfig
	{
		public Uri Uri { get; private set; }

		public ServerConfig()
		{
		}

		public ServerConfig(string uri)
			: this(new Uri(uri))
		{
		}

		public ServerConfig(Uri uri)
		{
			Uri = uri;

			EndPoints.Add(new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port));
		}

		NameValueCollection _parsed;

		public string this[string key]
		{
			get
			{
				if (_parsed == null)
				{
					_parsed = HttpUtility.ParseQueryString(Uri.Query);
				}
				if (key == "user" || key == "password")
				{
					var user = Uri.UserInfo;
					var i = user.IndexOf(':');
					if (i >= 0)
					{
						if (key == "user")
						{
							return user.Substring(0, i);
						}
						else
						{
							return user.Substring(i + 1);
						}
					}
					return user;
				}
				return _parsed.Get(key);
			}
		}

		public static implicit operator ServerConfig(string uri)
		{
			return new ServerConfig(uri);
		}

		public static implicit operator ServerConfig(Uri uri)
		{
			return new ServerConfig(uri);
		}

		public static ServerConfig From(string uri)
		{
			return new ServerConfig(uri);
		}

		public static ServerConfig From(Uri uri)
		{
			return new ServerConfig(uri);
		}


		public IList<IPEndPoint> EndPoints { get; } = new List<IPEndPoint>();
	}

	public class UserCredentials
	{

	}
}
