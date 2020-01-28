Appveyer build:
[![Build status](https://ci.appveyor.com/api/projects/status/6wag92vlg4btmr54?svg=true)](https://ci.appveyor.com/project/gusarov/river)

Azure DevOps build:
[![Build status](https://dev.azure.com/xkit/River/_apis/build/status/River%20CI?branchName=develop)](https://dev.azure.com/xkit/River)

For restricted area:
[Download Source](https://ci.appveyor.com/api/projects/gusarov/river/artifacts/River.SourceInstaller/bin/Source.zip)

For cloud server:
[Download Mouth](https://ci.appveyor.com/api/projects/gusarov/river/artifacts/River.MouthInstaller/bin/Mouth.zip)


# River - HTTP based network tunneling
This services allows to setup an outgoing tunnel from restricted area to the uncensored interned and override trafic filtration pretending to be usual HTTP trafic.

# Motivation
System administrators constantly fights with employees by banning some trafic and enterprenious employees are fighting back by setting up a tunnel or doing some other tricks. During last 12 years I've seen a lot of different network configuration at my workplaces, from disallowing everything except 80 & 443 to mandatory corporate-wide proxy server (with disabled 'CONNECT' verb). Nowdays we recently were fighted back by some aggressive filtration of 80-port requests and throwing away any SSH/SSL/HTTP proxy Connect/SOCKS activity on that port.
Developers should win because we can create stuff while admins - only configure existing ;)

# How it works
So, this is my move - simple trick by incapsulating trafic inside usual HTTP response and requests. The service (River Source) accepts SOCKS as internal proxy connection, XORs the body and sends it forward as HTTP PUT request to the responding part. The responding part (River Mouth) extracts this information, sets up a real outgoing connection, deXORs and forward it to the destincation.

# Tasks

- [ ] Intensive testing - there are some bugs with lost packages
- [ ] Incapsulate socket id in HTTP session. There is a network that is processed on a higher level and HTTP connections are reshaped to a single connection. To overcome this, socket information must be encapsulated inside HTTP and connection itself shuld not identify real outgoing destination.
- [ ] Support proper HTTP session. Current implementation don't guarantee the sequence like response-request. There might be several requests or several responses depending on received data. Filters that do analyze HTTP session deeply can be overcomed by a long pool + incapsulation target socket in request.
