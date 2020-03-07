using River.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace River.SelfService
{
	partial class RiverSelfService
	{
		string GetAdminPage()
		{
			return GetAdminPageCore();
		}

		string GetAdminPageCore()
		{
			var objs = ObjectTracker.Default.Items.ToArray();
			var objsGroups = objs.GroupBy(x => x?.GetType().Name);

			var sb = new StringBuilder($@"
Authorizations:
<p>

<input type=checkbox> Enable Anonimous Users</input> <br/>
<input type=checkbox> Enable Cleartext User/Password</input> <br/>

<p>

<table>
<tr><th></th><th></th></tr>
<tr><td>Threads:</td><td>{Process.GetCurrentProcess().Threads.Count}</td></tr>
<tr><td>Process Uptime:</td><td>{DateTime.Now - Process.GetCurrentProcess().StartTime}</td></tr>
<tr><td>Clients:</td><td>{StatService.Instance.HandlersCount}</td></tr>
<tr><td>Connections:</td><td>{objsGroups.FirstOrDefault(x => x.Key == nameof(TcpClient))?.Count()}</td></tr>
</table>

Live Objects:
<table><tr><th>Type</th><th>Count</th></tr>");
			foreach (var item in objsGroups.OrderByDescending(g=>g.Count()))
			{
				sb.AppendLine($"<tr><td>{item.Key}</td><td>{item.Count()}</td></tr>");
			}
			sb.AppendLine($"</table>");
			return sb.ToString();
		}
	}
}
