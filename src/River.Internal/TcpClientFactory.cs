using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace River
{
	public static class TcpClientFactory
	{
		// This class remembers AddressFamily for host name...
		// This helps to fight HappyEyeballs from new TcpClient(string, int)

		static ConcurrentDictionary<string, (IPAddress to, AddressFamily fam, DateTime at)> _dic
			= new ConcurrentDictionary<string, (IPAddress to, AddressFamily fam, DateTime at)>(StringComparer.OrdinalIgnoreCase);

		static int _cachedForMinutes = 240; // 4 hours

		public static TcpClient Create(string host, int port)
		{
			if (host is null)
			{
				throw new ArgumentNullException(nameof(host));
			}

			var now = DateTime.UtcNow;
			TcpClient cli;
			if (_dic.TryGetValue(host.ToUpperInvariant(), out var known))
			{
				if ((known.at - now).TotalMinutes < _cachedForMinutes)
				{
					cli = new TcpClient(known.fam);
					cli.Connect(host, port); // we are not caching IP, only family!! This effectively disables HappyEyeballs discovery delays
					return cli;
				}
			}
			cli = new TcpClient(host, port); // this constructor
			var ep = (IPEndPoint)cli.Client.RemoteEndPoint;
			_dic[host] = (ep.Address, ep.AddressFamily, now);
			return cli;
		}
	}
}
