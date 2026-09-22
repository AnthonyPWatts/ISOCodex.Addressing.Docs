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

Capture these pages at a **1600 × 900** desktop viewport with **View profile JSON** collapsed. Use 100% browser zoom and a device scale factor of 1, wait for the fonts to load, and save viewport-only PNGs (not full-page captures). Each exported file must be exactly 1600 × 900 pixels.

| Page | Output from the repository root |
| --- | --- |
| `/?CountryCode=ES&SampleId=es-valid` | `assets/spanish-profile-form.png` |
| `/?CountryCode=ES&SampleId=es-invalid-postal` | `assets/spanish-invalid-postal-code.png` |

The captures use the demo's fixed light colour scheme. At desktop widths of 1100 pixels and above, the shorter introduction, paired form fields and compact profile facts keep the complete form and validation result visible together. Address lines retain the full form width; input text and controls keep their normal size. Do not resize existing images or crop away fields, errors or results.

Check that the valid capture shows the Spanish labels, province selection and formatted address. The invalid capture must show `ABC`, its field error and the **Invalid** result together. Before replacing the assets, exercise the valid/invalid sample flows and correct an invalid field using **Validate address**. Also check at a 375-pixel mobile width, where the form and result stack vertically without horizontal scrolling.
