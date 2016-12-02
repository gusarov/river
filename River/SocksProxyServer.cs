namespace River
{
	public class SocksProxyServer : SocksServer<SocksServerProxyClientWorker>
	{
		public SocksProxyServer(int listenPort) : base(listenPort)
		{

		}
	}
}