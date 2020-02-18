using System;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using River.Internal;

namespace River.Test
{
	[TestClass]
	public class HappyEyeballsTest
	{
		HappyEyeballs _sut = new HappyEyeballs();

		[TestMethod]
		public void Should_allways_return_ip_if_host_is_ipv4()
		{
			var host = "192.168.10.12";
			var act = _sut.GetPreferredEndpoint(null, host, 80);
			Assert.IsTrue(act is IPEndPoint);
			Assert.AreEqual(AddressFamily.InterNetwork, act.AddressFamily);
			Assert.AreEqual("192.168.10.12:80", act.ToString());
		}

		[TestMethod]
		public void Should_allways_return_ip_if_host_is_ipv6()
		{
			var host = "2001:4860:4802:38::75";
			var act = _sut.GetPreferredEndpoint(null, host, 80);
			Assert.IsTrue(act is IPEndPoint);
			Assert.AreEqual(AddressFamily.InterNetworkV6, act.AddressFamily);
			Assert.AreEqual("[2001:4860:4802:38::75]:80", act.ToString());
		}

		[TestMethod]
		public void Should_allways_return_ip_if_host_is_ipv6_sq()
		{
			var host = "[2001:4860:4802:38::75]";
			var act = _sut.GetPreferredEndpoint(null, host, 80);
			Assert.IsTrue(act is IPEndPoint);
			Assert.AreEqual(AddressFamily.InterNetworkV6, act.AddressFamily);
			Assert.AreEqual("[2001:4860:4802:38::75]:80", act.ToString());
		}

		[TestMethod]
		public void Should_return_domain_name_if_host_is_domain_name()
		{
			var host = "asdasd";
			var act = _sut.GetPreferredEndpoint(null, host, 80);
			var actd = act as DnsEndPoint;
			Assert.IsTrue(act != null);
			Assert.AreEqual(default, act.AddressFamily);
			Assert.AreEqual("asdasd", actd.Host);
		}

		[TestMethod]
		public void Should_return_IPv6_if_host_is_domain_name_and_dns_is_not_allowed()
		{
			var host = "google.com";
			var act = _sut.GetPreferredEndpoint(null, host, 80, false);
			var actd = act as IPEndPoint;
			Assert.IsTrue(act != null);
			Assert.AreEqual(AddressFamily.InterNetworkV6, act.AddressFamily);
			Assert.AreNotEqual(default, actd);
			// Assert.IsTrue(Regex.IsMatch(actd.Address.ToString(), @"2001:4860:4802:[\da-f]+::[\da-f]+"), actd.Address.ToString());
		}

		[TestMethod]
		public void Should_return_IPv4_if_ipv6_reported_as_inaccessible()
		{
			Assert.Inconclusive();
			var host = "google.com";
			var act = _sut.GetPreferredEndpoint(null, host, 80, false);
			var actd = act as IPEndPoint;
			Assert.IsTrue(act != null);
			Assert.AreEqual(AddressFamily.InterNetworkV6, act.AddressFamily);
			Assert.IsTrue(Regex.IsMatch(actd.Address.ToString(), @"2607:f8b0:400b:[\da-f]+::200e"), actd.Address.ToString());
		}
	}
}
