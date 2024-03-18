# Playing with `docker compose` 

> ⚠️ **Disclaimer** 📢
>
> This is based on what I have understood so far: I do not expect it to be _necessarily_ technically accurate 😎, although I hope I'm not **blatantly** wrong 😉.

Sample solution to play with `docker compose`, consisting of

- A web API application `WeatherForecast`
- Another web API application `LuckyNumbers`
- SQL Server 2019

## System diagram

```puml
participant Client as C
participant WeatherForecast as WF
participant LuckyNumbers as LN
database SQL
C -> WF : ""GET /weatherforecast""
WF --> LN : ""GET /luckynumber""
LN ---> WF : return **//lucky//** number
WF --> SQL : Retrieve some data
SQL ---> WF : return some data
WF ---> C : return ""(forecast, number, sqldata)""
```

```puml
actor Client as C
component WeatherForecast as WF
component LuckyNumbers as LN
database SQL

C -down[#blue]-> WF : (1) REST request

WF -down[dashed,#green]-> LN : (2) Collect other data
WF -right[dashed,#green]-> SQL : (3) Collect other data

LN -[dashed,#magenta]-> WF : (2) Return data
SQL -[dashed,#magenta]-> WF : (3) Return data

WF -[#blue]> C : (4) REST response
```

## Build the image

```bash
docker build -t spot/forecast -f .\WeatherForecast\Dockerfile .  
```

## Run in container

```bash
docker run --rm -d --name forecast -p 45678:8080 spot/forecast
```

> Port `8080` is configured in the base image, via `ASPNETCORE_HTTP_PORTS`.
> 
> We can override this setting.

## Run in container with HTTPS

```bash
docker run -d --name forecast -e "ASPNETCORE_HTTPS_PORTS=4433" -p 45678:8080 -p 54333:4433 spot/forecast
```

> Currently throws because no certificate exists

### Using a self-signed certificate

First, create or export an existing self-signed development certificate:

