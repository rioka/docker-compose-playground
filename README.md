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

## Tutorials

- [Hosting ASP.NET Core images with Docker Compose over HTTPS](https://learn.microsoft.com/en-us/aspnet/core/security/docker-compose-https?view=aspnetcore-8.0)
- https://stackoverflow.com/questions/61197086/unable-to-configure-asp-net-https-endpoint-in-linux-docker-on-windows
- https://learn.microsoft.com/en-us/aspnet/core/security/docker-https?view=aspnetcore-8.0#running-pre-built-container-images-with-https
- https://github.com/dotnet/dotnet-docker/blob/main/samples/run-aspnetcore-https-development.md
