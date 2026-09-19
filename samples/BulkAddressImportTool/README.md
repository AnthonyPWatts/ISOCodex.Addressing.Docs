# BulkAddressImportTool

CSV address validation with review outputs.

Requires the .NET 10 SDK selected by the repository `global.json`. Run from the repository root:

```powershell
dotnet run --project samples/BulkAddressImportTool
```

The bundled CSV contains illustrative valid and invalid rows. Review files are written to the executable output directory under `Output/`; source CSV files are not changed. Pass a CSV path after `--` to use your own local sample.

All ISOCodex references use published NuGet packages. No private checkout, credentials or project references are needed.
