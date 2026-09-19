# ISOCodex.Addressing

Version 2.1.0 targets `netstandard2.0` for .NET Framework 4.7.2+ and modern .NET, replacing the redundant `netstandard2.1` asset. The matching core requires ISOCodex.Countries 1.1.0 or later.

`ISOCodex.Addressing` provides a small .NET domain model for postal addresses plus registries for country-specific formatting, validation, and profile metadata supplied by country packages.

## Features

- Countries-owned ISO 3166-1 alpha-2 country identity via `ISOCodex.Countries`
- Lightweight postal code value object
- Address model suitable for application/domain use
- Country-specific address formatting via extension packages
- Country-specific validation via `IAddressValidator`
- Country-specific address profile metadata via `IAddressProfileProvider`
- DI registration for validators, formatters, profile providers, and generic fallbacks
- Opt-in generic fallbacks for current countries known by `ISOCodex.Countries` without country packs
- Zero built-in country rules; add country packages such as `ISOCodex.Addressing.GreatBritain`, `ISOCodex.Addressing.UnitedStates`, and `ISOCodex.Addressing.Canada`

## Installation

```bash
dotnet add package ISOCodex.Addressing
```

## Core types

- `Address`
- `Address.CountryCode` (`CountryAlpha2Code` from `ISOCodex.Countries`)
- `PostalCode`
- `IAddressFormatter`
- `IAddressProfileProvider`
- `AddressValidationResult`
- `AddressValidationIssue`
- `IAddressValidator`
- `IAddressValidatorFactory`

## Register country services with DI

```csharp
using ISOCodex.Addressing;
using ISOCodex.Addressing.Formatting;
using ISOCodex.Addressing.Profiles;
using ISOCodex.Addressing.Validation;
using ISOCodex.Countries;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddAddressing();

using var serviceProvider = services.BuildServiceProvider();

var validatorFactory = serviceProvider.GetRequiredService<IAddressValidatorFactory>();
var formatter = serviceProvider.GetRequiredService<IAddressFormatter>();
var profileProvider = serviceProvider.GetRequiredService<IAddressProfileProvider>();
```

`AddAddressing()` registers the core factories and providers. Add country package extension methods when you need country-specific rules, for example `AddGreatBritainAddressing()` from `ISOCodex.Addressing.GreatBritain`.

## Validate an address

```csharp
using ISOCodex.Addressing;
using ISOCodex.Countries;

var address = new Address(
    line1: "10 Downing Street",
    line2: null,
    city: "London",
    stateOrProvince: null,
    postalCode: new PostalCode("SW1A 2AA"),
    countryCode: CountryAlpha2Code.Parse("GB"));

var result = validatorFactory
    .GetValidator(address.CountryCode)
    .Validate(address);

if (!result.IsValid)
{
    foreach (var issue in result.Issues)
    {
        Console.WriteLine(issue.Code);
        Console.WriteLine(issue.PropertyName);
        Console.WriteLine(issue.Message);
    }
}
```

`Validate(...)` does not throw for ordinary validation failures. It returns structured issue data for APIs, forms, imports, and other user-facing workflows.

`AddressValidationResult` contains:

- `IsValid` - `true` when no issues were found
- `Issues` - a list of `AddressValidationIssue`

Each `AddressValidationIssue` contains:

- `Code` - stable machine-readable issue code, for example `Address.PostalCode.Invalid`
- `Message` - human-readable description
- `PropertyName` - related `Address` property when applicable

`AddressValidationIssue.Code` is intended for programmatic handling and should be treated as the stable machine-readable contract. `Message` is intended for display/logging and may be refined for clarity in future minor or patch releases.

Country validators collect multiple issues where possible.

## Use validation results with FluentValidation

`ISOCodex.Addressing` does not depend on FluentValidation. The core package returns structured validation results so applications can adapt address validation into FluentValidation, ASP.NET ModelState, Blazor forms, imports, or their own validation pipeline.

For example, a consuming application can call the address validator from a FluentValidation rule and map each `AddressValidationIssue` to a FluentValidation failure:

```csharp
using FluentValidation;
using ISOCodex.Addressing;
using ISOCodex.Addressing.Validation;

public sealed class Customer
{
    public Address Address { get; init; } = default!;
}

public sealed class CustomerValidator : AbstractValidator<Customer>
{
    public CustomerValidator(IAddressValidatorFactory addressValidatorFactory)
    {
        RuleFor(customer => customer.Address)
            .Custom((address, context) =>
            {
                if (address is null)
                {
                    context.AddFailure("Address", "Address is required.");
                    return;
                }

                var validator = addressValidatorFactory.GetValidator(address.CountryCode);
                var result = validator.Validate(address);

                foreach (var issue in result.Issues)
                {
                    var propertyName = string.IsNullOrWhiteSpace(issue.PropertyName)
                        ? "Address"
                        : $"Address.{issue.PropertyName}";

                    context.AddFailure(propertyName, issue.Message);
                }
            });
    }
}
```

