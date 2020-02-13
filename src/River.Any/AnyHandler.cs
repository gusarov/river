using System;
using System.Collections.Generic;
using System.Text;

namespace River.Any
{
	public class AnyHandler : Handler
	{
		protected override void HandshakeHandler() => throw new NotSupportedException();
	}
}
