using System.Net;

namespace River
{
	public class DestinationIdentifier
	{
		public IPAddress IPAddress { get; set; }
		// OR
		public string Host { get; set; }

		// AND
		public int Port { get; set; }


		public IPEndPoint IPEndPoint => IPAddress != null ? new IPEndPoint(IPAddress, Port) : null;
	}
}