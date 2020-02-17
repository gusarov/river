using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace River
{
	class Dns
	{
		public static IPAddress[] GetHostAddresses(string targetHost)
		{
			switch (targetHost)
			{
				case "_river":
					return new[] { new IPAddress(new byte[] { 127, 127, 127, 127 }) };
				default:
					return System.Net.Dns.GetHostAddresses(targetHost);
			}
		}
	}
}
