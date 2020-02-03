using System.Collections.Generic;
using System.Net;

namespace River
{
	public class ServerConfig
	{
		public IList<IPEndPoint> EndPoints { get; set; } = new List<IPEndPoint>();
	}
}
