# Changelog

## 2.1.0 - 2026-09-19

- Added .NET Standard 2.0 assets to the core and all 11 country packages for .NET Framework 4.7.2+ and modern .NET; replaced the redundant .NET Standard 2.1 assets and retained all public APIs.
- Updated the Countries dependency to 1.1.0 and added package API validation against 2.0.1.
- Made README packaging explicit and made null guards explicit for older reference assemblies.
- Added packed Framework and .NET 10 consumer scenarios covering every country registration, profiles, validation and formatting.

## 2.0.1

- Updated `ISOCodex.Countries` to `1.0.1`.
- Aligned the generic fallback address profile with the core `Address` constructor by marking locality and postal code as required.
- Confirmed the remaining consumer-convenience ideas from the extended test rigs are deferred until there is demand for them.

## 2.0.0

- Replaced the Addressing-owned `CountryCode` value object with `ISOCodex.Countries.CountryAlpha2Code`.
- Added a published package dependency on `ISOCodex.Countries` `1.0.0`.
- Routed validators, formatters, and address profiles by Countries-owned alpha-2 identity.
- Delegated formatter country-line display names to `ISOCodex.Countries`.
- Limited generic fallbacks to current countries known by `ISOCodex.Countries`; special code elements such as `EU` and alias-like values such as `UK` are not treated as deliverable countries.

## 1.3.0

- Added country packages for India, Brazil, Mexico, Germany, and Italy.
- Enriched existing validators for safer default postal-code handling and improved country-specific postal/admin-area coverage.
- Expanded US postal administrative-area support for additional USPS state, possession, and military codes.
- Tightened Canada postal-code structure validation.
- Tightened Ireland Eircode structural validation.
- Refactored newer validators/formatters to reuse shared validation and formatting helpers.

## 1.2.0

Moves all country-specific behaviour into country packages and adds Ireland and France.

### Includes

- Core package now supports zero countries and provides shared registries, abstractions, and generic fallbacks only.
- Great Britain, United States, and Canada moved from core into country packages.
- Extended consumer-style test rigs for checkout APIs, dynamic profile-driven forms, and bulk CSV imports.
- Release validation updates for the expanded country package family.

## 1.1.0

Adds Ireland and France country packages.

### Includes

- Ireland validation, formatting, DI registration and profile metadata.
- France validation, formatting, DI registration and profile metadata.
- Convenience `CountryCode` constants for all supported ISO 3166-1 alpha-2 codes.
- Release validation and CI packaging updates for the full package family.

## 1.0.0

Initial stable release of ISOCodex.Addressing.

### Includes

- Core address model and value objects.
- Formatting and validation service infrastructure.
- Initial GB, US and CA country support, later moved to country packages.
- Optional Spain country package.
- Profile/form metadata support.
- Framework-neutral validation results suitable for ASP.NET, Blazor, FluentValidation adapters, import pipelines and custom validation layers.
- NuGet packaging and release validation scripts.
