using System;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;

namespace River.MouthService
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main(string[] args)
		{
			if (args.Any(x => string.Equals(x, "console", StringComparison.InvariantCultureIgnoreCase)))
			{
				Trace.Listeners.Add(new ConsoleTraceListener());
				var svc = new Service();
				svc.RunImpl();
				Console.WriteLine("Press <enter> to stop the service . . .");
				Console.ReadLine();
				svc.StopImpl();
			}
			else
			{
				ServiceBase.Run(new ServiceBase[]
				{
					new Service()
				});
			}
		}
	}
}
