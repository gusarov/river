using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace River
{
	/// <summary>
	/// Just create real terminating connection
	/// </summary>
	public sealed class NullForwarder : Forwarder
	{
		public override ForwardHandler CreateForwardHandler()
		{
			var handler = new NullForwardHandler(this);
			return handler;
		}

		protected override (byte[] buf, int pos, int cnt) PackCore(byte[] buf, int pos, int cnt)
		{
			// no enveloping
			return (buf, pos, cnt);
		}

	}
}
