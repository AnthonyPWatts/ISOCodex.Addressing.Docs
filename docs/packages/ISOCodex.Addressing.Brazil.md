# ISOCodex.Addressing.Brazil

Version 2.1.0 targets `netstandard2.0` for .NET Framework 4.7.2+ and modern .NET, replacing the redundant `netstandard2.1` asset. The matching core requires ISOCodex.Countries 1.1.0 or later.

Brazil-specific extension package for `ISOCodex.Addressing`.

## Installation

```bash
dotnet add package ISOCodex.Addressing.Brazil
```

## Registration

```csharp
services.AddAddressing();
services.AddBrazilAddressing();
```

## What it provides

- `BR` validator registration.
- Brazil address formatter.
- Brazil address profile metadata for forms.
- CEP shape validation with or without a hyphen.
- State UF validation against package metadata.

Validation is structural. It does not call external services and does not prove that an address exists or is deliverable.

## Example

`csharp
using ISOCodex.Countries;

var address = new Address(
    "Praça da Sé",
    null,
    "São Paulo",
    "SP",
    new PostalCode("01001-000"),
    CountryAlpha2Code.Parse("BR"));
```

Formatted output:

```text
Praça da Sé
São Paulo - SP
01001-000
Brazil
```
