using System;
using System.Collections.Generic;
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
	}
}
