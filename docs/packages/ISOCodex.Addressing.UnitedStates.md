# ISOCodex.Addressing.UnitedStates

Version 2.1.1 targets `netstandard2.0` for .NET Framework 4.7.2+ and modern .NET, replacing the redundant `netstandard2.1` asset. The matching core requires ISOCodex.Countries 1.1.1 or later.

United States-specific extension package for `ISOCodex.Addressing`.

## Installation

```bash
dotnet add package ISOCodex.Addressing.UnitedStates
```

## Registration

```csharp
services.AddAddressing();
services.AddUnitedStatesAddressing();
```

## What it provides

- `US` validator registration.
- United States address formatter.
- United States address profile metadata for forms.
- ZIP and ZIP+4 validation.
- USPS state, territory, possession, and military-code validation.

Validation is structural. It does not call external services and does not prove that an address exists or is deliverable. It does not cross-check city, state, and ZIP combinations.

## Example

`csharp
using ISOCodex.Countries;

var address = new Address(
    "1600 Pennsylvania Avenue NW",
    null,
    "Washington",
    "DC",
    new PostalCode("20500"),
    CountryAlpha2Code.Parse("US"));
```

Formatted output:

```text
1600 Pennsylvania Avenue NW
Washington, DC 20500
United States
```
