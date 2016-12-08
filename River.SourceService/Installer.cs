using System.Collections;
using System.ComponentModel;
using System.ServiceProcess;

namespace River.SourceService
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
