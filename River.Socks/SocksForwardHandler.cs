
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace River.Socks
{
	public class SocksForwardHandler : ForwardHandler
	{
		private readonly SocksForwarder _forwarder;
		Socks4Client _client;

		public SocksForwardHandler(SocksForwarder forwarder) : base(forwarder)
		{
			_forwarder = forwarder;
			_client = new Socks4Client();
		}

		protected override Stream EstablishConnectionCore(DestinationIdentifier id)
		{
			_client.Plug(_forwarder.Host, _forwarder.Port);
			/*
				_forwarder.Host, _forwarder.Port
				, destination.Host ?? destination.IPAddress.ToString()
				, destination.Port, true
				);
			*/

			return _client;
		}
	}
}
