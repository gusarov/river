using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace River
{
	public static class ThreadExt
	{
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

			// big enough to be noticed in unit tests as a problem.
			// Small enough to gracefully shutdown in PROD
			var success = th.Join(10000);
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
