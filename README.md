> Currently does not support HTTPS when running in docker

# Build the image

```bash
docker build -t spot/webapp2 -f .\WebApplication2\Dockerfile .  
```

# Run in container

```bash
docker run --rm -d --name webapp2 -p 45678:8080 spot/webapp2
```

> Port `8080` is configured in the base image, via `ASPNETCORE_HTTP_PORTS`.
> 
> We can override this setting.

# Run in container with HTTPS

```bash
docker run -d --name webapp2 -e "ASPNETCORE_HTTPS_PORTS=4433" -p 45678:8080 -p 54333:4433 spot/webapp2
```

> Currently throws because no certificate exists

Tutorials

- https://stackoverflow.com/questions/61197086/unable-to-configure-asp-net-https-endpoint-in-linux-docker-on-windows
- https://learn.microsoft.com/en-us/aspnet/core/security/docker-https?view=aspnetcore-8.0#running-pre-built-container-images-with-https
- https://github.com/dotnet/dotnet-docker/blob/main/samples/run-aspnetcore-https-development.md
