using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace River
{
	public static class ThreadExt
	{
		public static bool JoinDebug(this Thread th, int ms)
		{
			bool success;
			do
			{
				success = th.Join(ms);
			} while (!success && Debugger.IsAttached);
			return success;
		}

		public static void JoinAbort(this Thread th)
		{
			if (th is null)
			{
				return;
			}
			if (!th.IsAlive)
			{
				return;
			}
			if (th.ManagedThreadId == Thread.CurrentThread.ManagedThreadId)
			{
				return;
			}

			// big enough to be noticed in unit tests as a problem.
			// Small enough to gracefully shutdown in PROD
			var success = th.JoinDebug(10000);
			if (!success)
			{
				try
				{
					th.Abort();
				}
				catch { }
			}
		}
	}
}
