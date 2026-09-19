# ISOCodex.Addressing.Ireland

Version 2.1.0 targets `netstandard2.0` for .NET Framework 4.7.2+ and modern .NET, replacing the redundant `netstandard2.1` asset. The matching core requires ISOCodex.Countries 1.1.0 or later.

`ISOCodex.Addressing.Ireland` adds Irish address validation and formatting support to the core `ISOCodex.Addressing` library.

## What it provides

- `IrelandAddressValidator`
- `IrelandAddressFormatter`
- Irish address profile metadata
- `AddIrelandAddressing()` DI extension

The Irish validator uses pragmatic Eircode validation. It accepts seven alphanumeric characters with an optional space after the routing key, such as `D02 X285` or `D02X285`.

## Prerequisites

Register the core addressing services first.

```bash
dotnet add package ISOCodex.Addressing.Ireland
```

## Example

```csharp
using ISOCodex.Addressing;
using ISOCodex.Addressing.Formatting;
using ISOCodex.Addressing.Ireland;
using ISOCodex.Addressing.Profiles;
using ISOCodex.Addressing.Validation;
using ISOCodex.Countries;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddAddressing();
services.AddIrelandAddressing();

using var serviceProvider = services.BuildServiceProvider();

var validatorFactory = serviceProvider.GetRequiredService<IAddressValidatorFactory>();
var formatter = serviceProvider.GetRequiredService<IAddressFormatter>();
var profileProvider = serviceProvider.GetRequiredService<IAddressProfileProvider>();

var address = new Address(
    line1: "1 College Green",
    line2: null,
    city: "Dublin",
    stateOrProvince: null,
    postalCode: new PostalCode("D02 X285"),
    countryCode: CountryAlpha2Code.Parse("IE"));

var validationResult = validatorFactory
    .GetValidator(CountryAlpha2Code.Parse("IE"))
    .Validate(address);

var formatted = formatter.Format(address);
var profile = profileProvider.GetProfile(CountryAlpha2Code.Parse("IE"));
```

Default formatted output:

```text
1 College Green
Dublin
D02 X285
Ireland
```

## Validation behaviour

The Ireland validator currently:

- requires country code `IE`
- requires address line 1
- requires town/city
- requires an Eircode-shaped postal code using characters allowed by the structural validator
- accepts Eircodes with or without the internal space
- compares Eircodes case-insensitively without mutating the stored postal code

Validation is structural. It does not call external services and does not prove that an address exists or is deliverable. Formatting is presentation only. It does not validate the address or normalize the stored postal code.

## License

MIT
