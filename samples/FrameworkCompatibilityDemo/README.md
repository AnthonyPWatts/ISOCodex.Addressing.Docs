# FrameworkCompatibilityDemo

Windows Framework consumer using Countries, Addressing and Currency.

Requires the .NET 10 SDK selected by the repository `global.json`. Run from the repository root:

```powershell
pwsh ./samples/FrameworkCompatibilityDemo/verify.ps1
```

Windows and PowerShell 7 are required. The verifier exercises net462 (exploratory) and net472 (supported) and confirms that net46 is rejected. Both target executables use the installed Framework runtime; this does not prove execution on separate original 4.6.2 and 4.7.2 installations. The supported package floor remains .NET Framework 4.7.2+.

All ISOCodex references use published NuGet packages. No private checkout, credentials or project references are needed.
