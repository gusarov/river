using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;

namespace River
{
	public class SocksServerToRiverClient : SocksServer<SocksServerTunnelClientWorker>
	{
		public SocksServerToRiverClient(int listenPort, string riverHosts, IPEndPoint outgoingInterface = null)
			: base(listenPort)
		{
			RiverHosts = riverHosts;

			var hosts = RiverHosts.Split(';');
			foreach (var host in hosts)
			{
				var parts = host.Split(':');
				var serv = parts[0];
				var port = int.Parse(parts[1]);
				_riverHostsEntry.Add(new ServerEntry
				{
					EndPoint = new DnsEndPoint(serv, port),
				});
			}

			RotateServer();
			OutgoingInterface = outgoingInterface;
		}

		private static readonly Random _rmd = new Random();

		private DateTime _lastRotation;
		private readonly object _lockRotation = new object();

		public void RotateServer()
		{
			lock (_lockRotation)
			{
				var now = DateTime.UtcNow;
				if ((now - _lastRotation).TotalSeconds > 10)
				{
					_lastRotation = now;
					var server = _riverHostsEntry[_rmd.Next(_riverHostsEntry.Count)];
					var dnp = server.EndPoint;
					Trace.WriteLine(dnp.ToString());
					RiverHost = dnp.Host;
					RiverPort = dnp.Port;
				}
			}
		}

		public string RiverHost { get; set; }
		public int RiverPort { get; set; }

		public string RiverHosts { get; private set; }
		private readonly List<ServerEntry> _riverHostsEntry = new List<ServerEntry>();

		class ServerEntry
		{
			public DnsEndPoint EndPoint { get; set; }
			public DateTime LastError { get; set; }
		}

		public IPEndPoint OutgoingInterface { get; private set; }
	}
}