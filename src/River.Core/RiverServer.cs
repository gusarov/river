using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using River.Common;
using River.Internal;

namespace River
{

	public abstract class RiverServer : IDisposable
	{
		public RiverServer()
		{
			ObjectTracker.Default.Register(this);
		}

		public IList<ProxyIdentifier> Chain { get; } = new List<ProxyIdentifier>();

		protected DebuggerTracker DebuggerTracker { get; } = new DebuggerTracker();

		protected abstract Handler CreateHandlerCore(TcpClient client);

		// List<>

		protected Handler CreateHandler(TcpClient client)
		{
			var handler = CreateHandlerCore(client);
			return handler;
		}

		protected bool IsDisposed { get; private set; }

		public ServerConfig Config;

		public void Run(ServerConfig config)
		{
			Config = config;
			RunCore(config);
		}

		public abstract void RunCore(ServerConfig config);

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
			IsDisposed = true;
		}

		public override string ToString()
		{
			var b = base.ToString();
			return $"{b} {(IsDisposed ? "Disposed" : "NotDisposed")}";
		}
	}
}
