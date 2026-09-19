# Requires PowerShell 7, the .NET 10 SDK and Windows for Framework execution.
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
if (-not $IsWindows) { throw 'This demo must execute on Windows.' }

$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$runRoot = Join-Path $repoRoot ('artifacts/framework-demo/' + [guid]::NewGuid().ToString('N'))
$consumer = Join-Path $runRoot 'consumer'
$probe = Join-Path $runRoot 'net46-probe'
$packages = Join-Path $runRoot 'packages'
New-Item -ItemType Directory -Path $consumer, $probe, $packages -Force | Out-Null
# Isolate the consumer from the repository's build and packaging properties.
foreach ($file in @('Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props')) {
    '<Project />' | Set-Content (Join-Path $runRoot $file)
}
foreach ($file in @('FrameworkCompatibilityDemo.csproj', 'Program.cs', 'NuGet.Config')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $file) -Destination $consumer
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'FrameworkCompatibilityDemo.csproj') -Destination $probe
$config = Join-Path $consumer 'NuGet.Config'
$project = Join-Path $consumer 'FrameworkCompatibilityDemo.csproj'

function Invoke-DotNet([string]$LogName, [string[]]$Arguments) {
    & dotnet @Arguments 2>&1 | Tee-Object -FilePath (Join-Path $runRoot $LogName)
    if ($LASTEXITCODE -ne 0) { throw "dotnet failed; see $LogName." }
}

Invoke-DotNet 'restore.log' @('restore', $project, '--configfile', $config, '--packages', $packages,
    '--no-http-cache', '-p:DisableImplicitLibraryPacksFolder=true')
Invoke-DotNet 'build.log' @('build', $project, '-c', 'Release', '--no-restore', '--no-incremental')

$assets = Get-Content -Raw (Join-Path $consumer 'obj/project.assets.json') | ConvertFrom-Json
$sources = @($assets.project.restore.sources.PSObject.Properties.Name)
if ($sources.Count -ne 1 -or $sources[0] -ne 'https://api.nuget.org/v3/index.json') {
    throw 'The consumer did not restore exclusively from NuGet.org.'
}
[xml]$projectXml = Get-Content -Raw $project
$expected = @{}
foreach ($reference in $projectXml.Project.ItemGroup.PackageReference) {
    if ($reference.Include -like 'ISOCodex.*') { $expected[$reference.Include] = $reference.Version }
}
$provenance = foreach ($library in $assets.libraries.PSObject.Properties) {
    if ($library.Value.type -ne 'package') { throw "Unexpected non-package dependency: $($library.Name)" }
    $metadata = Get-Content -Raw (Join-Path $packages ($library.Value.path + '/.nupkg.metadata')) | ConvertFrom-Json
    if ($metadata.source -ne 'https://api.nuget.org/v3/index.json') {
        throw "Unexpected package origin: $($library.Name)"
    }
    [pscustomobject]@{ package = $library.Name; source = $metadata.source }
}
$selections = foreach ($target in $assets.targets.PSObject.Properties) {
    foreach ($id in ($expected.Keys | Sort-Object)) {
        $identity = $id + '/' + $expected[$id]
        $package = $target.Value.PSObject.Properties[$identity].Value
        if ($null -eq $package) { throw "Missing $identity for $($target.Name)." }
        foreach ($kind in @('compile', 'runtime')) {
            $paths = @($package.$kind.PSObject.Properties.Name)
            if ($paths.Count -ne 1 -or $paths[0] -ne "lib/netstandard2.0/$id.dll") {
                throw "Unexpected $kind selection for $identity on $($target.Name): $paths"
            }
            [pscustomobject]@{ target = $target.Name; package = $identity; kind = $kind; asset = $paths[0] }
        }
    }
}
$provenance | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $runRoot 'package-provenance.json')
$selections | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $runRoot 'asset-selection.json')

foreach ($target in @('net462', 'net472')) {
    $exe = Join-Path $consumer "bin/Release/$target/FrameworkCompatibilityDemo.exe"
    & $exe 2>&1 | Tee-Object -FilePath (Join-Path $runRoot "$target.log")
    if ($LASTEXITCODE -ne 0) { throw "$target demo failed." }
}
Invoke-DotNet 'audit.log' @('list', $project, 'package', '--vulnerable', '--include-transitive', '--no-restore')

# Plain 4.6 is intentionally tested as a rejected target, without forcing asset fallbacks.
$probeProject = Join-Path $probe 'FrameworkCompatibilityDemo.csproj'
$probeLog = Join-Path $runRoot 'net46-restore.log'
try {
    $PSNativeCommandUseErrorActionPreference = $false
    & dotnet restore $probeProject '-p:TargetFrameworks=net46' '-p:DisableImplicitLibraryPacksFolder=true' `
        --configfile $config --packages $packages --no-http-cache *> $probeLog
    $probeExitCode = $LASTEXITCODE
}
finally {
    $PSNativeCommandUseErrorActionPreference = $true
}
$probeOutput = Get-Content -Raw $probeLog
if ($probeExitCode -eq 0 -or $probeOutput -notmatch 'NU1202.*ISOCodex.Addressing') {
    throw 'The net46 probe did not fail with the expected package incompatibility; inspect net46-restore.log.'
}
Write-Host 'PASS: net46 correctly rejected with NU1202; net462 and net472 demos passed.'
Write-Host "Evidence: $runRoot"
# Do not propagate the intentionally non-zero restore status to CI shell wrappers.
exit 0
