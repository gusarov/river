using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using River.Common;

namespace River
{

	public abstract class RiverServer : IDisposable
	{
		public IList<ProxyIdentifier> Chain { get; } = new List<ProxyIdentifier>();

		protected DebuggerTracker DebuggerTracker { get; } = new DebuggerTracker();

		protected abstract Handler CreateHandler(TcpClient client);

		protected bool Disposing { get; private set; }

		public abstract void Run(ServerConfig config);

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~RiverServer()
		{
			Dispose(false);
		}

		protected virtual void Dispose(bool managed)
		{
			Disposing = true;
		}

	}
}
