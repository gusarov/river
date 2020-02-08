using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using River.SourceService.Properties;

namespace River.SourceService
{
	public partial class Service : ServiceBase
	{
		public Service()
		{
			InitializeComponent();
		}

		protected override void OnStart(string[] args)
		{
			RunImpl();
		}

		protected override void OnStop()
		{
			StopImpl();
		}

		private SocksServerToRiverClient _server;


		public void RunImpl()
		{
			var outgoingInterface = string.IsNullOrWhiteSpace(Settings.Default.OutgoingInterfaceIP)
				? default(IPEndPoint)
				: new IPEndPoint(IPAddress.Parse(Settings.Default.OutgoingInterfaceIP), 0);

			var bw = Settings.Default.Bandwidth;
			if (bw <= 0)
			{
				bw = 1024 * 1024;
			}

			_server = new SocksServerToRiverClient(Settings.Default.ListeningPort, Settings.Default.RiverServers, outgoingInterface);
			_server.Bandwidth = bw;
		}

		public void StopImpl()
		{
			
		}
	}
}