If unsupported countries are possible in your application, register generic fallbacks or handle the explicit `GetValidator(...)` failure in your application validation layer.

Framework-neutral adapters can use the same issue data:

```csharp
using System.Collections.Generic;
using System.Linq;
using ISOCodex.Addressing.Validation;

public static IReadOnlyDictionary<string, string[]> ToErrorDictionary(
    AddressValidationResult result)
{
    return result.Issues
        .GroupBy(issue => issue.PropertyName ?? string.Empty)
        .ToDictionary(
            group => group.Key,
            group => group.Select(issue => issue.Message).ToArray());
}
```

This keeps the core package framework-agnostic. An optional adapter package may be considered later if there is enough demand, but consumers do not need one in order to use the current validators from FluentValidation.

## Format an address

```csharp
using ISOCodex.Addressing.Formatting;

var formatted = formatter.Format(address);
```

Default output is multi-line and includes the country. For a GB address:

```text
10 Downing Street
London
SW1A 2AA
United Kingdom
```

The formatter uses `Address.CountryCode` to choose the correct country-specific formatter. That lets application code stay simple while each formatter owns its country's postal layout.

Country package formatters can produce layouts such as:

```text
GB
10 Downing Street
London
SW1A 2AA
United Kingdom

US
1600 Pennsylvania Avenue NW
Washington, DC 20500
United States

CA
111 Wellington Street
Ottawa ON K1A 0A9
Canada
```

Country names are currently emitted as English display names: `United Kingdom`, `United States`, `Canada`, and country names supplied by extension packages. Localization and alternate country-name output can be added later without changing the core routing model.

### Single-line formatting

Single-line output is useful for grids, autocomplete results, logs, exports, and other compact displays:

```csharp
var singleLine = formatter.Format(
    address,
    new AddressFormatOptions
    {
        Style = AddressFormatStyle.SingleLine
    });
```

Output:

```text
10 Downing Street, London, SW1A 2AA, United Kingdom
```

You can also choose a different separator:

```csharp
var pipeSeparated = formatter.Format(
    address,
    new AddressFormatOptions
    {
        Style = AddressFormatStyle.SingleLine,
        SingleLineSeparator = " | "
    });
```

`AddressFormatOptions` is mutable by design for straightforward call-site configuration:

```csharp
var options = new AddressFormatOptions
{
    Style = AddressFormatStyle.SingleLine,
    IncludeCountry = false
};
```

## Address profiles / form metadata

Address profiles provide framework-agnostic metadata for building address entry forms and APIs. A profile tells consumers which conceptual address fields are used for a country, which are required, what labels to show, the display order, and optional placeholders or help text.

Profiles complement the `Address` model, formatters, and validators. They do not render UI, validate addresses, autocomplete, geocode, call remote services, or prove deliverability.

Fields may include selectable `Options` when the package already has stable subdivision metadata. US profiles expose state and territory options, Canadian profiles expose province and territory options, and Spain profiles expose province options from the Spain package. GB counties are deliberately left as optional text metadata rather than a closed option list.

```csharp
using ISOCodex.Addressing.Profiles;
using ISOCodex.Countries;

var profileProvider = serviceProvider.GetRequiredService<IAddressProfileProvider>();
var profile = profileProvider.GetProfile(CountryAlpha2Code.Parse("GB"));

foreach (var field in profile.Fields.OrderBy(field => field.DisplayOrder))
{
    Console.WriteLine($"{field.Label}: {(field.IsRequired ? "required" : "optional")}");
}
```

Country packages register country-specific profiles alongside their validators and formatters. The core package registers no country-specific profiles by itself.

Applications can serialize profile metadata for a frontend:

```json
{
  "countryCode": "GB",
  "source": "CountrySpecific",
  "fields": [
    {
      "field": "AddressLine1",
      "label": "Address line 1",
      "isRequired": true,
      "displayOrder": 10,
      "placeholder": "10 Downing Street"
    },
    {
      "field": "PostalCode",
      "label": "Postcode",
      "isRequired": true,
      "displayOrder": 60,
      "placeholder": "SW1A 2AA"
    }
  ]
}
```

