using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace River.Test
{
	/// <summary>
	/// Keep track of IDisposable
	/// </summary>
	public static class Tracker
	{
		static ConcurrentDictionary<object, List<IDisposable>> _dic = new ConcurrentDictionary<object, List<IDisposable>>();

		/// <summary>
		/// Remember IDisposable object
		/// </summary>
		public static T Track<T>(this T item, object state) where T : IDisposable
		{
			var list = _dic.GetOrAdd(state, _ => new List<IDisposable>());
			list.Add(item);
			return item;
		}

		/// <summary>
		/// Dispose all objects associated with this state
		/// </summary>
		public static void Explode(object state)
		{
			if (_dic.TryGetValue(state, out var list))
			{
				foreach (var item in list)
				{
					try
					{
						item.Dispose();
					}
					catch { }
				}
			}
			_dic.TryRemove(state, out var _);
		}

		public static void Explode()
		{
			var states = _dic.Keys.ToArray();
			foreach (var state in states)
			{
				Explode(state);
			}
		}
	}
}
