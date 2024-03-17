# WebApp Sample - Playing with `docker compose` 

> Currently does not support HTTPS when running in docker

## Build the image

```bash
docker build -t spot/webapp2 -f .\WebApplication2\Dockerfile .  
```

## Run in container

```bash
docker run --rm -d --name webapp2 -p 45678:8080 spot/webapp2
```

> Port `8080` is configured in the base image, via `ASPNETCORE_HTTP_PORTS`.
> 
> We can override this setting.

## Run in container with HTTPS

```bash
docker run -d --name webapp2 -e "ASPNETCORE_HTTPS_PORTS=4433" -p 45678:8080 -p 54333:4433 spot/webapp2
```

> Currently throws because no certificate exists

### Using a self-signed certificate

First, create or export an existing self-signed development certificate:

```powershell
dotnet dev-certs https -ep "$env:USERPROFILE\.aspnet\https\webapp2.pfx"  -p latoccopiano -v
```

> In Linux, run
>
>  ```bash
>  dotnet dev-certs https -ep ${HOME}/.aspnet/https/aspnetapp.pfx -p latoccopiano -v
>  ```

> A certificate is created in Windows certificate store if there's no one available 
> `webapp2.pfx` is created in `%USERPROFILE%\.aspnet\https`

If a certificate already exist, check if it is already trusted:

```bash
dotnet dev-certs --check --trust
```

Update compose file and add variables so that our application can use the certificate (using sample values below):

- Map HTTPS port in the host, e.g.

  ```yaml
  # ...
  ports:
    - "45678:8080"  # http
    - "44330:4433"  # https
  ```

- To Make our development certificate available to the container, add these line to the compose file: 
  
  ```yaml
  # ...
  volumes:
  - ~/.aspnet/https:/https:ro
  ```
 
- Add environment variables

  ```console
  ASPNETCORE_ENVIRONMENT=Development
  ASPNETCORE_URLS=https://+:4433;http://+:8080
  # set the value used when creating/exporting the certificate
  ASPNETCORE_Kestrel__Certificates__Default__Password=latoccopiano
  # use mapped path
  ASPNETCORE_Kestrel__Certificates__Default__Path=/https/webapp2.pfx
  ```

## Now so obvious things

- To use hostname in container-to-container communication, `network_mode` cannot be set to `bridge`.
- On the other hand, when `network_mode` is set to `bridge`, a container cannot reach the host
- Moreover, when `network_mode` is set to `host`, ports are not mapped:

  > Given that the container does not have its own IP-address when using host mode networking, port-mapping does not take effect, and the `-p`, `--publish`, `-P`, and `--publish-all` option are ignored.
  
  But

  > [...] if you run a container which binds to port 80 and you use host networking, the container’s application is available on port 80 on the host’s IP address. 

  [Source](https://docker-docs.uclv.cu/network/host/)

## Tutorials

- [Hosting ASP.NET Core images with Docker Compose over HTTPS](https://learn.microsoft.com/en-us/aspnet/core/security/docker-compose-https?view=aspnetcore-8.0)
- https://stackoverflow.com/questions/61197086/unable-to-configure-asp-net-https-endpoint-in-linux-docker-on-windows
- https://learn.microsoft.com/en-us/aspnet/core/security/docker-https?view=aspnetcore-8.0#running-pre-built-container-images-with-https
- https://github.com/dotnet/dotnet-docker/blob/main/samples/run-aspnetcore-https-development.md
- [Why is Firefox not trusting my self-signed certificate?](https://stackoverflow.com/a/77009337)
- [Develop Locally with HTTPS, Self-Signed Certificates and ASP.NET Core](https://www.humankode.com/asp-net-core/develop-locally-with-https-self-signed-certificates-and-asp-net-core/)