using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace River.Common
{
	/// <summary>
	/// This calss allows to determine if current debug session is in progress
	/// </summary>
	public class DebuggerTracker
	{
		const int _max = 5;
		double maxSum(int count)
		{
			if (count == 0)
			{
				return 0.5; // extra for stability
			}
			return 1 + maxSum(count - 1);
		}

		Timer _timer;

		public DebuggerTracker()
		{
			_timer = new Timer(Callback, null, 0, 1000);
		}

		LinkedList<DateTime> _lastCallbacks = new LinkedList<DateTime>();

		private void Callback(object state)
		{
			lock (_lastCallbacks)
			{
				_lastCallbacks.AddLast(DateTime.UtcNow);
				if (_lastCallbacks.Count > _max)
				{
					_lastCallbacks.RemoveFirst();
				}
			}
		}

		[DebuggerStepThrough]
		public async Task NormalAsync()
		{
			double sd, ms;
			while (!IsNormal(out sd, out ms)) // white till exit from debugger
			{
				Console.WriteLine($"Waiting for finishing debugger... {sd:0.0} {ms:0.0}");
				await Task.Delay(2000);
			}
		}

		[DebuggerStepThrough]
		public bool IsNormal()
		{
			double sd, ms;
			return IsNormal(out sd, out ms);
		}

		[DebuggerStepThrough]
		public bool IsNormal(out double sd, out double ms)
		{
			// expect
			// 4-5 sec ago
			// 3-4 sec ago
			// 2-3 sec ago
			// 1-2 sec ago
			// 0-1 sec ago

			var now = DateTime.UtcNow;

			int cnt;
			lock (_lastCallbacks) {
				cnt = _lastCallbacks.Count;
				sd = _lastCallbacks.Sum(x => (now - x).TotalSeconds);
			}

			ms = maxSum(cnt);
			return sd < ms;
		}
	}
}
