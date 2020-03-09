using RiverApp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace River
{
	class ShutdownRequestTracker
	{
		private const string _eventWaitHandleName = "_river_shutdown_message_";

		public static ShutdownRequestTracker Instance { get; } = new ShutdownRequestTracker();

		ShutdownRequestTracker()
		{

		}

		HashSet<string> _names = new HashSet<string>();

		public void RequestStop(string name)
		{
			using var ev = new EventWaitHandle(false, EventResetMode.AutoReset, _eventWaitHandleName + name);
			ev.Set();
		}

		public void AddTracker(string name)
		{
			bool n;
			lock (_names)
			{
				n = _names.Add(name);
			}

			if (n)
			{
				Task.Run(delegate
				{
					using var ev = new EventWaitHandle(false, EventResetMode.AutoReset, _eventWaitHandleName + name);
					ev.WaitOne();
					Console.WriteLine("Stop requested...");
					DisposeAll();

					Console.WriteLine("Waiting 30 sec for stop...");

					for (int i = 0; i < 30 / 3; i++)
					{
						GC.Collect();
						if (ObjectTracker.Default.Count != 0)
						{
							Console.WriteLine("Waiting for...");
							foreach (var item in ObjectTracker.Default.Entries)
							{
								Console.WriteLine(item.Details);
							}
							Console.WriteLine();
							Thread.Sleep(3 * 1000);
						}
						else
						{
							// Process.GetCurrentProcess().Kill();
							// return;
						}
					}

					Console.WriteLine("Kill...");
					Process.GetCurrentProcess().Kill();
				});
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void DisposeAll()
		{
			foreach (var item in ObjectTracker.Default.Get<RiverServer>())
			{
				item.Dispose();
			}
			foreach (var item in ObjectTracker.Default.Get<Handler>())
			{
				item.Dispose();
			}
		}
	}
}
