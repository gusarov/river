namespace River
{
	public class SocksServerToRiverClient : SocksServer<SocksServerTunnelClientWorker>
	{
		public SocksServerToRiverClient(int listenPort, string riverHost, int riverPort) : base(listenPort)
		{
			RiverHost = riverHost;
			RiverPort = riverPort;
		}

		public string RiverHost { get; private set; }

		public int RiverPort { get; private set; }
	}
}