# Using `extra_hosts`

Exploring the idea to have container-to-container communication to go through the host, using the FQDN we want. So the ideas in our example is that 

- we can use "meaningful" names, e.g. `luckynumbers.local` and `forecasts.local`
- each container is configured to map these names to the host (using `extra_hosts`)
- since all services expose their port(s) to the host, each container can reach other containers in the network through the host

Using `extra_hosts` in services, we can assign a container one or more arbitrary aliases: other containers in the "compose" network can then reach this container using these aliases.

> It looks like ASP.Net apps are configured to automatically redirect the request to secure endpoint: because of this, when using the unsecure URI, you receive a 307 response, but the new location contains the "local" secure port, not the one mapped to (hence available via) the host.

From container `tools`, I finally am able to connect via the host using the secure URL, not the unsecure one (because, as noted above, the _redirect_ URL is referencing the "private" secure port, not the exported one).

> **Note**
>
> Since the certificate is not trusted (self-signed), we need to pass `--insecure` to `curl`, i.e.
> 
> ```bash
> curl -v https://myhost:44331/luckynumber --insecure   
> ```
