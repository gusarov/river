# River - HTTP based network tunneling
This services allows to setup an outgoing tunnel from restricted area to the uncensored interned and override trafic filtration pretending to be usual HTTP trafic.

# Motivation
System administrators constantly fights with employees by banning some trafic and enterprenious employees are fighting back by setting up a tunnel or doing some other tricks. During last 12 years I've seen a lot of different network configuration at my workplaces, from disallowing everything except 80 & 443 to mandatory corporate-wide proxy server (with disabled 'CONNECT' verb). Nowdays we recently were fighted back by some aggressive filtration of 80-port requests and throwing away any SSH/SSL/HTTP proxy Connect/SOCKS activity on that port.

# How it works
So, this is my move - simple trick by incapsulating trafic inside usual HTTP response and requests. The service (River Source) accepts SOCKS as internal proxy connection, XORs the body and sends it forward as HTTP PUT request to the responding part. The responding part (River Mouth) extracts this information, sets up a real outgoing connection, deXORs and forward it to the destincation.
