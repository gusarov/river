using River;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace River.ShadowSocks
{
	public class PortForwardingHandler : Handler
	{
		static Trace Trace = River.Trace.Default;

		new PortForwardingServer Server => (PortForwardingServer)base.Server;

		protected override void HandshakeHandler()
		{
			try
			{
				EstablishUpstream(new DestinationIdentifier
				{
					Host = Server.TargetHost,
					Port = Server.TargetPort,
				});
				var b = 0;
				if (_bufferReceivedCount > b)
				{
					// forward the rest of the buffer
					Trace.WriteLine(TraceCategory.NetworkingData, "Forward the rest >> " + (_bufferReceivedCount - b) + " bytes");
					SendForward(_buffer, b, _bufferReceivedCount - b);
				}
				BeginStreaming();
			}
			catch (Exception ex)
			{
				Trace.TraceError(ex.GetType().Name + ": " + ex.Message);
				Dispose();
				throw;
			}
		}

		/*
		private void ReceivedStreaming(IAsyncResult ar)
		{
			if (Disposing)
			{
				return;
			}
			try
			{
				var count = _stream.EndRead(ar);
				Trace.WriteLine("Streaming - received from client >> " + count + " bytes");
				if (count > 0 && Client.Connected)
				{
					SendForward(_buffer, 0, count);
					_stream.BeginRead(_buffer, 0, _buffer.Length, ReceivedStreaming, null);
				}
				else
				{
					Dispose();
				}
			}
			catch (Exception ex)
			{
				Trace.TraceError("Streaming - received from client: " + ex);
				Dispose();
			}
		}
		*/
	}
}
