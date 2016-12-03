using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

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

		public void StopImpl()
		{
			_riverServer?.Dispose();
		}

		public void RunImpl()
		{
			_riverServer = new RiverServer(80);
		}
	}
}
