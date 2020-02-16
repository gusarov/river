using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace River
{
	public class Trace
	{
		[Conditional("DEBUG")]
		public static void WriteLine(string str)
		{
			Console.WriteLine(str);
		}

		[Conditional("DEBUG")]
		public static void TraceError(string str)
		{
			Console.WriteLine(str);
		}
	}

	public class Debug : Trace
	{
		[Conditional("DEBUG")]
		public static void Assert(bool condition)
		{
			if (!condition) throw new Exception();
		}
	}
}
