using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

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
					_list = newList;
				}
			}
		}

		List<WeakReference> _list = new List<WeakReference>();

		// [Conditional("DEBUG")]
		public void Register<T>(T obj)
		{
			_list.Add(new WeakReference(obj));
		}


		public int Count
		{
			get
			{
				lock (_list)
				{
					return _list.Count;
				}
			}
		}


		public IEnumerable<object> Items
		{
			get
			{
				lock (_list)
				{
					return _list.Select(x => x.Target).ToArray();
				}
			}
		}
	}

}
