using System.Net;

namespace River
{
	public class SocksServerToRiverClient : SocksServer<SocksServerTunnelClientWorker>
	{
		public SocksServerToRiverClient(int listenPort, string riverHost, int riverPort, IPEndPoint outgoingInterface = null)
			: base(listenPort)
		{
			RiverHost = riverHost;
			RiverPort = riverPort;
			OutgoingInterface = outgoingInterface;
		}

		public string RiverHost { get; private set; }

		public int RiverPort { get; private set; }

		public IPEndPoint OutgoingInterface { get; private set; }
	}
}