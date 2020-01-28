using System.IO;
using System.Net.Sockets;

namespace River
{
	public sealed class NullForwardHandler : ForwardHandler
	{
		public NullForwardHandler(Forwarder forwarder) : base(forwarder)
		{
		}

		protected override Stream EstablishConnectionCore(DestinationIdentifier destination)
		{
			TcpClient client;
			if (destination.IPEndPoint != null)
			{
				client = new TcpClient(destination.IPEndPoint);
			}
			else
			{
				client = new TcpClient(destination.Host, destination.Port);
			}
			client.Client.NoDelay = true;

			var stream = client.GetStream();

			return stream;
		}
	}
}
