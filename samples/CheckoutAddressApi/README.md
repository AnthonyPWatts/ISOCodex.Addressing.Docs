# CheckoutAddressApi

Address profiles, validation and formatted previews over HTTP.

Requires the .NET 10 SDK selected by the repository `global.json`. Run from the repository root:

```powershell
dotnet run --project samples/CheckoutAddressApi --no-launch-profile --urls http://localhost:5002
```

Open [the endpoint index](http://localhost:5002/) or use the requests in [CheckoutAddressApi.http](CheckoutAddressApi.http). This local demonstration has no persistence or authentication and is not a production service.

All ISOCodex references use published NuGet packages. No private checkout, credentials or project references are needed.
