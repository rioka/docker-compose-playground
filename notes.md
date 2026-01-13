# Notes about trusting a self-signed certificate

**TODO** Clean-up and structure content.

> Original script from https://stackoverflow.com/a/62193586

```powershell
$certPass = "password_here"
$certSubj = "host.docker.internal"
$certAltNames = "DNS:localhost,DNS:host.docker.internal,DNS:identity_server" # i believe you can also add individual IP addresses here like so: IP:127.0.0.1
$opensslPath="path\to\openssl\binaries" #assuming you can download OpenSSL, I believe no installation is necessary
$workDir="path\to\your\project" # i assume this will be your solution root
$dockerDir=Join-Path $workDir "ProjectApi" #you probably want to check if my assumptions about your folder structure are correct

#generate a self-signed cert with multiple domains
Start-Process -NoNewWindow -Wait -FilePath (Join-Path $opensslPath "openssl.exe") -ArgumentList "req -x509 -nodes -days 365 -newkey rsa:2048 -keyout ",
                                          (Join-Path $workDir aspnetapp.key),
                                          "-out", (Join-Path $dockerDir aspnetapp.crt),
                                          "-subj `"/CN=$certSubj`" -addext `"subjectAltName=$certAltNames`""

# this time round we convert PEM format into PKCS#12 (aka PFX) so .net core app picks it up
Start-Process -NoNewWindow -Wait -FilePath (Join-Path $opensslPath "openssl.exe") -ArgumentList "pkcs12 -export -in ", 
                                           (Join-Path $dockerDir aspnetapp.crt),
                                           "-inkey ", (Join-Path $workDir aspnetapp.key),
                                           "-out ", (Join-Path $workDir aspnetapp.pfx),
                                           "-passout pass:$certPass"

$password = ConvertTo-SecureString -String $certPass -Force -AsPlainText
$cert = Get-PfxCertificate -FilePath (Join-Path $workDir "aspnetapp.pfx") -Password $password

# and still, trust it on your host machine
$store = New-Object System.Security.Cryptography.X509Certificates.X509Store [System.Security.Cryptography.X509Certificates.StoreName]::Root,"LocalMachine"
$store.Open("ReadWrite")
$store.Add($cert)
$store.Close()
```

## What does the script above do?

The two `Start-Process` statements above actually run these two commands:

- Create certificate

  ```powershell
  $certSubject = "host.docker.internal"
  openssl.exe req `
    -x509 ` 
    -nodes ` 
    -days 365 `
    -newkey rsa:2048 ` 
    -keyout \path\to\my.key `
    -out \path\to\my.crt `
    -subj "/CN=$certSubj" ` 
    -addext "subjectAltName=DNS:localhost,DNS:host.docker.internal,DNS:identity_server"
  ```
  
- Convert to pfx

  ```
  openssl.exe pkcs12 `
    -export `
    -in  \pth\to\my.crt `
    -inkey \path\to\my.key `
    -out \path\to\my.pfx `
    -passout pass:my-password-for-the-certificate
  ```  
  
## A concrete example

Generate a certificate with this command:

```powershell
openssl.exe req `
    -x509 `
    -nodes `
    -days 365 `
    -newkey rsa:2048 `
    -keyout \Projects\self-signed.key `
    -out \Projects\self-signed.crt `
    -subj "/CN=host.docker.internal" `
    -addext "subjectAltName=DNS:localhost,DNS:host.docker.internal,DNS:identity_server,DNS:luckynumbers.local,DNS:forecasts.local"
```

Then, convert it to pfx

```powershell
openssl.exe pkcs12  `
    -export `
    -in  \Projects\self-signed.crt `
    -inkey \Projects\self-signed.key `
    -out \Projects\self-signed.pfx `
    -passout pass:my-password-for-the-certificate
```

`self-signed.pfx` now is a self-signed certificate for all these domains:

- localhost
- host.docker.internal
- identity_server
- luckynumbers.local
- forecasts.local

if this certificate is trusted by the host who is sending HTTPS requests to any of these server, then these requests flow without errors.

So the process has proved to be effective:

- we create a self-signed certificate, with multiple hostnames, using `openssl` as shown above
- after converting it to `.pfx`, we use this certificate for ASP.NET servers
- the original `.crt` certificate is copied into each container as well, and as part of the image build, we add these steps:

  ```
  ADD ./aspnetapp.crt /usr/local/share/ca-certificates/asp_dev/
  RUN chmod -R 644 /usr/local/share/ca-certificates/asp_dev/
  RUN update-ca-certificates --fresh
  ```
  
  > Note, `asp_dev` folder is not necessary, we can simply copy the certificate to `/usr/local/share/ca-certificates`
  
## How to test

- crete a virtual network in docker

  ```
  docker network create my-cool-netwokr
  ```

- Run luckynumbers, using 

  - A custom network (because if you use the default bridge network, container-to-container communication requires IP addresses, as names are not resolved)
  - Custom certificate and password; note that, if we use `docker.env`, we have to override these variables in the command line:
  
    - `ASPNETCORE_Kestrel__Certificates__Default__Password`
    - `ASPNETCORE_Kestrel__Certificates__Default__Path`

  Recap: use `--network my-cool-network --network-alias ...` using one of the names added to the certificate; also add `-e "ASPNETCORE_Kestrel__Certificates__Default__Password=blah blah"` and `-e "ASPNETCORE_Kestrel__Certificates__Default__Path=/https/new-certificate.pfx"`
  
- Run a `mcr.microsoft.com/aspnet:8.0` based container, using the custom network

  ```
  docker run --name https-playground --network my-cool-netwokr -it -v C:\Projects:/Projects mcr.microsoft.com/dotnet/aspnet:8.0
  ```

- Install `wget` and `ping`

  Check that wget throws when trying to call luckynumbers (using the DNS name registered in the certificate) via `https`
  
- Trust the self-signed certificate

  Verify we can now call luckynumbers via `https`
  
## References  

- https://stackoverflow.com/a/62193586

## See also

- [How to setup the dev certificate when using Docker in development](https://github.com/dotnet/AspNetCore.Docs/issues/6199#issuecomment-1123993460)

  May suggest an alternative way?