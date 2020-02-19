using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace River
{
	public class Trace
	{
		// [Conditional("DEBUG")]
		public static void WriteLine(string str)
		{
#if DEBUG
			Console.WriteLine(str);
#endif
			Logger.Instance.WriteLine(str);
		}

		// [Conditional("DEBUG")]
		public static void TraceError(string str)
		{
#if DEBUG
			Console.WriteLine(str);
#endif
			Logger.Instance.WriteLine(str);
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

	public class Logger
	{
		public static Logger Instance { get; } = new Logger();
		
		Timer _timer; 

		Logger()
		{
			LogPath = ConfigurationManager.AppSettings["LogPath"];
			_timer = new Timer(Flusher, null, 2000, 2000);
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

		public string LogPath { get; set; }

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
							, now.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".log")
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
