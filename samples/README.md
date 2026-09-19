# Runnable examples

Install the stable .NET 10 SDK (10.0.401 or later in 10.0). Clone this public repository, then choose an example:

| Example | Demonstrates |
| --- | --- |
| [DynamicAddressFormDemo](DynamicAddressFormDemo/README.md) | Interactive address form for Spain, France and Ireland. |
| [CheckoutAddressApi](CheckoutAddressApi/README.md) | Address profiles, validation and formatted previews over HTTP. |
| [BulkAddressImportTool](BulkAddressImportTool/README.md) | CSV address validation with review outputs. |
| [FrameworkCompatibilityDemo](FrameworkCompatibilityDemo/README.md) | Windows Framework consumer using Countries, Addressing and Currency. |

To restore, build and exercise all examples, including real HTTP requests, run:

```powershell
pwsh ./eng/verify-samples.ps1
```

The verifier restores packages from NuGet.org into a fresh cache and records package provenance and smoke-test output under `artifacts/`. It starts web examples on temporary loopback ports and stops only the processes it created. Framework execution requires Windows; the Addressing verification workflow uses Windows.

Samples are covered by the repository MIT licence. Bundled third-party web assets retain their accompanying licences.
