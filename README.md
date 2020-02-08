Azure DevOps build:
[![Build status](https://dev.azure.com/xkit/River/_apis/build/status/River%20CI?branchName=develop)](https://dev.azure.com/xkit/River)

# River - network tunneling
River is shipped in 2 ways:

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

How to wrap you existing TCP connection to SOCKS proxy:

