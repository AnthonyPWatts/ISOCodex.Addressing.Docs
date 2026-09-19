# ISOCodex.Addressing.GreatBritain

Version 2.1.1 targets `netstandard2.0` for .NET Framework 4.7.2+ and modern .NET, replacing the redundant `netstandard2.1` asset. The matching core requires ISOCodex.Countries 1.1.1 or later.

Great Britain-specific extension package for `ISOCodex.Addressing`.

## Installation

```bash
dotnet add package ISOCodex.Addressing.GreatBritain
```

## Registration

```csharp
services.AddAddressing();
services.AddGreatBritainAddressing();
```

## What it provides

- `GB` validator registration.
- Great Britain address formatter.
- Great Britain address profile metadata for forms.
- UK postcode shape validation with internal case/spacing normalisation.

Validation is structural. It does not call external services and does not prove that an address exists or is deliverable. Counties are not required or validated by this package.

## Example

`csharp
using ISOCodex.Countries;

var address = new Address(
    "10 Downing Street",
    null,
    "London",
    null,
    new PostalCode("SW1A 2AA"),
    CountryAlpha2Code.Parse("GB"));
```

Formatted output:

```text
10 Downing Street
London
SW1A 2AA
United Kingdom
```
