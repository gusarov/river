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

		internal void HandlerAdd(Handler handler)
		{
			var cnt = Interlocked.Increment(ref _handlersCount);
#if DEBUG
			Console.Title = $"Handlers: {cnt}";
#endif
		}

		internal void HandlerRemove(Handler handler)
		{
			var cnt = Interlocked.Decrement(ref _handlersCount);
#if DEBUG
			Console.Title = $"Handlers: {cnt}";
#endif
		}

		ConcurrentDictionary<int, (int, string)> _dic = new ConcurrentDictionary<int, (int, string)>();

		[Conditional("DEBUG")]
		public void MaxBufferUsage(int size, string from)
		{
			_dic.AddOrUpdate(size, (size, from), (s, c) =>
				s > c.Item1
				 ? (s, from)
				 : c
			);
		}

		
	}
}
