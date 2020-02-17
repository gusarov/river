using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace River.Internal
{
	public class ObjectTracker
	{
		public static ObjectTracker Default = new ObjectTracker();

		Timer _timer;

		protected ObjectTracker()
		{
			_timer = new Timer(Tick, null, 1000, 1000);
		}

		private void Tick(object state)
		{
			Maintain();
		}

		public void Maintain()
		{
			lock (_list)
			{
				var newList = _list.Where(x => x.IsAlive).ToList();
				if (newList.Count != _list.Count)
				{
					_list = new ConcurrentBag<WeakReference>(newList);
				}
			}
		}

		ConcurrentBag<WeakReference> _list = new ConcurrentBag<WeakReference>();

		bool _isEnabled
#if DEBUG
			= true
#endif
			;

		public void EnableCollection()
		{
			_isEnabled = true;
		}

		public void ResetCollection()
		{
			_list = new ConcurrentBag<WeakReference>();
		}

		// [Conditional("DEBUG")]
		public void Register<T>(T obj)
		{
			if (_isEnabled)
			{
				_list.Add(new WeakReference(obj));
			}
		}


		public int Count
		{
			get
			{
				return _list.Count;
			}
		}


		public IEnumerable<object> Items
		{
			get
			{
				return _list.Select(x => x.Target).ToArray();
			}
		}
	}

}
