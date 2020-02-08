using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace River.MouthService
{
	[RunInstaller(true)]
	public partial class Installer : System.Configuration.Install.Installer
	{
		public Installer()
		{
			InitializeComponent();
		}
		protected override void OnAfterInstall(IDictionary savedState)
		{
			base.OnAfterInstall(savedState);
			try
			{
				using (var sc = new ServiceController(serviceInstaller1.ServiceName))
				{
					sc.Start();
				}
			}
			catch { }
		}

		protected override void OnBeforeUninstall(IDictionary savedState)
		{
			try
			{
				using (var sc = new ServiceController(serviceInstaller1.ServiceName))
				{
					sc.Stop();
				}
			}
			catch { }
			base.OnBeforeUninstall(savedState);
		}
	}
}
