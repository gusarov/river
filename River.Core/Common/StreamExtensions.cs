using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace River
{
	public static class StreamExtensions
	{
		public static void Write(this Stream stream, params byte[] data)
		{
			stream.Write(data, 0, data.Length);
		}

	}
}
