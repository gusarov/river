namespace River
{
	public class SocksServerToRiverClient : SocksServer<SocksServerTunnelClientWorker>
	{
		public SocksServerToRiverClient(int listenPort, string riverHost, int riverPort) : base(listenPort)
		{
			
		}
	}
}