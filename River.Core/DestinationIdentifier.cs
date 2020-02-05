using System;
using System.Net;

namespace River
{
	public class ProxyIdentifier
	{
		public Uri Uri { get; }

		public ProxyIdentifier(Uri uri)
		{
			Uri = uri;
		}

		public static implicit operator ProxyIdentifier(Uri uri)
		{
			return new ProxyIdentifier(uri);
		}

		public static implicit operator ProxyIdentifier(string uri)
		{
			return new ProxyIdentifier(new Uri(uri));
		}

		public static ProxyIdentifier FromUri(Uri uri)
		{
			return new ProxyIdentifier(uri);
		}

		public static ProxyIdentifier FromString(string str)
		{
			return new ProxyIdentifier(new Uri(str));
		}
	}

	public class DestinationIdentifier
	{
		#region Factory

		public DestinationIdentifier()
		{

		}

		public DestinationIdentifier(string host, int port)
		{
			Host = host;
			Port = port;
		}

		public DestinationIdentifier(IPAddress ip, int port)
		{
			IPAddress = ip;
			Port = port;
		}

		public static implicit operator DestinationIdentifier((string host, int port) pair)
		{
			return new DestinationIdentifier(pair.host, pair.port);
		}

		public static DestinationIdentifier From((string host, int port) pair)
		{
			return new DestinationIdentifier(pair.host, pair.port);
		}

		public static implicit operator DestinationIdentifier((IPAddress ip, int port) pair)
		{
			return new DestinationIdentifier(pair.ip, pair.port);
		}

		public static DestinationIdentifier From((IPAddress ip, int port) pair)
		{
			return new DestinationIdentifier(pair.ip, pair.port);
		}

		#endregion

		#region Data

		public IPAddress IPAddress { get; set; }
		// OR
		public string Host { get; set; }

		// AND
		public int Port { get; set; }


		public IPEndPoint IPEndPoint => IPAddress != null
			? new IPEndPoint(IPAddress, Port)
			: null;

		#endregion
	}
}