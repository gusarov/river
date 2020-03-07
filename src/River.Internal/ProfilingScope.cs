using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace River
{
	public static class Profiling
	{
		static Stopwatch _sw = new Stopwatch();

		static Trace Trace = River.Trace.Default;

		public static void Start()
		{
			_sw = Stopwatch.StartNew();
		}

		[Conditional("DEBUG")]
		public static void Stamp(string message)
		{
			Stamp(TraceCategory.Performance, message);
		}

		[Conditional("DEBUG")]
		public static void Stamp(TraceCategory category, string message)
		{
			var s = _sw.ElapsedMilliseconds;
			if (s > 9999)
			{
				s = 9999;
			}
			Trace.WriteLine(category, $"[G{s:0000}] {message}");
		}
	}

	public class ProfilingScope : IDisposable
	{
		static Trace Trace = River.Trace.Default;

		private readonly TraceCategory _traceCategory;
		private readonly string _name;
		private readonly ProfilingScope _parent;
		Stopwatch _sw = Stopwatch.StartNew();

		public ProfilingScope(string name, ProfilingScope parent = null)
			: this(TraceCategory.Performance, name, parent)
		{
		}

		public ProfilingScope(TraceCategory traceCategory, string name, ProfilingScope parent = null)
		{
			_traceCategory = traceCategory;
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

		public void Stamp(string message)
		{
			Trace.WriteLine(_traceCategory, $"[{_sw.ElapsedMilliseconds:0000}]{new string('\t', Level)} {_name}: {message}");
		}

		public void Dispose()
		{
			Stamp("Stop");
		}
	}
}
