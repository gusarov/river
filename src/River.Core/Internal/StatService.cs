using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace River.Internal
{
	public class StatService
	{
		public static StatService Instance { get; } = new StatService();

		private StatService()
		{
		
		}

		int _handlersCount;
		public int HandlersCount { get => _handlersCount; set => _handlersCount = value; }

		// Dictionary<int, (int, string)> _dicn = new Dictionary<int, (int, string)>();

		[Conditional("DEBUG")]
		public void MaxBufferUsage(int size, string from)
		{
		}

		
	}
}
