using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace River
{
	public class Profiling
	{
		static Stopwatch _sw = new Stopwatch();

		public static void Start()
		{
			_sw = Stopwatch.StartNew();
		}

		[Conditional("DEBUG")]
		public static void Stamp(string line)
		{
			var s = _sw.ElapsedMilliseconds;
			if (s > 9999)
			{
				s = 9999;
			}
			Trace.WriteLine($"[G{s:0000}] {line}");
		}
	}

	public class ProfilingScope : IDisposable
	{
		private readonly string _name;
		private readonly ProfilingScope _parent;
		Stopwatch _sw = Stopwatch.StartNew();

		public ProfilingScope(string name, ProfilingScope parent = null)
		{
			_name = name;
			_parent = parent;
			if (_parent == null)
			{
				Level = 0;
			}
			else
			{
				Level = _parent.Level + 1;
			}
			Stamp("Start");
		}

		public int Level { get; }

		public void Stamp(string line)
		{
			Trace.WriteLine($"[{_sw.ElapsedMilliseconds:0000}]{new string('\t', Level)} {_name}: {line}");
		}

		public void Dispose()
		{
			Stamp("Stop");
		}
	}
}
