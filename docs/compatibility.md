# Package compatibility

The current coordinated versions are Countries **1.1.1**, Addressing **2.1.1** and Currency **1.1.1**. These documentation and metadata patches preserve the targets and public APIs introduced by the 1.1.0 / 2.1.0 compatibility release.

| Packages | Supported applications |
| --- | --- |
| Countries | .NET Framework 4.7.2+ and modern .NET; includes Standard 2.0 and .NET 8 assets |
| Addressing core and all 11 country packs | .NET Framework 4.7.2+ and modern .NET through Standard 2.0 |
| Currency core, Addressing/Countries bridges, Dapper, exchange abstractions, both JSON adapters and validation | .NET Framework 4.7.2+ and modern .NET through Standard 2.0 |
| Currency.AspNetCore | .NET 9+; does not support classic ASP.NET |
| Currency.EntityFrameworkCore | .NET 10 / EF Core 10; does not support EF6 |
| Currency.Analyzers | Roslyn 5.3+ compiler host, independently of the application target |

Framework executables should enable `AutoGenerateBindingRedirects` and `GenerateBindingRedirectsOutputType`.

## Verification and limits

The compatibility release passed 654 unit tests, package/API validation for the 23 runtime packages, inspection of all 24 packages, and nine package-consuming applications targeting net472, net48 and net10.0. The Framework applications ran on installed Framework 4.8.1.

A separate console demo restored six published ISOCodex packages directly from NuGet.org and passed 16 checks per target for net462 and net472: country lookup/JSON, address profiles/validation/formatting for Spain, France and Ireland, and money arithmetic/precision. NuGet correctly rejected its plain net46 target.

The net462 experiment does not lower the supported 4.7.2 floor, prove execution on an original 4.6.2 runtime, or establish compatibility for every optional adapter. Framework 3.5 is not supported.

Runnable consumer examples are available in the public repositories: see [this repository's samples](../samples/README.md), the [address-form demo](https://github.com/AnthonyPWatts/ISOCodex.Addressing.Docs/tree/main/samples/DynamicAddressFormDemo) and the [Framework verifier](https://github.com/AnthonyPWatts/ISOCodex.Addressing.Docs/tree/main/samples/FrameworkCompatibilityDemo). They restore released packages from NuGet.org; their public verification workflows need no private source access. Library implementation and internal development tests remain in the private repositories.

Earlier package versions do not gain the new targets retrospectively. Refer to [Microsoft's .NET Standard guidance](https://learn.microsoft.com/en-us/dotnet/standard/net-standard) for the Framework platform boundary.
