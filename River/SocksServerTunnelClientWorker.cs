using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace River
{
	public class SocksServerTunnelClientWorker : SocksServerClientWorker, IThrottable
	{
		private readonly SocksServerToRiverClient _server;

		//private RiverClient _forward;
		protected TcpClient _clientForward;
		protected NetworkStream _streamFroward;

		private byte[] _bufferForwardRead = new byte[1024 * 128];
		private int _bufferForwardReadPos;

		public override void Dispose()
		{
			base.Dispose();
			try
			{
				_clientForward?.Close();
				_clientForward = null;
			}
			catch { }
			try
			{
				_streamFroward?.Close();
				_streamFroward = null;
			}
			catch { }
		}

		public SocksServerTunnelClientWorker(SocksServer<SocksServerTunnelClientWorker> server, TcpClient client) : base(client)
		{
			if (server == null)
			{
				throw new ArgumentNullException(nameof(server));
			}
			_server = (SocksServerToRiverClient)server;
			if (_server == null)
			{
				throw new ArgumentNullException(nameof(server));
			}
		}

		protected override void EstablishForwardConnection()
		{
			if (_disposed)
			{
				return;
			}
			if (_server.OutgoingInterface != null)
			{
				_clientForward = new TcpClient(_server.OutgoingInterface);
			}
			else
			{
				_clientForward = new TcpClient();
			}
			string response = "";
			try
			{
				_clientForward.SendTimeout = Math.Min(_clientForward.SendTimeout, 6000);
				_clientForward.Connect(_server.RiverHost, _server.RiverPort);
				_streamFroward = _clientForward.GetStream();

				var target = _dnsNameRequested ?? _addressesRequested[0].ToString();
				var targetPort = _portRequested;

				var requestString = $"GET http://{_server.RiverHost}:{_server.RiverPort}/?{Obfuscate(0)}={Obfuscate()}&{Obfuscate(1, 'v')}=1&{Obfuscate(2, 'c')}=c&{Obfuscate(3, 'h')}={target}&{Obfuscate(4, 'p')}={targetPort}&{Obfuscate(5)}={Obfuscate()} HTTP/1.0\r\n"
				                    + "Connection: keep-alive\r\n"
					//+ "User-Agent: Mozilla/5.0 (Windows NT 6.1; Win64; x64; rv:50.0) Gecko/20100101 Firefox/50.0\r\n"
				                    + "Accept: text/html\r\n"
				                    + $"Host: {_server.RiverHost}:{_server.RiverPort}\r\n"
				                    + "Accept-Encoding: gzip, deflate\r\n"
				                    + "Cache-Control: no-cache\r\n"
				                    + "\r\n";
				var request = _utf.GetBytes(requestString);
				Trace.WriteLine("Requesting establish connection...");
				_streamFroward.Write(request, 0, request.Length);

				var count = _streamFroward.Read(_bufferForwardRead, 0, _bufferForwardRead.Length);
				response = _utf.GetString(_bufferForwardRead, 0, count);
			}
			catch (Exception ex)
			{
				Trace.TraceError("Rotate server becasue: " + ex.Message);
				_server.RotateServer();
				throw;
			}
			// todo must parse response only till \r\n\r\n
			if (!response.StartsWith("HTTP/1.0 200"))
			{
				Trace.WriteLine("Bad response...");
				throw new Exception();
			}
			Trace.WriteLine("Established...");
			_client.NoDelay = true;
			_clientForward.NoDelay = true;
			_streamFroward.BeginRead(_bufferForwardRead, 0, _bufferForwardRead.Length, ReceiveFromForward, null);
		}

		void ReceiveFromForward(IAsyncResult ar)
		{
			if (_disposed)
			{
				return;
			}

			try
			{
				// _bufferForwardRead
				var count = _streamFroward.EndRead(ar);
				if (count > 0
#if CC
					|| _clientForward.Connected
#endif
					)
				{
					ReceiveFromForward(count);
				}
				else
				{
					Dispose();
				}
			}
			catch (Exception ex)
			{
				Trace.TraceError($"ReceiveFromForward Exception: " + ex);
				Dispose();
			}
		}

		void ReceiveFromForward(int count)
		{
			Throttle(count);
			// do the job - unpack the bytes from river and send it to the client
			int eoh;
			string responseHeaderString;
			var headers = Utils.TryParseHttpHeader(_bufferForwardRead, 0, _bufferForwardReadPos + count, out eoh, out responseHeaderString);
			if (headers == null)
			{
				// not complete - read some more
				Trace.WriteLine("ReceiveFromForward - not complete - read some more");
				_bufferForwardReadPos += count;
				_streamFroward.BeginRead(_bufferForwardRead, _bufferForwardReadPos, _bufferForwardRead.Length - _bufferForwardReadPos, ReceiveFromForward, null);
				return;
			}
			// make sure headers and full body are in place
			// extract body
			string lenStr;
			if (!headers.TryGetValue("Content-Length", out lenStr))
			{
				throw new Exception("Content-Length is mandatory\r\n" + responseHeaderString);
			}
			int len = int.Parse(lenStr);

			if (len > count + _bufferForwardReadPos - eoh)
			{
				// not complete - read some more
				Trace.WriteLine("ReceiveFromForward - not complete - read some more");
				_bufferForwardReadPos += count;
				_streamFroward.BeginRead(_bufferForwardRead, _bufferForwardReadPos, _bufferForwardRead.Length - _bufferForwardReadPos, ReceiveFromForward, null);
			}
			else
			{
				// process - decode the body
				var data = new byte[len];
				Array.Copy(_bufferForwardRead, eoh, data, 0, len);
				for (int i = 0; i < len; i++)
				{
					data[i] = (byte)(data[i] ^ 0xAA);
				}
				// send back to SOCKS client
#if DEBUG
				var debug = Encoding.ASCII.GetString(data, 0, data.Length);
#endif
				Trace.WriteLine($"Receive from forward {_clientForward.Client.LocalEndPoint} river {data.Length} bytes");

				_stream.Write(data);

				// process remaining part of message
				if (len + eoh < _bufferForwardReadPos + count)
				{
					Array.Copy(_bufferForwardRead, len + eoh, _bufferForwardRead, 0, _bufferForwardReadPos + count - len - eoh);
					var len2 = _bufferForwardReadPos + count - len - eoh;
					_bufferForwardReadPos = 0;
					Trace.WriteLine($"ReceiveFromForward - have extra {len2} bytes, call ReceiveFromForward");
					ReceiveFromForward(len2);
				}
				else
				{
					// get ready for next message
					_bufferForwardReadPos = 0;
					_streamFroward.BeginRead(_bufferForwardRead, 0, _bufferForwardRead.Length, ReceiveFromForward, null);
				}
			}
		}

		protected override void SendForward(byte[] buffer, int pos, int count)
		{
			if (_disposed)
			{
				return;
			}

			Throttle(count);

			var requestString = $"POST http://{_server.RiverHost}:{_server.RiverPort}/ HTTP/1.0\r\n"
				+ $"Connection: keep-alive\r\n"
				//+ "User-Agent: Mozilla/5.0 (Windows NT 6.1; Win64; x64; rv:50.0) Gecko/20100101 Firefox/50.0\r\n"
				+ $"Accept: text/html\r\n"
				+ $"Content-Type: text/html\r\n"
				+ $"Host: {_server.RiverHost}:{_server.RiverPort}\r\n"
				+ $"Accept-Encoding: gzip, deflate\r\n"
				+ $"Cache-Control: no-cache\r\n"
				+ $"Content-Length: {count}\r\n"
				+ "\r\n";
			var request = _utf.GetBytes(requestString);
			var requestBuf = new byte[request.Length + count];
			Array.Copy(request, requestBuf, request.Length);
			for (int i = pos; i < pos+count; i++)
			{
				buffer[i]= (byte)(buffer[i] ^ 0xAA);
			}
			Array.Copy(buffer, pos, requestBuf, request.Length, count);
			Trace.WriteLine($"Send to the {_clientForward.Client.LocalEndPoint} river {count} bytes");
			_streamFroward.Write(requestBuf, 0, requestBuf.Length);
		}

		private static readonly Random _rnd = new Random();

		static string Obfuscate(int id = -1, char actualName = '_')
		{
			var sb = new StringBuilder();
			for (int i = 0, m=_rnd.Next(2)+id+2; i < m; i++)
			{
				if (i == id)
				{
					sb.Append(actualName);
				}
				else
				{
					sb.Append((char)('a'+_rnd.Next(26)));
				}
			}
			return sb.ToString();
		}

		#region

		/// <summary>
		/// The maximum bytes per second that can be transferred through the base stream.
		/// </summary>
		public long MaximumBytesPerSecond
		{
			get { return Throttling.Default.Bandwidth; }
			set { Throttling.Default.Bandwidth = value; }
		}

		/// <summary>
		/// Throttles for the bytesCount.
		/// </summary>
		void Throttle(int bytesCount)
		{
			Throttling.Default.Throttle(bytesCount);
		}

		#endregion

	}

	public interface IThrottable
	{
		long MaximumBytesPerSecond { get; set; }
	}
}