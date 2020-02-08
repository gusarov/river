using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace River.AcceptanceTestingAgent
{
	class Program
	{
		static void Main(string[] args)
		{
			new Program().Start();
			Console.WriteLine("Waiting binary data...");
			Console.ReadLine();
		}

		public Program()
		{
			
		}

		private TcpListener _listener;

		public void Start()
		{
			_listener = TcpListener.Create(79);
			_listener.Start();
			_listener.BeginAcceptTcpClient(NewTcpClient, null);
		}

		void NewTcpClient(IAsyncResult ar)
		{
			var client = _listener.EndAcceptTcpClient(ar);
			new ClientHandler(client);
			_listener.BeginAcceptTcpClient(NewTcpClient, null);
		}
	}

	internal class ClientHandler : IDisposable
	{
		private readonly TcpClient _client;
		private readonly NetworkStream _stream;
		private readonly byte[] _buffer = new byte[1024 * 256];

		public ClientHandler(TcpClient client)
		{
			_client = client;
			_client.NoDelay = true;
			_stream = _client.GetStream();
			_stream.BeginRead(_buffer, 0, _buffer.Length, CommandData, null);
		}

		/// <summary>
		/// First package contains command
		/// </summary>
		private void CommandData(IAsyncResult ar)
		{
			try
			{
				var count = _stream.EndRead(ar);
				if (count > 0)
				{
					if (_buffer[0] == 1) // get incoming bytes and validate it
					{
						_mode = 1;
						_stream.WriteByte(1); // 1 ready
						Console.WriteLine($"Command {_mode} begin: count={count}");
					}
					_stream.BeginRead(_buffer, 0, _buffer.Length, IncommingData, null);
				}
				else
				{
					Console.WriteLine("Count 0c");
					Dispose();
				}
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex.ToString());
				Dispose();
			}
		}

		private int _mode;
		private int _total;
		private byte _lastByte;

		private byte _zeroCounts;

		/// <summary>
		/// next packages just contains data
		/// </summary>
		private void IncommingData(IAsyncResult ar)
		{
			try
			{
				var count = _stream.EndRead(ar);
				_total += count;
				if (count > 0)
				{
					for (int i = 0; i < count; i++)
					{
						if (_buffer[i] != _lastByte++)
						{
							_stream.Write(BitConverter.GetBytes(0), 0, 4); // error
							Console.WriteLine($"Binary data error: i={i}, count={count}, total={_total}, expected={_lastByte-1}, actual={_buffer[i]}");
						}
					}
					_stream.Write(BitConverter.GetBytes(count), 0, 4); // count
					Console.WriteLine($"Binary data good: count={count}, total={_total}");
					_stream.BeginRead(_buffer, 0, _buffer.Length, IncommingData, null);
				}
				else
				{
					Console.WriteLine($"Count 0i, total={_total}");
					if (_zeroCounts++ > 4)
					{
						Dispose();
					}
					else
					{
						_stream.BeginRead(_buffer, 0, _buffer.Length, IncommingData, null);
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
				Dispose();
			}
		}

		public void Dispose()
		{
			Console.WriteLine("Dispose");
			try
			{
				_stream?.Close();
			}
			catch
			{
			}
			try
			{
				_client?.Close();
			}
			catch
			{
			}
		}
	}
}
