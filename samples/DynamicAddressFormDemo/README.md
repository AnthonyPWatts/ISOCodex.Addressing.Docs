# DynamicAddressFormDemo

Interactive address form for Spain, France and Ireland.

Requires the .NET 10 SDK selected by the repository `global.json`. Run from the repository root:

```powershell
dotnet run --project samples/DynamicAddressFormDemo --no-launch-profile --urls http://localhost:5001
```

Open [the Spanish sample](http://localhost:5001/?CountryCode=ES&SampleId=es-valid). Use the scenario selector to try valid and invalid addresses. Screenshots are in the [main README](../../README.md).

All ISOCodex references use published NuGet packages. No private checkout, credentials or project references are needed.
