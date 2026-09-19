# ISOCodex.Addressing.Italy

Version 2.1.1 targets `netstandard2.0` for .NET Framework 4.7.2+ and modern .NET, replacing the redundant `netstandard2.1` asset. The matching core requires ISOCodex.Countries 1.1.1 or later.

Italy-specific extension package for `ISOCodex.Addressing`.

## Installation

```bash
dotnet add package ISOCodex.Addressing.Italy
```

## Registration

```csharp
services.AddAddressing();
services.AddItalyAddressing();
```

## What it provides

- `IT` validator registration.
- Italy address formatter.
- Italy address profile metadata for forms.
- Five-digit CAP validation.
- Province validation against package metadata.

Validation is structural. It does not call external services and does not prove that an address exists or is deliverable.

## Example

`csharp
using ISOCodex.Countries;

var address = new Address(
    "Piazza del Colosseo 1",
    null,
    "Roma",
    "RM",
    new PostalCode("00184"),
    CountryAlpha2Code.Parse("IT"));
```

Formatted output:

```text
Piazza del Colosseo 1
00184 Roma RM
Italy
```
