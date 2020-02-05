using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace River
{
	static class Utils
	{
		public static Encoding Utf8 { get; } = new UTF8Encoding(false, false);

		public static int WriteUInt16(byte[] buf, int pos, ushort targetPort)
		{
			// Big Endian
			buf[pos++] = unchecked((byte)(targetPort >> 8));
			buf[pos++] = unchecked((byte)targetPort);
			return 2;
		}

		[Obsolete]
		public static byte[] GetPortBytes(int targetPort)
		{
			var portBuf = BitConverter.GetBytes(checked((ushort)targetPort));
			if (BitConverter.IsLittleEndian)
			{
				portBuf = new[] { portBuf[1], portBuf[0], };
			}
#if DEBUG
			if (portBuf.Length != 2)
			{
				throw new Exception("Fatal: portBuf must be 2 bytes");
			}
#endif
			return portBuf;
		}

	}
}