```powershell
dotnet dev-certs https -ep "$env:USERPROFILE\.aspnet\https\webapp2.pfx" -p latoccopiano -v
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
dotnet dev-certs https --check --trust
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

## Using `docker-compose`

> Set environment variables `CERTIFICATE_PASSWORD` and `MSSQL_SA_PASSWORD` (referenced in `compose*.yaml` and `docker.env`); for example, in powershell:
> 
> ```powershell
> $env:CERTIFICATE_PASSWORD="Some-strong-Password_123"
> $env:MSSQL_SA_PASSWORD="Sa-strong-Password"
> ``` 

There are two versions:

- `compose.yaml` sets network mode to `bridge` : services are available to the host via published ports in the host, and internally via containers' hostnames and exposed ports

  - Run `docker compose -f compose.yaml build` to build images

  - Run `docker compose -f compose.yaml up -d` to start containers

- `compose-hostmode.yaml` sets network mode to `host`: containers do not have their own IP address, and the ports each container binds to are available on the host's IP address. 

  > The host networking driver only works on Linux hosts, and is not supported on Docker Desktop for Mac, Docker Desktop for Windows, or Docker EE for Windows Server.

  [Source](https://docs.docker.com/network/drivers/host/)

  > With Podman, apparently it works in Windows, too.

## Now so obvious things

- To use hostnames in container-to-container communication, `network_mode` cannot be set to `bridge`.
- On the other hand, when `network_mode` is set to `bridge`, a container cannot reach the host
- Moreover, when `network_mode` is set to `host`, ports are not mapped:

  > Given that the container does not have its own IP-address when using host mode networking, port-mapping does not take effect, and the `-p`, `--publish`, `-P`, and `--publish-all` option are ignored.
  
  But

  > [...] if you run a container which binds to port 80 and you use host networking, the container’s application is available on port 80 on the host’s IP address. 

  [Source](https://docker-docs.uclv.cu/network/host/)

## Networking

> Containers have networking enabled by default, and they can make outgoing connections. A container has no information about what kind of network it's attached to, or whether their peers are also Docker workloads or not. A container only sees a network interface with an IP address, a gateway, a routing table, DNS services, and other networking details. That is, unless the container uses the `none` network driver.

From [Networking overview](https://docs.docker.com/network/) 

We're experimenting with two different network mode:

- "default" mode
- "host" mode

When using "default" mode, i.e. not setting `network_mode` explicitly, and then you inspect the container:

- network mode is set to "bridge" (see `HostConfig:NetworkMode`)
- bridge is not given a name (see `NetworkSettings:Bridge`)
- a new network is created, named after the project (see entry in `NetworkSettings:Networks`)

  The project is named based on `name` property in the compose file, or the name of the folder if `name` is not set)
 
  > In Compose, the default project name is derived from the base name of the project directory. However, you have the flexibility to set a custom project name.

  [Source](https://docs.docker.com/compose/project-name/) 

  Moreover:
  
  > Your app's network is given a name based on the "project name", which is based on the name of the directory it lives in. You can override the project name with either the `--project-name` flag or the `COMPOSE_PROJECT_NAME` environment variable. [...] 
  >
  > When you run docker compose up, the following happens:
  > 
  > 1. A network called `<myapp>_default` is created.
  >
  > [...] 
  >
  > Each container can now look up the service name, and get back the appropriate container's IP address. [...]
  > 
  > Networked service-to-service communication uses the `CONTAINER_PORT`. When `HOST_PORT` is defined, the service is accessible outside the swarm as well. 

  [Source](https://docs.docker.com/compose/networking/)
  
  > There are **two** `NetworkSettings` element when inspecting the container, with partially different content, not sure how / why...
  >
  > The only difference are (**apparently**) `NetworkSettings:Ports` and `NetworkSettings:PortMapping`, but these differences are not important (AFAICS: just a different order for properties) 

There are some important differences between used-defined bridges and the default bridge:

- > User-defined bridges provide automatic DNS resolution between containers.
  
  This explains why setting `network_mode` to bridge prevents using container hostnames in container-to-container communication.

- > The default bridge network is considered a legacy detail of Docker and is not recommended for production use. Configuring it is a manual operation, and it has technical shortcomings.
  >
  > [...] Containers connected to the default `bridge` network can communicate, but only by IP address, unless they are linked using the legacy `--link` flag. 

Other differences available at [Differences between user-defined bridges and the default bridge](https://docker-docs.uclv.cu/network/bridge/#differences-between-user-defined-bridges-and-the-default-bridge).  

## References

- [Hosting ASP.NET Core images with Docker Compose over HTTPS](https://learn.microsoft.com/en-us/aspnet/core/security/docker-compose-https?view=aspnetcore-8.0)
- https://stackoverflow.com/questions/61197086/unable-to-configure-asp-net-https-endpoint-in-linux-docker-on-windows
- https://learn.microsoft.com/en-us/aspnet/core/security/docker-https?view=aspnetcore-8.0#running-pre-built-container-images-with-https
- https://github.com/dotnet/dotnet-docker/blob/main/samples/run-aspnetcore-https-development.md
- [Why is Firefox not trusting my self-signed certificate?](https://stackoverflow.com/a/77009337)
- [Develop Locally with HTTPS, Self-Signed Certificates and ASP.NET Core](https://www.humankode.com/asp-net-core/develop-locally-with-https-self-signed-certificates-and-asp-net-core/)
- [Docker Networking – Basics, Network Types & Examples](https://spacelift.io/blog/docker-networking)
- [Specify a project name](https://docs.docker.com/compose/project-name/)
- [Version and name top-level elements](https://docs.docker.com/compose/compose-file/04-version-and-name/#name-top-level-element)
- [Differences between user-defined bridges and the default bridge](https://docker-docs.uclv.cu/network/bridge/#differences-between-user-defined-bridges-and-the-default-bridge)
- [Networking with standalone containers](https://docs.docker.com/network/network-tutorial-standalone/)