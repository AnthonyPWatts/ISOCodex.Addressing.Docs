# ISOCodex.Addressing.Spain

Version 2.1.0 targets `netstandard2.0` for .NET Framework 4.7.2+ and modern .NET, replacing the redundant `netstandard2.1` asset. The matching core requires ISOCodex.Countries 1.1.0 or later.

`ISOCodex.Addressing.Spain` adds Spanish address validation and formatting support to the core `ISOCodex.Addressing` library.

## What it provides

- `SpanishAddressValidator`
- `SpanishAddressFormatter`
- Spanish address profile metadata
- `AddSpainAddressing()` DI extension

The Spanish address profile includes province options on the administrative-area field. These options are form metadata for consumers; validation remains handled by `SpanishAddressValidator`.

Profile consumers can inspect those options through `IAddressProfileProvider` and present them as a dropdown or send them to a frontend API. The profile does not replace validation, formatting, or application-specific localization.

## Prerequisites

Register the core addressing services first.

```bash
dotnet add package ISOCodex.Addressing.Spain
```

## Example

```csharp
using ISOCodex.Addressing;
using ISOCodex.Addressing.Formatting;
using ISOCodex.Addressing.Profiles;
using ISOCodex.Addressing.Spain;
using ISOCodex.Addressing.Validation;
using ISOCodex.Countries;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddAddressing();
services.AddSpainAddressing();

using var serviceProvider = services.BuildServiceProvider();

var validatorFactory = serviceProvider.GetRequiredService<IAddressValidatorFactory>();
var formatter = serviceProvider.GetRequiredService<IAddressFormatter>();
var profileProvider = serviceProvider.GetRequiredService<IAddressProfileProvider>();

var address = new Address(
    line1: "Calle Mayor 1",
    line2: null,
    city: "Madrid",
    stateOrProvince: "Madrid",
    postalCode: new PostalCode("28013"),
    countryCode: CountryAlpha2Code.Parse("ES"));

var validationResult = validatorFactory
    .GetValidator(CountryAlpha2Code.Parse("ES"))
    .Validate(address);

var formatted = formatter.Format(address);
var profile = profileProvider.GetProfile(CountryAlpha2Code.Parse("ES"));
```

Validation returns structured issue data:

```csharp
var result = validatorFactory
    .GetValidator(CountryAlpha2Code.Parse("ES"))
    .Validate(address);
```

For invalid Spanish addresses, issues include codes such as `Address.PostalCode.Invalid`, `Address.StateOrProvince.Invalid`, and `Address.PostalCode.ProvinceMismatch`.

`AddressValidationIssue.Code` is intended for programmatic handling and follows the core package compatibility policy. Human-readable messages may be refined for clarity in future minor or patch releases.

Default formatted output:

```text
Calle Mayor 1
28013 Madrid
Spain
```

## Formatting behaviour

The Spanish formatter produces a postal-friendly layout using the core `IAddressFormatter` service. `AddSpainAddressing()` registers the Spanish formatter for `CountryAlpha2Code.Parse("ES")`, so consumers do not need to resolve Spain-specific services directly.

The country name is currently emitted as the English display name `Spain`.

Multi-line output:

```text
Calle Mayor 1
28013 Madrid
Spain
```

Single-line output:

```csharp
var singleLine = formatter.Format(
    address,
    new AddressFormatOptions
    {
        Style = AddressFormatStyle.SingleLine
    });
```

```text
Calle Mayor 1, 28013 Madrid, Spain
```

Country output can be omitted when the country is already known:

```csharp
var localOnly = formatter.Format(
    address,
    new AddressFormatOptions
    {
        IncludeCountry = false
    });
```

```text
Calle Mayor 1
28013 Madrid
```

Formatting is presentation only. It does not validate the address, normalize the stored postal code, or prove the address exists. Use the Spanish validator when validation matters.

## Validation behaviour

The Spain validator currently:

- requires country code `ES`
- requires a 5-digit postal code
- validates province names and common aliases when provided
- checks that the postal code prefix matches the supplied province when a province is supplied

Validation is structural. It does not call external services and does not prove that an address exists or is deliverable.

## License

MIT
