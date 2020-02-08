using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using River.MouthService.Properties;

namespace River.MouthService
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

		private RiverServer _riverServer;
		private SocksProxyServer _proxyServer;

		public void StopImpl()
		{
			_riverServer?.Dispose();
			_proxyServer?.Dispose();
		}

		public void RunImpl()
		{
			_riverServer = new RiverServer(Settings.Default.Port, Settings.Default.Bypass);
			if (Settings.Default.SocksPort > 0)
			{
				_proxyServer = new SocksProxyServer(Settings.Default.SocksPort);
			}
		}
	}
}
