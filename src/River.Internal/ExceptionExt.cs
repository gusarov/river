using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace River
{
	public static class ExceptionExt
	{
		public static bool IsConnectionClosing(this Exception ex)
		{
			if (ex.InnerException != null)
			{
				return ex.InnerException.IsConnectionClosing();
			}

			if (ex is SocketException sex)
			{
				if (sex.SocketErrorCode == SocketError.Interrupted)
				{
					return true;
				}
			}

			if (ex is ConnectionClosingException)
			{
				return true;
			}

			return false;
		}
	}


	[Serializable]
	public class ConnectionClosingException : Exception
	{
		public ConnectionClosingException() { }
		public ConnectionClosingException(string message) : base(message) { }
		public ConnectionClosingException(string message, Exception inner) : base(message, inner) { }
		protected ConnectionClosingException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}
