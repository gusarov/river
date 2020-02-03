using System;
using System.IO;
using System.IO.Compression;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace River
{
	public abstract class ForwardHandler : IDisposable
	{
		private readonly Forwarder _forwarder;
		private ForwardHandler _nextForwarderHandler;
		protected Stream _upstream;
		// protected Stream _downstream;

		public ForwardHandler(Forwarder forwarder)
		{
			_forwarder = forwarder;
		}

		byte[] _readBuffer = new byte[1024 * 16];

		protected abstract Stream EstablishConnectionCore(DestinationIdentifier id);

		public void EstablishConnection(DestinationIdentifier id)
		{
			if (_forwarder.NextForwarder != null)
			{
				_nextForwarderHandler = _forwarder.NextForwarder.CreateForwardHandler();
				_nextForwarderHandler.EstablishConnection(id);
			}
			else
			{
				_upstream = EstablishConnectionCore(id);
				_upstream.BeginRead(_readBuffer, 0, _readBuffer.Length, Received, null);
			}
		}

		public void Route(DestinationIdentifier destination)
		{
			// _original = original;
			// _stream = original ?? EstablishConnectionCore(destination);
			// _stream.BeginRead(_readBuffer, 0, _readBuffer.Length, Received, null);
		}

		void Received(IAsyncResult ar)
		{
			var bytes = _upstream.EndRead(ar);

			Trace.WriteLine("<<<< \r\n" + Encoding.ASCII.GetString(_readBuffer, 0, bytes));

			if (bytes == 0)
			{
				Dispose();
				return;
			}

			// _forwarder.ReceivedFromUpstream(_readBuffer, 0, bytes);

			if (!_isDisposing)
			{
				_upstream.BeginRead(_readBuffer, 0, _readBuffer.Length, Received, null);
			}
		}

		public void Send(byte[] buf, int pos, int cnt)
		{
			// envelope data by chain
			(buf, pos, cnt) = _forwarder.Pack(buf, pos, cnt);
			Trace.WriteLine(">>>> \r\n" + Encoding.ASCII.GetString(buf, pos, cnt));

			// send to upstream
			_upstream.Write(buf, pos, cnt);
		}

		bool _isDisposing;

		public void Dispose()
		{
			_isDisposing = true;
		}
	}
}
