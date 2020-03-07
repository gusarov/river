using River.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace River.SelfService
{
	partial class RiverSelfService
	{
		string GetStatsPageCore()
		{
			var entries = ObjectTracker.Default.Entries;
			var objects = entries.Select(x=>x.WeakReference.Target).ToString(); // keep strong refs here for a while

			var objsGroups = entries.GroupBy(x => x.WeakReference.Target?.GetType().Name);

			var sb = new StringBuilder($@"
<table>
<tr><th></th><th></th></tr>
<tr><td>Threads:</td><td>{Process.GetCurrentProcess().Threads.Count}</td></tr>
<tr><td>Process Uptime:</td><td>{DateTime.Now - Process.GetCurrentProcess().StartTime}</td></tr>
<tr><td>Clients:</td><td>{StatService.Instance.HandlersCount}</td></tr>
<tr><td>Connections:</td><td>{objsGroups.FirstOrDefault(x => x.Key == nameof(TcpClient))?.Count()}</td></tr>
</table>

Live Objects By Type:
<table><tr><th>Type</th><th>Count</th></tr>");
			foreach (var item in objsGroups.OrderByDescending(g => g.Count()))
			{
				sb.AppendLine($"<tr><td>{item.Key}</td><td>{item.Count()}</td></tr>");
			}
			sb.AppendLine($@"</table>

Live Objects:
<table><tr>
	<th>Id</th>
	<th>Type</th>
	<th>Since</th>
	<th>ToString</th>
</tr>");
			foreach (var entry in entries.OrderBy(x=>x.Id))
			{
				sb.AppendLine($"<tr>" +
					$"<td>{entry.Id}</td>" +
					$"<td>{entry.WeakReference?.Target?.GetType()?.Name}</td>" +
					$"<td>{entry.Utc:dd HH:mm:ss.fff}</td>" +
					$"<td>{Stringify(entry.WeakReference.Target)}</td>" +
					$"</tr>");
			}
			sb.AppendLine($"</table>");
			return sb.ToString();
		}

		private static string Stringify(object obj)
		{
			if (obj is Thread t)
			{
				return t.Name;
			}
			if (obj is TcpClient c)
			{
				try
				{
					return c?.Client?.RemoteEndPoint?.ToString();
				}
				catch (Exception ex)
				{
					return ex.GetType().Name;
				}
			}
			return obj?.ToString();
		}
	}


}
