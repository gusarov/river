using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace River
{
	/// <summary>
	/// Just create real direct connection
	/// </summary>
	sealed class NullClientStream : ClientStream
	{
		public override void Route(Uri uri) => throw new Exception($"Where are you going to route? This is {nameof(NullClientStream)}");
	}
}
