# Docker `host` network mode

`host` network mode may simplify local development, but comes with some potential drawbacks

- Full support in Linux only
  
  > Available in Windows as experimental feature: moreover, it **requires** IPv4 (see [here](https://docs.docker.com/network/drivers/host/#limitations)).
  >
  > Because of this limitation, we cannot use `*` (or `+`) when specifying URLs for a site, e.g. using `ASPNETCORE_URLS`, because that results into an IPv6 address:
  > see, for example
  > - [ParseAddressDefaultsToAnyIPOnInvalidIPAddress](https://github.com/dotnet/aspnetcore/blob/28481ab0d6a31883a6c058d045ca8f72591a7eca/src/Servers/Kestrel/Core/test/AddressBinderTests.cs#L54)
  > - [AddressBinder.ParseAddress](https://github.com/dotnet/aspnetcore/blob/28481ab0d6a31883a6c058d045ca8f72591a7eca/src/Servers/Kestrel/Core/src/Internal/AddressBinder.cs#L100)
