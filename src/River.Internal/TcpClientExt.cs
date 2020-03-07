using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace River
{
	public static partial class TcpClientExt
	{
		public static void Configure(this TcpClient client)
		{
			if (client is null)
			{
				throw new ArgumentNullException(nameof(client));
			}
			ObjectTracker.Default.Register(client);
			client.Client.NoDelay = true;
		}

		/// <summary>
		/// Get NetworkStream with fast PUSH_IMMEDIATE reads
		/// </summary>
		public static Stream GetStream2(this TcpClient client)
		{
			if (client is null)
			{
				throw new ArgumentNullException(nameof(client));
			}

			return new NetworkStream2(client, true);
		}
	}

}
