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
		const double _maxSum = 15.5;

		/*
		static double MaxSum(int count)
		{
			if (count == 0)
			{
				return 0.5; // extra for stability
			}
			return 1 + MaxSum(count - 1);
		}
		*/

		Timer _timer;

		public DebuggerTracker()
		{
			_timer = new Timer(Callback, null, 0, 1000);

			// initialize a _lastCallbacks window
			var now = DateTime.UtcNow;
			for (var i = 0; i < _max; i++)
			{
				_lastCallbacks[i++] = now.AddSeconds(i - _max);
			}
		}

		~DebuggerTracker()
		{
			try
			{
				_timer?.Change(Timeout.Infinite, Timeout.Infinite);
			}
			catch { }
		}

		DateTime[] _lastCallbacks = new DateTime[_max];
		int _lastCallbacksIndex;

		private void Callback(object state)
		{
			lock (_lastCallbacks)
			{
				_lastCallbacks[_lastCallbacksIndex++] = DateTime.UtcNow;
				if (_lastCallbacksIndex >= _max)
				{
					_lastCallbacksIndex = 0;
				}
			}
		}

		[DebuggerStepThrough]
		public async Task EnsureNoDebuggerAsync()
		{
			double sd;
			while (!IsNoDebugger(out sd)) // white till exit from debugger
			{
				Console.WriteLine($"Waiting for finishing debugger... {sd:0.0}");
				await Task.Delay(2000);
			}
		}

		[DebuggerStepThrough]
		public bool IsNoDebugger()
		{
			double sd;
			return IsNoDebugger(out sd);
		}

		[DebuggerStepThrough]
		public bool IsNoDebugger(out double sd)
		{
			// expect
			// 4-5 sec ago
			// 3-4 sec ago
			// 2-3 sec ago
			// 1-2 sec ago
			// 0-1 sec ago

			var now = DateTime.UtcNow;

			lock (_lastCallbacks)
			{
				sd = _lastCallbacks.Sum(x => (now - x).TotalSeconds);
			}

			return sd < _maxSum;
		}
	}
}
