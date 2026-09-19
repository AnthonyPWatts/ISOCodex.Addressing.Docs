# ISOCodex.Addressing.Mexico

Version 2.1.1 targets `netstandard2.0` for .NET Framework 4.7.2+ and modern .NET, replacing the redundant `netstandard2.1` asset. The matching core requires ISOCodex.Countries 1.1.1 or later.

Mexico-specific extension package for `ISOCodex.Addressing`.

## Installation

```bash
dotnet add package ISOCodex.Addressing.Mexico
```

## Registration

```csharp
services.AddAddressing();
services.AddMexicoAddressing();
```

## What it provides

- `MX` validator registration.
- Mexico address formatter.
- Mexico address profile metadata for forms.
- Five-digit postal-code validation, including leading-zero codes.
- State validation against package metadata.

Validation is structural. It does not call external services and does not prove that an address exists or is deliverable.

## Example

`csharp
using ISOCodex.Countries;

var address = new Address(
    "Palacio Nacional",
    null,
    "Ciudad de México",
    "CMX",
    new PostalCode("06066"),
    CountryAlpha2Code.Parse("MX"));
```

Formatted output:

```text
Palacio Nacional
06066 Ciudad de México, CMX
Mexico
```
