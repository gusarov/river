﻿using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using CSChaCha20;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace River.ShadowSocks.Test
{
	[TestClass]
	public class ShadowSocksTest
	{
		/*
This code been used as a referenece: https://play.golang.org/

func kdf(password string, keyLen int) []byte {
	var b, prev []byte
	h := md5.New()
	for len(b) < keyLen {
		h.Write(prev)
		h.Write([]byte(password))
		b = h.Sum(b)
		prev = b[len(b)-h.Size():]
		h.Reset()
	}
	return b[:keyLen]
}
		*/

		[TestMethod]
		public void Should_derive_key_as_original_shadow_socks()
		{
			Assert.AreEqual("202cb962ac59075b964b07152d234b70d1d99ca9b7ec0708c83ecca4b635dbf1", Hex(ShadowSocksClientStream.Kdf("123")));
			Assert.AreEqual("250cf8b51c773f3f8dc8b4be867a9a02e34b5564d64960137c5fb9c6c5e80797", Hex(ShadowSocksClientStream.Kdf("456")));
			Assert.AreEqual("23308d9f4c4f0e40077418ca045792abda991f59ef37305c64e8f8e9c3af8549", Hex(ShadowSocksClientStream.Kdf("202cb962ac59075b964b07152d234b70d1d99ca9b7ec0708c83ecca4b635dbf1")));
		}

		private const int validKeyLength = 32;

		private const int validNonceLength = 12;


		[TestMethod]
		public void Should_encrypt_same_way()
		{
			var nonce = new byte[] { 158, 117, 91, 144, 144, 242, 8, 211 };
			var orig = "030e6170692e6769746875622e636f6d01bb16030300a5010000a103035e37b36fb24109df1e6e42c9dafc356e2d9b612b25cd5b6210876a946140b76000002ac02cc02bc030c02f009f009ec024c023c028c027c00ac009c014c013009d009c003d003c0035002f000a0100004e00000013001100000e6170692e6769746875622e636f6d000a00080006001d00170018000b00020100000d001400120401050102010403050302030202060106030023000000170000ff01000100".Replace(" ", "");
			var origBytes = Hex(orig);

			var server = Encoding.ASCII.GetString(origBytes, 2, 14);
			var port = (origBytes[16] << 8) + origBytes[17];
			var body = Encoding.ASCII.GetString(origBytes, 17, origBytes.Length - 17);

			var enc = "c04da2490ee5658a19d282ca4fdb359e9e2028e7e871af3a642b67123fc296916976b535facd57f4f847ae9fe6412f8de3ad652290a5184043f88050aac7e0cc729a396a967dd8d2a4baa62a880af73006322aa0f219d932d91fbcf6a9b00ce864f7c28a7e57a912fd98454c14e040d91e4a2e848c1b87bb0b7735cf2a87c55cba78cb07e389f30a05efdff937ed35fc14116ab14aed2650fd5cdc981b0020bc58d798b011bc92c47bdc7579ccf100ac87c8d7a31239a81151403325";

			Assert.AreEqual(enc, Hex(new ChaCha20B(ShadowSocksClientStream.Kdf("pwd"), nonce, 0).EncryptBytes(origBytes)));
		}

		[TestMethod]
		public void Should_encrypt_block_by_block_and_count_counter_correctly()
		{
			var nonce = new byte[] { 158, 117, 91, 144, 144, 242, 8, 211 };
			var data = Hex("030e6170692e6769746875622e636f6d01bb16030300a5010000a103035e37b36fb24109df1e6e42c9dafc356e2d9b612b25cd5b6210876a946140b76000002ac02cc02bc030c02f009f009ec024c023c028c027c00ac009c014c013009d009c003d003c0035002f000a0100004e00000013001100000e6170692e6769746875622e636f6d000a00080006001d00170018000b00020100000d001400120401050102010403050302030202060106030023000000170000ff01000100");

			// prepare small buffer of different sizes
			var part1 = data.Take(60).ToArray();
			var part2 = data.Skip(60).Take(100).ToArray();
			var part3 = data.Skip(160).ToArray();

			var result = new byte[16 * 1024];
			var chacha = new ChaCha20B(ShadowSocksClientStream.Kdf("pwd"), nonce, 0);
			chacha.Crypt(part1, 0, result, 0, part1.Length);
			chacha.Crypt(part2, 0, result, part1.Length, part2.Length);
			chacha.Crypt(part3, 0, result, part1.Length + part2.Length, part3.Length);

			result = result.Take(part1.Length + part2.Length + part3.Length).ToArray();
			var exp = "c04da2490ee5658a19d282ca4fdb359e9e2028e7e871af3a642b67123fc296916976b535facd57f4f847ae9fe6412f8de3ad652290a5184043f88050aac7e0cc729a396a967dd8d2a4baa62a880af73006322aa0f219d932d91fbcf6a9b00ce864f7c28a7e57a912fd98454c14e040d91e4a2e848c1b87bb0b7735cf2a87c55cba78cb07e389f30a05efdff937ed35fc14116ab14aed2650fd5cdc981b0020bc58d798b011bc92c47bdc7579ccf100ac87c8d7a31239a81151403325";
			Assert.AreEqual(exp, Hex(result));
		}

		[TestMethod]
		public void Should_encrypt_same_way2()
		{
			// Actual

			// These vectors are from https://github.com/quartzjer/chacha20/blob/master/test/chacha20.js

			var key1 = new byte[validKeyLength] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			var nonce1 = new byte[validNonceLength] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			uint counter1 = 0;
			const int lengthOfContent1 = 64;
			var content1 = new byte[lengthOfContent1] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			var expected1 = new byte[lengthOfContent1] {
															0x76, 0xb8, 0xe0, 0xad, 0xa0, 0xf1, 0x3d, 0x90,
															0x40, 0x5d, 0x6a, 0xe5, 0x53, 0x86, 0xbd, 0x28,
															0xbd, 0xd2, 0x19, 0xb8, 0xa0, 0x8d, 0xed, 0x1a,
															0xa8, 0x36, 0xef, 0xcc, 0x8b, 0x77, 0x0d, 0xc7,
															0xda, 0x41, 0x59, 0x7c, 0x51, 0x57, 0x48, 0x8d,
															0x77, 0x24, 0xe0, 0x3f, 0xb8, 0xd8, 0x4a, 0x37,
															0x6a, 0x43, 0xb8, 0xf4, 0x15, 0x18, 0xa1, 0x1c,
															0xc3, 0x87, 0xb6, 0x69, 0xb2, 0xee, 0x65, 0x86
															};
			var output1 = new byte[lengthOfContent1];


			var key2 = new byte[validKeyLength] {
														0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
														0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f,
														0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
														0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f
													};
			var nonce2 = new byte[validNonceLength] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x4a, 0x00, 0x00, 0x00, 0x00 };
			uint counter2 = 1;
			const int lengthOfContent2 = 114;
			var content2 = new byte[lengthOfContent2] {
														0x4c, 0x61, 0x64, 0x69, 0x65, 0x73, 0x20, 0x61,
														0x6e, 0x64, 0x20, 0x47, 0x65, 0x6e, 0x74, 0x6c,
														0x65, 0x6d, 0x65, 0x6e, 0x20, 0x6f, 0x66, 0x20,
														0x74, 0x68, 0x65, 0x20, 0x63, 0x6c, 0x61, 0x73,
														0x73, 0x20, 0x6f, 0x66, 0x20, 0x27, 0x39, 0x39,
														0x3a, 0x20, 0x49, 0x66, 0x20, 0x49, 0x20, 0x63,
														0x6f, 0x75, 0x6c, 0x64, 0x20, 0x6f, 0x66, 0x66,
														0x65, 0x72, 0x20, 0x79, 0x6f, 0x75, 0x20, 0x6f,
														0x6e, 0x6c, 0x79, 0x20, 0x6f, 0x6e, 0x65, 0x20,
														0x74, 0x69, 0x70, 0x20, 0x66, 0x6f, 0x72, 0x20,
														0x74, 0x68, 0x65, 0x20, 0x66, 0x75, 0x74, 0x75,
														0x72, 0x65, 0x2c, 0x20, 0x73, 0x75, 0x6e, 0x73,
														0x63, 0x72, 0x65, 0x65, 0x6e, 0x20, 0x77, 0x6f,
														0x75, 0x6c, 0x64, 0x20, 0x62, 0x65, 0x20, 0x69,
														0x74, 0x2e
														};
			var expected2 = new byte[lengthOfContent2] {
															0x6e, 0x2e, 0x35, 0x9a, 0x25, 0x68, 0xf9, 0x80, 0x41, 0xba, 0x07, 0x28, 0xdd, 0x0d, 0x69, 0x81,
															0xe9, 0x7e, 0x7a, 0xec, 0x1d, 0x43, 0x60, 0xc2, 0x0a, 0x27, 0xaf, 0xcc, 0xfd, 0x9f, 0xae, 0x0b,
															0xf9, 0x1b, 0x65, 0xc5, 0x52, 0x47, 0x33, 0xab, 0x8f, 0x59, 0x3d, 0xab, 0xcd, 0x62, 0xb3, 0x57,
															0x16, 0x39, 0xd6, 0x24, 0xe6, 0x51, 0x52, 0xab, 0x8f, 0x53, 0x0c, 0x35, 0x9f, 0x08, 0x61, 0xd8,
															0x07, 0xca, 0x0d, 0xbf, 0x50, 0x0d, 0x6a, 0x61, 0x56, 0xa3, 0x8e, 0x08, 0x8a, 0x22, 0xb6, 0x5e,
															0x52, 0xbc, 0x51, 0x4d, 0x16, 0xcc, 0xf8, 0x06, 0x81, 0x8c, 0xe9, 0x1a, 0xb7, 0x79, 0x37, 0x36,
															0x5a, 0xf9, 0x0b, 0xbf, 0x74, 0xa3, 0x5b, 0xe6, 0xb4, 0x0b, 0x8e, 0xed, 0xf2, 0x78, 0x5e, 0x42,
															0x87, 0x4d
															};
			var output2 = new byte[lengthOfContent2];


			var key3 = new byte[validKeyLength] {
														0x1c, 0x92, 0x40, 0xa5, 0xeb, 0x55, 0xd3, 0x8a,
														0xf3, 0x33, 0x88, 0x86, 0x04, 0xf6, 0xb5, 0xf0,
														0x47, 0x39, 0x17, 0xc1, 0x40, 0x2b, 0x80, 0x09,
														0x9d, 0xca, 0x5c, 0xbc, 0x20, 0x70, 0x75, 0xc0
													};
			var nonce3 = new byte[validNonceLength] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02 };
			uint counter3 = 42;
			const int lengthOfContent3 = 127;
			var content3 = new byte[lengthOfContent3] {
														0x27, 0x54, 0x77, 0x61, 0x73, 0x20, 0x62, 0x72, 0x69, 0x6c, 0x6c, 0x69, 0x67, 0x2c, 0x20, 0x61,
														0x6e, 0x64, 0x20, 0x74, 0x68, 0x65, 0x20, 0x73, 0x6c, 0x69, 0x74, 0x68, 0x79, 0x20, 0x74, 0x6f,
														0x76, 0x65, 0x73, 0x0a, 0x44, 0x69, 0x64, 0x20, 0x67, 0x79, 0x72, 0x65, 0x20, 0x61, 0x6e, 0x64,
														0x20, 0x67, 0x69, 0x6d, 0x62, 0x6c, 0x65, 0x20, 0x69, 0x6e, 0x20, 0x74, 0x68, 0x65, 0x20, 0x77,
														0x61, 0x62, 0x65, 0x3a, 0x0a, 0x41, 0x6c, 0x6c, 0x20, 0x6d, 0x69, 0x6d, 0x73, 0x79, 0x20, 0x77,
														0x65, 0x72, 0x65, 0x20, 0x74, 0x68, 0x65, 0x20, 0x62, 0x6f, 0x72, 0x6f, 0x67, 0x6f, 0x76, 0x65,
														0x73, 0x2c, 0x0a, 0x41, 0x6e, 0x64, 0x20, 0x74, 0x68, 0x65, 0x20, 0x6d, 0x6f, 0x6d, 0x65, 0x20,
														0x72, 0x61, 0x74, 0x68, 0x73, 0x20, 0x6f, 0x75, 0x74, 0x67, 0x72, 0x61, 0x62, 0x65, 0x2e
														};
			var expected3 = new byte[lengthOfContent3] {
															0x62, 0xe6, 0x34, 0x7f, 0x95, 0xed, 0x87, 0xa4, 0x5f, 0xfa, 0xe7, 0x42, 0x6f, 0x27, 0xa1, 0xdf,
															0x5f, 0xb6, 0x91, 0x10, 0x04, 0x4c, 0x0d, 0x73, 0x11, 0x8e, 0xff, 0xa9, 0x5b, 0x01, 0xe5, 0xcf,
															0x16, 0x6d, 0x3d, 0xf2, 0xd7, 0x21, 0xca, 0xf9, 0xb2, 0x1e, 0x5f, 0xb1, 0x4c, 0x61, 0x68, 0x71,
															0xfd, 0x84, 0xc5, 0x4f, 0x9d, 0x65, 0xb2, 0x83, 0x19, 0x6c, 0x7f, 0xe4, 0xf6, 0x05, 0x53, 0xeb,
															0xf3, 0x9c, 0x64, 0x02, 0xc4, 0x22, 0x34, 0xe3, 0x2a, 0x35, 0x6b, 0x3e, 0x76, 0x43, 0x12, 0xa6,
															0x1a, 0x55, 0x32, 0x05, 0x57, 0x16, 0xea, 0xd6, 0x96, 0x25, 0x68, 0xf8, 0x7d, 0x3f, 0x3f, 0x77,
															0x04, 0xc6, 0xa8, 0xd1, 0xbc, 0xd1, 0xbf, 0x4d, 0x50, 0xd6, 0x15, 0x4b, 0x6d, 0xa7, 0x31, 0xb1,
															0x87, 0xb5, 0x8d, 0xfd, 0x72, 0x8a, 0xfa, 0x36, 0x75, 0x7a, 0x79, 0x7a, 0xc1, 0x88, 0xd1
															};
			var output3 = new byte[lengthOfContent3];


			var forEncrypting1 = new ChaCha20B(key1, nonce1, counter1);
			var forEncrypting2 = new ChaCha20B(key2, nonce2, counter2);
			var forEncrypting3 = new ChaCha20B(key3, nonce3, counter3);

			// Act
			forEncrypting1.EncryptBytes(output1, content1, lengthOfContent1);
			forEncrypting2.EncryptBytes(output2, content2, lengthOfContent2);
			forEncrypting3.EncryptBytes(output3, content3, lengthOfContent3);

			// Assert
			CollectionAssert.AreEqual(expected1, output1);
			CollectionAssert.AreEqual(expected2, output2);
			CollectionAssert.AreEqual(expected3, output3);
		}

		public static string Hex(byte[] data, int cnt = 0)
		{
			if (cnt == 0) cnt = data.Length;
			return string.Join("", data.Take(cnt).Select(x => x.ToString("x2")));
		}

		public static byte[] Hex(string hex)
		{
			return Enumerable.Range(0, hex.Length)
							 .Where(x => x % 2 == 0)
							 .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
							 .ToArray();
		}


	}


}
