
[assembly: TinyInstaller.InstallerIdentity("River Mouth Service")]
[assembly: TinyInstaller.InstallUserMode(false)]
[assembly: TinyInstaller.InstallUtilsAssembly(assembly: typeof(River.MouthService.Service))]

namespace River.MouthInstaller
{
	class Program
	{
		static void Main()
		{
			TinyInstaller.EntryPoint.GuiRunWith("River Mouth Service");
		}
	}
}
