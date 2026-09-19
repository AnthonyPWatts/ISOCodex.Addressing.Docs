# DynamicAddressFormDemo

Interactive address form for Spain, France and Ireland.

Requires the .NET 10 SDK selected by the repository `global.json`. Run from the repository root:

```powershell
dotnet run --project samples/DynamicAddressFormDemo --no-launch-profile --urls http://localhost:5001
```

Open [the Spanish sample](http://localhost:5001/?CountryCode=ES&SampleId=es-valid). Use the scenario selector to try valid and invalid addresses. Screenshots are in the [main README](../../README.md).

All ISOCodex references use published NuGet packages. No private checkout, credentials or project references are needed.

The form follows the typography, colours and layout of [Anthony Watts' site](https://anthonypwatts.co.uk/). It uses your system's light or dark colour scheme. Instrument Sans and IBM Plex Mono load from Google Fonts, with system-font fallbacks when offline.

## Check the form

For each country, load the valid and invalid samples. Correct the invalid field and select **Validate address**. The result should change to **Valid** and show the formatted address. Validation checks the documented country rules; it does not verify that an address exists.

## Update the screenshots

Capture these pages at a 1440 × 1320 desktop viewport, including the complete page, with **View profile JSON** collapsed:

| Page | Output from the repository root |
| --- | --- |
| `/?CountryCode=ES&SampleId=es-valid` | `assets/spanish-profile-form.png` |
| `/?CountryCode=ES&SampleId=es-invalid-postal` | `assets/spanish-invalid-postal-code.png` |

The current captures use the dark colour scheme. Also check the form at a narrow mobile width before replacing them.
