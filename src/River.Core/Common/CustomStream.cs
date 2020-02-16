using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using River.Internal;

namespace River.Common
{
	public class CustomStream : SimpleNetworkStream
	{
		private readonly Stream _underlying;
		private Action<Stream, byte[], int, int> _send;
		private Func<Stream, byte[], int, int, int> _read;
		// private Func<byte[], int, int, int> _read1;

		protected void Init(Action<Stream, byte[], int, int> send, Func<Stream, byte[], int, int, int> read)
		{
			_send = send;
			_read = read;
		}

		protected CustomStream(Stream underlying)
		{
			_underlying = underlying;
		}

		public CustomStream(Stream underlying, Action<Stream, byte[], int, int> send, Func<Stream, byte[], int, int, int> read)
			: this(underlying)
		{
			Init(send, read);
		}

		/*
		public CustomStream(Stream underlying, Action<Stream, byte[], int, int> send, Func<byte[], int, int, int> read1)
		{
			_underlying = underlying;
			_send = send;
			_read1 = read1;
		}
		*/

		public override void Write(byte[] buffer, int offset, int count)
		{
			try
			{
				_send(_underlying, buffer, offset, count);
			}
			catch (IOException ex) when (ex.IsConnectionClosing())
			{
				Close();
			}
			catch (SocketException ex) when (ex.IsConnectionClosing())
			{
				Close();
			}
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			try
			{
				var r = _read(_underlying, buffer, offset, count);
				return r;
			}
			catch (IOException ex) when (ex.IsConnectionClosing())
			{
				Close();
				return 0;
			}
			catch (SocketException ex) when (ex.IsConnectionClosing())
			{
				Close();
				return 0;
			}
		}

		public override void Close()
		{
			_underlying.Close();
			base.Close();
		}
	}

}
