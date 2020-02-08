using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace River.v2
{

	public class SocksHttpListenerData : ListenerData
	{
	}

	public class ForwarderConnectionData
	{
		public IPEndPoint IPEndPoint;
		public DnsEndPoint DnsEndPoint;
	}

	public abstract class Forwarder
	{
		public Forwarder()
		{

		}

		public Forwarder(ForwarderConnectionData data)
		{
			Connect(data);
		}

		public abstract void Connect(ForwarderConnectionData data);
		public abstract void Send(byte[] buf, int offset, int size);
	}

	public class TransparentForwarder : Forwarder
	{
		TcpClient _client;
		NetworkStream _stream;

		public override void Connect(ForwarderConnectionData data)
		{
			_client = new TcpClient();
			_stream = _client.GetStream();
			if (data.IPEndPoint != null)
			{
				_client.Connect(data.IPEndPoint);
			}
			else if (data.DnsEndPoint != null)
			{
				_client.Connect(data.DnsEndPoint.Host, data.DnsEndPoint.Port);
			}
			else
			{
				throw new Exception("No connection data");
			}
		}

		public override void Send(byte[] buf, int offset, int size)
		{
			if (_stream == null)
			{
				throw new InvalidOperationException("Client is not connected");
			}
			_stream.Write(buf, offset, size);
		}
	}
}
