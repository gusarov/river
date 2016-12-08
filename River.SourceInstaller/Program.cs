
[assembly: TinyInstaller.InstallerIdentity("River Source Service")]
[assembly: TinyInstaller.InstallUserMode(false)]
[assembly: TinyInstaller.InstallUtilsAssembly(assembly: typeof(River.SourceService.Service))]

namespace River.MouthInstaller
{
	class Program
	{
		static void Main()
		{
			TinyInstaller.EntryPoint.GuiRunWith("River Source Service");
		}
	}
}
