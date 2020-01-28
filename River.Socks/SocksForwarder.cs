using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace River.Socks
{
	public class SocksForwarder : Forwarder
	{
		public SocksForwarder(string host, int port)
		{
			Host = host;
			Port = port;
		}

		public string Host { get; }
		public int Port { get; }

		public override ForwardHandler CreateForwardHandler()
		{
			return new SocksForwardHandler(this);
		}

		protected override (byte[] buf, int pos, int cnt) PackCore(byte[] buf, int pos, int cnt)
		{
			return (buf, pos, cnt);
		}

	}
}