[![Build status](https://dev.azure.com/xkit/River/_apis/build/status/River%20CI?branchName=develop)](https://dev.azure.com/xkit/River)

# River - network tunneling
River is a proxy client & server with cryptography. Everything is pure .Net CLR without external dependancies.

Shipped in 2 ways:

1) [River.dll in NuGet](https://www.nuget.org/packages/River/) - is a .Net Standard 2.0 library for any cross platform project

2) [River.exe for Windows](https://github.com/gusarov/river/releases) - is a .Net Framwork 4.8 application for Windows

# Application Usage

The commandline inspired by '[gost](https://github.com/ginuerzh/gost)' project:

Run SOCKS server:
```
river -L socks://0.0.0.0:1080
```

Run ShadowSocks server:
```
river -L ss://chacha20:password@0.0.0.0:8338
```

Proxy Chain - a list of forwarders:
```
river -L socks://0.0.0.0:1080 -F socks4://rhop2:1080 -F socks4://10.7.1.1:1080 
```

# Library Usage

NuGet: https://www.nuget.org/packages/River/

Installation: ```Install-Package River```

How to wrap your existing TCP connection to SOCKS proxy:

Original:
```cs
var cli = new TcpClient("httpbin.org", 80);
var stream = cli.GetStream();
```
Change to:
```cs
var stream = new Socks4ClientStream("127.0.0.1", 1080, "httpbin.org", 80);
```
Or if you need TcpClient to proxy:
```cs
var cli = new TcpClient("127.0.0.1", 1080);
var stream = new Socks4ClientStream(cli.GetStream(), "httpbin.org", 80);

```

Proxy Chain:
```cs
var step1 = new Socks4ClientStream();
step1.Plug("127.0.0.1", 1080); // 1st proxy
step1.Route("127.0.0.1", 1081); // 2nd proxy

var step2 = new Socks4ClientStream();
step2.Plug(step1);
step2.Route("127.0.0.1", 1082); // 3rd proxy

var step3 = new Socks4ClientStream(step2, "httpbin.org", 80); // you can do same in constructor - route to destination

var stream = step3;
```
