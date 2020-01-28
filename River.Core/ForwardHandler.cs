using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace River
{
	public abstract class ForwardHandler : IDisposable
	{
		private readonly Forwarder _forwarder;
		protected Stream _stream;

		public ForwardHandler(Forwarder forwarder)
		{
			_forwarder = forwarder;
		}

		byte[] _readBuffer = new byte[1024 * 16];

		protected abstract Stream EstablishConnectionCore(DestinationIdentifier destination);

		public void EstablishConnection(DestinationIdentifier destination)
		{
			_stream = EstablishConnectionCore(destination);
			_stream.BeginRead(_readBuffer, 0, _readBuffer.Length, Received, null);
		}

		void Received(IAsyncResult ar)
		{
			var bytes = _stream.EndRead(ar);
			// _forwarder.ReceivedFromUpstream(_readBuffer, 0, bytes);
			if (!_isDisposing)
			{
				_stream.BeginRead(_readBuffer, 0, _readBuffer.Length, Received, null);
			}
		}

		public void Send(byte[] buf, int pos, int cnt)
		{
			// envelope data by chain
			(buf, pos, cnt) = _forwarder.Pack(buf, pos, cnt);
			Trace.WriteLine("Forward Packet: \r\n" + Encoding.ASCII.GetString(buf, pos, cnt));

			// send to upstream
			_stream.Write(buf, pos, cnt);
		}

		bool _isDisposing;

		public void Dispose()
		{
			_isDisposing = true;
		}
	}
}
