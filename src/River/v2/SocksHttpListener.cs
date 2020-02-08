using System.Net.Sockets;
using System.Threading.Tasks;

namespace River.v2
{
	public class SocksHttpListener : Listener
	{
		public SocksHttpListener(SocksHttpListenerData data)
			: base(data)
		{

		}

		protected override async Task Accept(TcpListener listener)
		{
			var client = await listener.AcceptTcpClientAsync();
			var stream = client.GetStream();
			_ = Accept(listener); // fork a new one

			_ = Handle(client, stream);
		}

		async Task Read(TcpClient client, NetworkStream stream)
		{
			stream.ReadAsync()
		}

	}
}
