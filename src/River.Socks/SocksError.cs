using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace River.Socks
{
	public enum SocksError : byte
	{
		OK = 0,
		GeneralSOCKSServerFailure = 1,
		ConnectionNotAllowedByRuleset = 2,
		NetworkUnreachable = 3,
		HostUnreachable = 4,
		ConnectionRefused = 5,
		TTLExpired = 6,
		CommandNotSupported = 7,
		AddressTypeNotSupported = 8,
	}
}
