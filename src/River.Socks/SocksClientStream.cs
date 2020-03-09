using River.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace River.Socks
{
	public abstract class SocksClientStream : ClientStream
	{
		protected override int GetDefaultPort(string scheme)
		{
			if (scheme != "socks")
			{
				throw new NotSupportedException($"scheme {scheme} is not supported by SOCKS");
			}
			return 1080; // this is part of RFC, so, should be handled from URI
		}
	}
}
