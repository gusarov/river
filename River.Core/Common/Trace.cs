using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace River
{
	public class Trace
	{
		public static void WriteLine(string str)
		{
			Console.WriteLine(str);
		}

		public static void TraceError(string str)
		{
			Console.WriteLine(str);
		}
	}

	public class Debug : Trace
	{

	}
}
