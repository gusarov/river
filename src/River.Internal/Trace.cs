using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace River
{
	public class TraceCategory
	{
		public static TraceCategory Test = new TraceCategory("test")!;
		public static TraceCategory Misc = new TraceCategory("misc");
		public static TraceCategory ObjectLive = new TraceCategory("objectlive");
		public static TraceCategory Networking = new TraceCategory("networking");
		public static TraceCategory NetworkingData = new TraceCategory("networking/data");
		public static TraceCategory Performance = new TraceCategory("performance");

		private readonly string _category;

		TraceCategory(string category)
		{
			IDisposable q = default;
			using var x = q;
			_category = category;
		}

		public override string ToString() => _category;
	}

	public class Trace
	{
		public static Trace Default { get; } = new Trace();

		HashSet<string> _categoryExcludes = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
		public ISet<string> CategoryExcludes => _categoryExcludes;

		bool Predicate(TraceCategory category)
		{
			var code = category.ToString();
			var i = -1;
			while (true)
			{
				i = code.IndexOf('/', i + 1);
				if (i < 0)
				{
					break;
				}

				var sub = code.Substring(0, i);
				if (_categoryExcludes.Contains(sub))
				{
					return false;
				}
			}
			return true;
		}


		// [Conditional("DEBUG")]
		/// <summary>
		/// 
		/// </summary>
		/// <param name="category">Message Category to quickly enable / disable</param>
		/// <param name="str"></param>
		public void WriteLine(TraceCategory category, string str)
		{
			if (category is null)
			{
				throw new ArgumentNullException(nameof(category));
			}

			if (!Predicate(category))
			{
				return;
			}
			str = $"[T{Thread.CurrentThread.ManagedThreadId:00}] {category}: {str}";
#if DEBUG
			Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] " + str);
#endif
			Appender.Instance.WriteLine(str); // this guy will add a time stamp
		}

		// [Conditional("DEBUG")]
		public void TraceError(string str)
		{
#if DEBUG
			Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] " + str);
#endif
			Appender.Instance.WriteLine(str);
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

	class Appender
	{
		internal static Appender Instance { get; } = new Appender();
		
		Timer _timer; 

		Appender()
		{
			try
			{
				LoadConfing();
			}
			catch { }
			_timer = new Timer(Flusher, null, 2000, 2000);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		void LoadConfing()
		{
			LogPath = ConfigurationManager.AppSettings["LogPath"] ?? LogPath;
		}

		private void Flusher(object state)
		{
			if (_writer != null)
			{
				lock (this)
				{
					if (_writer != null)
					{
						_writer.Flush();
					}
				}
			}
		}

		Stream _stream;
		StreamWriter _writer;
		DateTime _openedAt;

		const string DefaultLogPath = "./Logs";
		public string LogPath { get; set; } = DefaultLogPath;

		byte _seed;

		public void WriteLine(string msg)
		{
			var now = DateTime.UtcNow;
			lock (this)
			{
				#region Roll the file
				if (_seed++ == 0)
				{
					if (_openedAt.Date != now.Date)
					{
						_writer?.Close();
						_stream?.Close();
						Directory.CreateDirectory(LogPath);
						_stream = File.Open(Path.Combine(LogPath
							, now.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + $"_p{Process.GetCurrentProcess().Id}.log") 
							, FileMode.Append, FileAccess.Write, FileShare.Read);
						_openedAt = now.Date;
						_writer = new StreamWriter(_stream);

						#region Remove Old Files
						var toDelete = new DirectoryInfo(LogPath)
							.GetFiles()
							.Where(x => (now - x.CreationTimeUtc).TotalDays > 21)
							.ToArray();

						foreach (var item in toDelete)
						{
							try
							{
								File.Delete(item.FullName);
							}
							catch { }
						}
						#endregion
					}
				}
				#endregion
				_writer.WriteLine($"[{now:MM/dd HH:mm:ss.fff}] {msg}");
			}
		}
	}
}