Profiles can also include select-style options for administrative subdivisions:

```json
{
  "field": "AdministrativeArea",
  "label": "Province or territory",
  "inputKind": "Select",
  "options": [
    { "value": "ON", "label": "Ontario" },
    { "value": "NU", "label": "Nunavut" }
  ]
}
```

### Omitting the country

When the country is already shown elsewhere in your UI or data export, omit the country line:

```csharp
var localOnly = formatter.Format(
    address,
    new AddressFormatOptions
    {
        IncludeCountry = false
    });
```

Output:

```text
10 Downing Street
London
SW1A 2AA
```

### Custom country formatters

Extension packages and applications can register additional country formatters:

```csharp
services.AddAddressFormatter(
    CountryAlpha2Code.Parse("ES"),
    () => new MySpanishAddressFormatter());
```

Custom formatters implement `ICountryAddressFormatter`. `IAddressFormatter` remains the single service consumers use to format any registered country.

## Unsupported countries and generic fallbacks

`Address.CountryCode` is a `CountryAlpha2Code` from `ISOCodex.Countries`. Construct it from canonical alpha-2 input, or resolve alpha-3, numeric, alias, or display-name input through `ISOCodex.Countries` at your application boundary before constructing an `Address`.

Country-specific services are still explicit by default. If no formatter or validator is registered for an address's country, `IAddressFormatter.Format(...)` and `IAddressValidatorFactory.GetValidator(...)` throw. This is the right behaviour when an application expects strict, country-pack-backed handling.

When an application needs to accept structured addresses for countries without a country pack, register generic fallbacks:

```csharp
services.AddAddressing();
services.AddGenericAddressingFallbacks();
```

Registered country formatters and validators always take precedence. For unregistered countries, `AddGenericAddressingFallbacks()` registers:

- `PermissiveAddressValidator` - returns success for any non-null `Address`
- `GenericAddressFormatter` - emits `Line1`, optional `Line2`, `City StateOrProvince PostalCode`, and the Countries English short name
- generic `AddressProfile` metadata with `Source = GenericFallback`

Fallbacks only apply to current countries known by `ISOCodex.Countries`. Special or non-country code elements such as `EU` do not use postal-address fallbacks, and alias-like values such as `UK` are not silently resolved to `GB`.

Example output for a country without a specific formatter:

```text
1 Rue de Rivoli
Paris 75001
France
```

This is intended for store-first, import, migration, and validate-later workflows. It does not prove that an address exists or is deliverable.

Generic fallbacks do not make `Address` freeform. The core model still requires `Line1`, `City`, `PostalCode`, and `CountryCode`. If your application must preserve addresses that do not fit that structure, store the raw address text separately in your own data model.

The core `Address` model is intentionally a structured postal-address abstraction rather than a fully freeform global address record. It works best where an address can reasonably be represented using line, locality, postal-code and country components. Applications that must preserve arbitrary user-entered or legacy addresses should store the raw original text separately.

## Recommended persistence shape

The package does not prescribe a database schema, but consumers usually get the best results by storing the structured `Address` fields directly and keeping database constraints country-neutral.

Persist value objects as strings:

- `PostalCode.Code` -> `PostalCode`
- `Address.CountryCode.Value` -> `CountryCode`

Suggested relational columns:

| Column | Suggested type | Required | Notes |
| --- | --- | --- | --- |
| `Line1` | `nvarchar(200)` | Yes | First delivery/address line. |
| `Line2` | `nvarchar(200)` | No | Apartment, suite, building, organization, or other secondary line. |
| `City` | `nvarchar(100)` | Yes | Locality/town/city value used by the current `Address` model. |
| `StateOrProvince` | `nvarchar(100)` | No | Region, province, state, county, department, prefecture, or equivalent. |
| `PostalCode` | `nvarchar(32)` | Yes | Store the user's value; validators may normalize for checking without mutating this value. |
| `CountryCode` | `char(2)` | Yes | ISO 3166-1 alpha-2 code, stored uppercase. |

Recommended constraints:

- `Line1`, `City`, `PostalCode`, and `CountryCode` should be required
- `Line2` and `StateOrProvince` should be nullable
- `CountryCode` should be exactly two uppercase ASCII letters and should be validated/canonicalised through `ISOCodex.Countries`
- postal-code format should be validated in application code, not with database constraints
- human-entered address fields should use Unicode string columns

Example SQL shape:

