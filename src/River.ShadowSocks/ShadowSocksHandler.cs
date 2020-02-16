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
		// int _bufferProcessedCount;

		new ShadowSocksServer Server => (ShadowSocksServer)base.Server;

		protected override Stream WrapStream(Stream stream)
		{
			return new ChaCha20Stream(stream, Server.Password);
		}

		protected override void HandshakeHandler()
		{
			// reserved byte 2 skipped
			var b = 0;
			var addressType = _buffer[b++];

			bool addressTypeProcessed = false;
			switch (addressType)
			{
				case 1: // IPv4
					if (EnsureReaded(b + 4))
					{
						var ipv4 = new byte[4];
						Array.Copy(_buffer, b, ipv4, 0, 4);
						_addressRequested = new IPAddress(ipv4);
						b += 4;
						addressTypeProcessed = true;
					}
					break;
				case 3: // DNS
					if (EnsureReaded(b + 1))
					{
						var len = _buffer[b++];
						if (EnsureReaded(b + len))
						{
							_dnsNameRequested = _utf.GetString(_buffer, b, len);
							// 256 max, no need to check for overflow
						}
						b += len;
						addressTypeProcessed = true;
					}
					break;
				case 4: // IPv6
					if (EnsureReaded(b + 16))
					{
						var ipv6 = new byte[16];
						Array.Copy(_buffer, b, ipv6, 0, 16);
						_addressRequested = new IPAddress(ipv6);
						b += 16;
						addressTypeProcessed = true;
					}
					break;
			}
			if (addressTypeProcessed) // continue
			{
				if (EnsureReaded(b + 2))
				{
					_portRequested = _buffer[b++] * 256 + _buffer[b++];

					Trace.WriteLine($"ShadowSocks Route: A{addressType} {_dnsNameRequested}{_addressRequested}:{_portRequested}");

					try
					{
						EstablishUpstream(new DestinationIdentifier
						{
							Host = _dnsNameRequested,
							IPAddress = _addressRequested,
							Port = _portRequested,
						});
						if (b < _bufferReceivedCount)
						{
							// forward the rest of the buffer
							Trace.WriteLine("Forward the rest >> " + (_bufferReceivedCount - b) + " bytes");
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
