using River.ChaCha;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace River.ShadowSocks
{
	public class ShadowSocksHandler : Handler
	{
		static readonly Encoding _utf = new UTF8Encoding(false, false);

		IPAddress _addressRequested;
		string _dnsNameRequested;
		int _portRequested;
		int _bufferProcessedCount;

		new ShadowSocksServer Server => (ShadowSocksServer)base.Server;

		protected override Stream WrapStream(Stream stream)
		{
			return new ChaChaStream(stream, Server.Password);
		}

		protected override void HandshakeHandler()
		{
			// reserved byte 2 skipped
			var addressType = _buffer[_bufferProcessedCount++];

			bool addressTypeProcessed = false;
			switch (addressType)
			{
				case 1: // IPv4
					if (EnsureReaded(_bufferProcessedCount + 4))
					{
						var ipv4 = new byte[4];
						Array.Copy(_buffer, _bufferProcessedCount, ipv4, 0, 4);
						_addressRequested = new IPAddress(ipv4);
						_bufferProcessedCount += 4;
						addressTypeProcessed = true;
					}
					break;
				case 3: // DNS
					if (EnsureReaded(_bufferProcessedCount + 1))
					{
						var len = _buffer[_bufferProcessedCount];
						if (EnsureReaded(_bufferProcessedCount + 1 + len))
						{
							_dnsNameRequested = _utf.GetString(_buffer, _bufferProcessedCount + 1, len);
							// 256 max, no need to check for overflow
						}
						_bufferProcessedCount += 1 + len;
						addressTypeProcessed = true;
					}
					break;
				case 4: // IPv6
					if (EnsureReaded(_bufferProcessedCount + 16))
					{
						var ipv6 = new byte[16];
						Array.Copy(_buffer, _bufferProcessedCount, ipv6, 0, 16);
						_addressRequested = new IPAddress(ipv6);
						_bufferProcessedCount += 16;
						addressTypeProcessed = true;
					}
					break;
			}
			if (addressTypeProcessed) // continue
			{
				if (EnsureReaded(_bufferProcessedCount + 2))
				{
					_portRequested = _buffer[_bufferProcessedCount] * 256 + _buffer[_bufferProcessedCount + 1];
					_bufferProcessedCount += 2;

					Exception ex = null;
					try
					{
						EstablishUpstream(new DestinationIdentifier
						{
							Host = _dnsNameRequested,
							IPAddress = _addressRequested,
							Port = _portRequested,
						});
						if (_bufferProcessedCount < _bufferReceivedCount)
						{
							// forward the rest of the buffer
							Trace.WriteLine("Streaming - forward the rest >> " + (_bufferReceivedCount - _bufferProcessedCount) + " bytes");
							SendForward(_buffer, _bufferProcessedCount, _bufferReceivedCount - _bufferProcessedCount);
						}
						BeginStreaming();
					}
					catch (Exception exx)
					{
						ex = exx;
					}
				}
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
