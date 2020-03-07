#if DEBUG
#define TIMER
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.IO;

namespace River
{
	public interface ICustomStringifyProvider
	{
		string ToString(object item);
	}

	class DefaultStringifyProvider : ICustomStringifyProvider
	{
		public string ToString(object item)
		{
			if (item is Thread th)
			{
				return th.Name + " " + th.ThreadState;
			}

			if (item is TcpClient c)
			{
				return c?.Client?.RemoteEndPoint?.ToString();
			}

			return null;
		}
	}

	public static class Stringify
	{
		static List<ICustomStringifyProvider> _provider = new List<ICustomStringifyProvider>
		{
			new DefaultStringifyProvider(),
		};
		
		public static void Register<T>() where T : ICustomStringifyProvider, new()
		{
			_provider.Add(new T());
		}

		public static string ToString(object item, bool wrapExceptions = false)
		{
			if (ReferenceEquals(item, null))
			{
				return null;
			}

			try
			{
				foreach (var provider in _provider)
				{
					var stringify = provider.ToString(item);
					if (!string.IsNullOrEmpty(stringify))
					{
						return stringify;
					}
				}

				return item.ToString();
			}
			catch (Exception ex)
			{
				return ex.GetType().Name + " " + ex.Message;
			}
		}
	}

	public class ObjectTracker
	{
		public class Entry
		{
			public long Id { get; }
			public DateTime Utc { get; }
			public WeakReference WeakReference { get; }
			public int Level { get; }
			public Type Type { get; }

			private DateTime _lastGetDetails;

			private string _details;
			public string Details
			{
				get
				{
					var now = DateTime.UtcNow;
					var obj = WeakReference.Target;
					bool mightBeException = false;
					if (now - _lastGetDetails > TimeSpan.FromSeconds(5) && obj != null)
					{
						_lastGetDetails = now;

						var details = Stringify.ToString(obj);
						if (!string.IsNullOrEmpty(details))
						{
							_details = details;
						}
						else
						{
							mightBeException = true;
						}
						// _details = details?.GetLifecycleDetails() ?? obj?.ToString();
						// if (details == null) _lastGetDetails = new DateTime(9000, 1, 1); // interface not implemented
					}
					return _details + (mightBeException ? " " + Stringify.ToString(obj, true) : string.Empty);
				}
			}

			public Entry(long id, object item, int level)
			{
				Id = id;
				Utc = DateTime.UtcNow;
				WeakReference = new WeakReference(item ?? throw new ArgumentNullException(nameof(item)));
				Level = level;
				Type = item.GetType();
			}
		}


		public static ObjectTracker Default
		{
			get { return _default; }
			set
			{
#if TIMER
				if (_default != null)
				{
					_default._timer.Change(Timeout.Infinite, Timeout.Infinite);
					_default._timer.Dispose();
				}
#endif
				_default = value;
			}
		}

#if TIMER

		Timer _timer;

		internal protected ObjectTracker()
		{
			_timer = new Timer(Tick, null, 1000, Timeout.Infinite);
		}

		private void Tick(object state)
		{
			Maintain();
		}
#endif

		public void Maintain()
		{
			try
			{
				_lock.EnterWriteLock();

				foreach (var entry in _list.ToArray())
				{
					if (!entry.WeakReference.IsAlive)
					{
						_list.Remove(entry);
					}
				}

			}
			finally
			{
				_lock.ExitWriteLock();
#if TIMER
				_timer.Change(1000, Timeout.Infinite); // schedule again
#endif
			}

			/*
			var extra = "";
			foreach (var type in TypesToPrint)
			{
				extra += $", {CountOf(type)} {type.Name}s";
			}

			Console.Title = $"{CountOf<TcpClient>()} TcpClients, {CountOf<Stream>()} Streams{extra}, {CountOf<Thread>()} Threads Obj, {Process.GetCurrentProcess().Threads.Count} Threads in proc, {DateTime.Now: HH:mm:ss}";
			*/
		}

		long _nextId;

		ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
		readonly LinkedList<Entry> _list = new LinkedList<Entry>();

		bool _isAllEnabled
#if DEBUG
			= true
#endif
			;
		private static ObjectTracker _default = new ObjectTracker();

		public void EnableCollection()
		{
			_isAllEnabled = true;
		}

		/*
		public void ResetCollection()
		{
			_list = new ConcurrentBag<Entry>();
		}
		*/

		/// <summary>
		/// Track this object lifecycle
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="item">object to track</param>
		/// <param name="always">true if actually part of production and should be tracked event if not requested (e.g. to publish statistics)</param>
		public void Register<T>(T item, int level = 0, bool always = false)
		{
			if (always || _isAllEnabled)
			{
				var id = Interlocked.Increment(ref _nextId);
				_lock.EnterWriteLock();
				try
				{
#if DEBUG
					foreach (var entry in _list)
					{
						if (ReferenceEquals(entry.WeakReference.Target, item))
						{
							throw new InvalidOperationException("Object already registered");
						}
					}
#endif

					_list.AddLast(new Entry(Interlocked.Increment(ref _nextId), item, level));
				}
				finally
				{
					_lock.ExitWriteLock();
				}
				if ((id & byte.MaxValue) == 0) // every 256th item (0.4% calls)
				{
					Maintain();
				}
			}
		}

		public IEnumerable<T> Get<T>() where T : class
		{
			_lock.EnterReadLock();
			try
			{
				return _list
					.Where(x => typeof(T).IsAssignableFrom(x.Type))
					.Select(x => (T)x.WeakReference.Target)
					.Where(x => x != null)
					.ToArray();
			}
			finally
			{
				_lock.ExitReadLock();
			}
		}

		public int CountOf<T>()
		{
			return CountOf(typeof(T));
		}

		public int CountOf(Type type)
		{
			_lock.EnterReadLock();
			try
			{
				return _list
					.Where(x => type.IsAssignableFrom(x.Type) && x.WeakReference.IsAlive)
					.Count();
			}
			finally
			{
				_lock.ExitReadLock();
			}
		}

		public int Count
		{
			get
			{
				_lock.EnterReadLock();
				try
				{
					return _list
						.Where(x => x.WeakReference.IsAlive)
						.Count();
				}
				finally
				{
					_lock.ExitReadLock();
				}
			}
		}

		public IEnumerable<object> Items
		{
			get
			{
				_lock.EnterReadLock();
				try
				{
					return _list
						.Select(x => x.WeakReference.Target)
						.Where(x => x != null)
						.ToArray();
				}
				finally
				{
					_lock.ExitReadLock();
				}
			}
		}

		public IEnumerable<WeakReference> Weaks
		{
			get
			{
				_lock.EnterReadLock();
				try
				{
					// no need to filter, because result will immediately be a subject for GC
					return _list.Select(x => x.WeakReference).ToArray();
				}
				finally
				{
					_lock.ExitReadLock();
				}
			}
		}

		public IEnumerable<Entry> Entries
		{
			get
			{
				_lock.EnterReadLock();
				try
				{
					return _list
						.Where(x => x.WeakReference.IsAlive)
						.ToArray();
				}
				finally
				{
					_lock.ExitReadLock();
				}
			}
		}
	}

}
