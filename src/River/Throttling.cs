using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace River
{
	public class Throttling
	{
		public static readonly Throttling Default = new Throttling();

		/// <summary>
		/// The number of bytes that has been transferred since the last throttle.
		/// </summary>
		private long _byteCount;

		/// <summary>
		/// The start time in milliseconds of the last throttle.
		/// </summary>
		private readonly Stopwatch _start = Stopwatch.StartNew();

		public long Bandwidth { get; set; } = 1024 * 1024;

		public void Throttle(long count)
		{
			Trace.WriteLine($"Analyze {_byteCount} bytes {GetHashCode():X}");
			_byteCount += count;
			long elapsedMilliseconds = _start.ElapsedMilliseconds;

			if (elapsedMilliseconds > 0)
			{
				// Calculate the current bps.
				long bps = _byteCount * 1000L / elapsedMilliseconds;

				// If the bps are more then the maximum bps, try to throttle.
				if (bps > Bandwidth)
				{
					// Calculate the time to sleep.
					long wakeElapsed = _byteCount * 1000L / Bandwidth;
					int toSleep = (int)(wakeElapsed - elapsedMilliseconds);
					if (toSleep > 5000)
					{
						toSleep = 5000;
					}

					if (toSleep > 1)
					{
						Trace.WriteLine($"Throttle {toSleep}ms {GetHashCode():X}");
						try
						{
							// The time to sleep is more then a millisecond, so sleep.
							Thread.Sleep(toSleep);
						}
						catch (ThreadAbortException)
						{
							// Eatup ThreadAbortException.
						}

						// A sleep has been done, reset.
						// Only reset counters when a known history is available of more then 1 second.
						if (elapsedMilliseconds + toSleep > 1000)
						{
							_byteCount = 0;
							_start.Restart();
						}
					}
				}
				if (elapsedMilliseconds > 5000)
				{
					_byteCount = 0;
					_start.Restart();
				}
			}
		}
	}
}
