# DynamicAddressFormDemo

Interactive address form for Spain, France and Ireland.

Requires the .NET 10 SDK selected by the repository `global.json`. Run from the repository root:

```powershell
dotnet run --project samples/DynamicAddressFormDemo --no-launch-profile --urls http://localhost:5001
```

Open [the Spanish sample](http://localhost:5001/?CountryCode=ES&SampleId=es-valid). Use the scenario selector to try valid and invalid addresses. Screenshots are in the [main README](../../README.md).

All ISOCodex references use published NuGet packages. No private checkout, credentials or project references are needed.

The form follows the typography and layout of [Anthony Watts' site](https://anthonypwatts.co.uk/), with a fixed light palette of warm cream and off-white so the demo stands out against a dark portfolio page. Instrument Sans and IBM Plex Mono load from Google Fonts, with system-font fallbacks when offline.

Validation results use a prominent green panel and tick for a valid address, or a red panel and cross for an invalid address. Invalid fields share the red highlight, and the result panel retains the specific validation message. The validator currently reports pass/fail only.

## Check the form

For each country, load the valid and invalid samples. Correct the invalid field and select **Validate address**. The result should change to **Valid** and show the formatted address. Validation checks the documented country rules; it does not verify that an address exists.

## Update the screenshots

Capture these pages at a 1440 × 1320 desktop viewport, including the complete page, with **View profile JSON** collapsed:

| Page | Output from the repository root |
| --- | --- |
| `/?CountryCode=ES&SampleId=es-valid` | `assets/spanish-profile-form.png` |
| `/?CountryCode=ES&SampleId=es-invalid-postal` | `assets/spanish-invalid-postal-code.png` |

The captures use the demo's fixed light colour scheme. Also check the form at a narrow mobile width before replacing them.
