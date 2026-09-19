# ISOCodex.Addressing.India

Version 2.1.1 targets `netstandard2.0` for .NET Framework 4.7.2+ and modern .NET, replacing the redundant `netstandard2.1` asset. The matching core requires ISOCodex.Countries 1.1.1 or later.

India-specific extension package for `ISOCodex.Addressing`.

## Installation

```bash
dotnet add package ISOCodex.Addressing.India
```

## Registration

```csharp
services.AddAddressing();
services.AddIndiaAddressing();
```

## What it provides

- `IN` validator registration.
- India address formatter.
- India address profile metadata for forms.
- PIN-code shape validation using six digits.
- State and union territory validation against package metadata.

Validation is structural. It does not call external services and does not prove that an address exists or is deliverable.

## Example

`csharp
using ISOCodex.Countries;

var address = new Address(
    "Rashtrapati Bhavan",
    null,
    "New Delhi",
    "DL",
    new PostalCode("110004"),
    CountryAlpha2Code.Parse("IN"));
```

Formatted output:

```text
Rashtrapati Bhavan
New Delhi 110004
DL
India
```
