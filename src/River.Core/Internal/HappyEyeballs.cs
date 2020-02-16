using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace River.Internal
{
	public class HappyEyeballs
	{
		public EndPoint GetPreferredEndpoint(string nodeKey, string host, int port, bool? allowDns = null)
		{
			if (IPAddress.TryParse(host, out var address))
			{
				return new IPEndPoint(address, port);
			}

			var candidates = new List<EndPoint>
			{
				new DnsEndPoint(host, port)
			};
			if (allowDns != false)
			{
				return candidates[0];
			}

			var entries = Dns.GetHostAddresses(host);
			var ipv6a = entries.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetworkV6);
			var ipv4a = entries.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
			var ipv6 = new IPEndPoint(ipv6a, port);
			var ipv4 = new IPEndPoint(ipv4a, port);

			candidates.Add(ipv6);
			candidates.Add(ipv4);

			return ipv6 ?? ipv4;
		}
	}
}
