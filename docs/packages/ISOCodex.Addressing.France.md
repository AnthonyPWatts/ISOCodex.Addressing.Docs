# ISOCodex.Addressing.France

Version 2.1.0 targets `netstandard2.0` for .NET Framework 4.7.2+ and modern .NET, replacing the redundant `netstandard2.1` asset. The matching core requires ISOCodex.Countries 1.1.0 or later.

`ISOCodex.Addressing.France` adds French address validation and formatting support to the core `ISOCodex.Addressing` library.

## What it provides

- `FranceAddressValidator`
- `FranceAddressFormatter`
- French address profile metadata
- `AddFranceAddressing()` DI extension

The French validator uses conservative five-digit postal-code validation. It does not attempt exhaustive validation of overseas territories, CEDEX, special cases, or commune/postal-code combinations.

## Prerequisites

Register the core addressing services first.

```bash
dotnet add package ISOCodex.Addressing.France
```

## Example

```csharp
using ISOCodex.Addressing;
using ISOCodex.Addressing.Formatting;
using ISOCodex.Addressing.France;
using ISOCodex.Addressing.Profiles;
using ISOCodex.Addressing.Validation;
using ISOCodex.Countries;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddAddressing();
services.AddFranceAddressing();

using var serviceProvider = services.BuildServiceProvider();

var validatorFactory = serviceProvider.GetRequiredService<IAddressValidatorFactory>();
var formatter = serviceProvider.GetRequiredService<IAddressFormatter>();
var profileProvider = serviceProvider.GetRequiredService<IAddressProfileProvider>();

var address = new Address(
    line1: "10 Rue de Rivoli",
    line2: null,
    city: "Paris",
    stateOrProvince: null,
    postalCode: new PostalCode("75001"),
    countryCode: CountryAlpha2Code.Parse("FR"));

var validationResult = validatorFactory
    .GetValidator(CountryAlpha2Code.Parse("FR"))
    .Validate(address);

var formatted = formatter.Format(address);
var profile = profileProvider.GetProfile(CountryAlpha2Code.Parse("FR"));
```

Default formatted output:

```text
10 Rue de Rivoli
75001 Paris
France
```

## Validation behaviour

The France validator currently:

- requires country code `FR`
- requires address line 1
- requires city/commune
- requires a five-digit postal code, including overseas-style five-digit examples

Validation is structural. It does not call external services and does not prove that an address exists or is deliverable. Formatting is presentation only. It does not validate the address or normalize the stored postal code.

## License

MIT
