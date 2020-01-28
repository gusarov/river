using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace River
{
	public abstract class Forwarder
	{
		public Forwarder NextForwarder { get; set; }

		public abstract ForwardHandler CreateForwardHandler();

		protected internal (byte[] buf, int pos, int cnt) Pack(byte[] buf, int pos, int cnt)
		{
			if (NextForwarder != null)
			{
				(buf, pos, cnt) = NextForwarder.Pack(buf, pos, cnt);
			}
			return PackCore(buf, pos, cnt);
		}

		protected abstract (byte[] buf, int pos, int cnt) PackCore(byte[] buf, int pos, int cnt);
	}
}