```sql
Line1 nvarchar(200) not null,
Line2 nvarchar(200) null,
City nvarchar(100) not null,
StateOrProvince nvarchar(100) null,
PostalCode nvarchar(32) not null,
CountryCode char(2) not null
```

These lengths are practical defaults rather than domain guarantees. Use wider columns when you need to preserve imported, legacy, or partner-provided address data exactly.

## Recommended validation state storage

Address persistence should not imply successful validation. A saved address may be unvalidated, valid, invalid-but-preserved, or accepted without country-specific proof.

Suggested optional columns:

| Column | Suggested type | Notes |
| --- | --- | --- |
| `ValidationStatus` | `nvarchar(32)` | `NotValidated`, `Valid`, `Invalid`, or `AcceptedUnverified`. |
| `ValidationProfile` | `nvarchar(100)` | The rules used, such as `ISOCodex.Addressing.GB`, `ISOCodex.Addressing.Spain`, or `GenericFallback`. |
| `ValidatedAt` | `datetimeoffset` | When validation last ran. |
| `ValidationIssuesJson` | `nvarchar(max)` | Serialized `AddressValidationIssue` values when validation fails. |

Suggested status meanings:

- `NotValidated` - the address has been saved, but no validation result is recorded
- `Valid` - the address passed the recorded validation profile
- `Invalid` - validation ran and returned one or more issues
- `AcceptedUnverified` - the address was accepted without country-specific proof, commonly through a generic fallback

Example issue payload:

```json
[
  {
    "code": "Address.PostalCode.Invalid",
    "propertyName": "PostalCode",
    "message": "PostalCode must be a valid GB postcode (e.g., SW1A 1AA)."
  }
]
```

`ValidationProfile` should identify the rule set that produced the result. For workflows that care about stale validation, include a package or rules version, for example `ISOCodex.Addressing.GB@1.0.0`.

Keep this metadata outside the `Address` value object. It belongs to the consuming application's persistence or workflow state.

## JSON serialization guidance

`Address` is a domain model that contains value objects. If you serialize it directly with `System.Text.Json`, `PostalCode` and `CountryCode` are represented by their object properties:

```json
{
  "line1": "10 Downing Street",
  "line2": null,
  "city": "London",
  "stateOrProvince": null,
  "postalCode": { "code": "SW1A 2AA" },
  "countryCode": { "value": "GB" }
}
```

For public APIs or storage contracts that should expose scalar strings, map to an application DTO with `postalCode` and `countryCode` string properties. Use `CountryAlpha2Code.Parse(...)`, `CountryAlpha2Code.TryParse(...)`, or richer `ISOCodex.Countries` registry lookup at the boundary.

## Country-specific notes

### Great Britain (`GB`)

- Validates postcode format
- Expects uppercase postcode format with a space, for example `SW1A 1AA`

### United States (`US`)

- Validates ZIP or ZIP+4
- Requires `StateOrProvince`
- Accepts US state and territory abbreviations

### Canada (`CA`)

- Validates postal code format such as `A1A 1A1`
- Province/territory is optional
- If provided, province/territory must be a valid abbreviation

## Country packages

Country support is provided through additional packages. The current repository contains country packages for Great Britain, United States, Canada, Spain, Ireland, France, India, Brazil, Mexico, Germany, and Italy. This core package contains no country-specific rules by itself.

## Important behaviour

- `PostalCode` itself does not enforce country-specific formatting
- Validation is performed by the country validator
- Formatting is performed by the country formatter registered for `Address.CountryCode`
- Formatting does not mutate the address
- Formatting does not validate the address or prove that it exists
- Country display names come from `ISOCodex.Countries`
- Validators normalize common postal-code casing and spacing for validation without changing the stored `PostalCode.Code`
- `AddAddressing()` registers core services only; country packages register country-specific services
- `AddGenericAddressingFallbacks()` is opt-in and keeps registered country-specific services ahead of generic behaviour
- Persistence should store `PostalCode.Code` and `Address.CountryCode.Value` as strings
- Validation status is application state and should be stored separately from the `Address` value

## Compatibility policy

From `1.0.0`, public types, method signatures, value-object behaviour, and validation issue codes are treated as compatibility-sensitive. Version `2.0.0` made the breaking move from Addressing-owned country identity to `ISOCodex.Countries`.

Patch and minor releases may add new countries, metadata, helper APIs, validation cases, and documentation. They may also correct country-specific formatting or validation behaviour where the existing behaviour is demonstrably wrong.

Validation issue `Code` values are intended for programmatic handling and should remain stable unless a major version change is made. Human-readable validation messages may be refined for clarity in minor or patch releases.

## License

MIT
