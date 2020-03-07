using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace River.Auth
{
	public class AuthHashProcessor
	{
		static RNGCryptoServiceProvider _rnd = new RNGCryptoServiceProvider();

		// static Rfc2898DeriveBytes _rfc2898DeriveBytes = new Rfc2898DeriveBytes()
		/*
		static unsafe byte[] GetPasswordBytes(SecureString password)
		{
		IntPtr unmanagedBytes = Marshal.SecureStringToGlobalAllocUnicode(password);
		byte[] bValue = null;
		try
		{
		byte* byteArray = (byte*)unmanagedBytes.GetPointer();

		// Find the end of the string
		byte* pEnd = byteArray;
		char c = '\0';
		do
		{
		byte b1 = *pEnd++;
		byte b2 = *pEnd++;
		c = '\0';
		c = (char)(b1 << 8);
		c += (char)b2;
		} while (c != '\0');

		// Length is effectively the difference here (note we're 2 past end) 
		int length = (int)((pEnd - byteArray) - 2);
		bValue = new byte[length];
		for (int i = 0; i < length; ++i)
		{
		// Work with data in byte array as necessary, via pointers, here
		bValue[i] = *(byteArray + i);
		}
		}
		finally
		{
		// This will completely remove the data from memory
		Marshal.ZeroFreeGlobalAllocUnicode(unmanagedBytes);
		}
		}

		public string Generate(SecureString password)
		{
			// var pwd = password.ToString();
			// var q = new Rfc2898DeriveBytes(pwd, )
			// q.
		}

		*/

		static SHA256CryptoServiceProvider _sha256 = new SHA256CryptoServiceProvider();

		public string Generate(string password)
		{
			return Generate(Encoding.UTF8.GetBytes(password));
		}

		public string Generate(byte[] password)
		{
			if (password is null)
			{
				throw new ArgumentNullException(nameof(password));
			}

			const int saltLen = 8;
			var buf = new byte[saltLen + password.Length];
			_rnd.GetBytes(buf, 0, 8);
			Array.Copy(password, 0, buf, saltLen, password.Length);

			var hash = _sha256.ComputeHash(buf);

			return $"SHA256.{Convert.ToBase64String(buf, 0, saltLen)}.{Convert.ToBase64String(hash)}";
		}

		const int _saltLen = 8;

		string Generate(byte[] password, byte[] salt)
		{
			if (password is null)
			{
				throw new ArgumentNullException(nameof(password));
			}

			var buf = new byte[_saltLen + password.Length];
			_rnd.GetBytes(buf, 0, 8);
			Array.Copy(password, 0, buf, _saltLen, password.Length);

			var hash = _sha256.ComputeHash(buf);

			return $"SHA256.{Convert.ToBase64String(buf, 0, _saltLen)}.{Convert.ToBase64String(hash)}";
		}

		public bool Validate(string hashStory, string password)
		{
			return Validate(hashStory, Encoding.UTF8.GetBytes(password));
		}

		public bool Validate(string hashStory, byte[] password)
		{
			var hashDetails = hashStory.Split('.');
			if (hashDetails[0] != "SHA256")
			{
				throw new Exception("Only SHA256 supported");
			}

			var salt = Convert.FromBase64String(hashDetails[1]);
			var buf = new byte[salt.Length + password.Length];
			salt.CopyTo(buf, 0);
			Array.Copy(password, 0, buf, salt.Length, password.Length);

			var hash = _sha256.ComputeHash(buf);
			var hashBase64 = Convert.ToBase64String(hash);

			return hashBase64 == hashDetails[2];
		}
	}
}
