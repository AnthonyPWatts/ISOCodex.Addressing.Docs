# Requires PowerShell 7 and the .NET 10 SDK; Framework examples require Windows.
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
$repoRoot = Split-Path $PSScriptRoot -Parent
$runRoot = Join-Path $repoRoot ('artifacts/samples/' + [guid]::NewGuid().ToString('N'))
$packages = Join-Path $runRoot 'packages'
$logs = Join-Path $runRoot 'logs'
New-Item -ItemType Directory -Path $logs -Force | Out-Null
$dotnet = (Get-Command dotnet).Source
$provenance = @()

function Invoke-DotNet {
    & $dotnet @args
    if ($LASTEXITCODE -ne 0) { throw "dotnet $args failed ($LASTEXITCODE)." }
}

foreach ($project in Get-ChildItem (Join-Path $repoRoot 'samples') -Recurse -Filter '*.csproj' | Sort-Object FullName) {
    $name = $project.BaseName
    [xml]$xml = Get-Content $project.FullName -Raw
    if ($xml.SelectNodes('//ProjectReference').Count) { throw "$name must use published packages only." }
    if ($name -eq 'FrameworkCompatibilityDemo') {
        if (-not $IsWindows) { throw 'Run the Addressing examples on Windows to include Framework execution.' }
        & pwsh -NoProfile -File (Join-Path $project.DirectoryName 'verify.ps1') *> (Join-Path $logs "$name.log")
        if ($LASTEXITCODE -ne 0) { throw "Framework verification failed; see $logs." }
        Write-Host "Passed: $name"
        continue
    }

    Invoke-DotNet restore $project.FullName --configfile (Join-Path $repoRoot 'NuGet.Config') --packages $packages '-p:DisableImplicitLibraryPacksFolder=true'
    Invoke-DotNet build $project.FullName -c Release --no-restore
    $assets = Get-Content (Join-Path $project.DirectoryName 'obj/project.assets.json') -Raw | ConvertFrom-Json -AsHashtable
    if (@($assets.project.restore.sources.Keys).Count -ne 1 -or -not $assets.project.restore.sources.ContainsKey('https://api.nuget.org/v3/index.json')) {
        throw "$name restored from an unexpected package feed."
    }
    foreach ($library in $assets.libraries.GetEnumerator()) {
        if ($library.Value.type -ne 'package') { throw "$name has a non-package dependency: $($library.Key)" }
        $metadata = Get-Content (Join-Path $packages ($library.Value.path + '/.nupkg.metadata')) -Raw | ConvertFrom-Json
        if ($metadata.source -ne 'https://api.nuget.org/v3/index.json') { throw "Unexpected package origin: $($library.Key)" }
        $provenance += [pscustomobject]@{ sample = $name; package = $library.Key; source = $metadata.source }
    }

    $dll = Join-Path $project.DirectoryName "bin/Release/net10.0/$name.dll"
    if ($xml.Project.Sdk -ne 'Microsoft.NET.Sdk.Web') {
        $output = & $dotnet $dll
        if ($LASTEXITCODE -ne 0) { throw "$name failed." }
        $output | Set-Content (Join-Path $logs "$name.log")
        if ($name -like 'Bulk*') {
            $result = Join-Path $project.DirectoryName 'bin/Release/net10.0/Output/import-results.csv'
            if (-not (Test-Path $result) -or @(Import-Csv $result).Count -eq 0) { throw "$name produced no import results." }
        } elseif ($name -eq 'CountryLookup.Console' -and ($output -join "`n") -notmatch 'GB / GBR / 826') {
            throw 'Country lookup did not return the expected GB identity.'
        } elseif ($name -eq 'CsvImport.Validation' -and ($output -join "`n") -notmatch 'Row-level errors') {
            throw 'Country import did not report invalid rows.'
        }
    } else {
        $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
        $listener.Start()
        $port = $listener.LocalEndpoint.Port
        $listener.Stop()
        $uri = "http://127.0.0.1:$port"
        $start = @{
            FilePath = $dotnet; ArgumentList = @('"' + $dll + '"', '--urls', $uri)
            WorkingDirectory = $project.DirectoryName; PassThru = $true
            RedirectStandardOutput = Join-Path $logs "$name.stdout.log"
            RedirectStandardError = Join-Path $logs "$name.stderr.log"
        }
        if ($IsWindows) { $start.WindowStyle = 'Hidden' }
        $process = Start-Process @start
        try {
            $ready = $false
            for ($attempt = 0; $attempt -lt 60; $attempt++) {
                if ($process.HasExited) { throw "$name exited before becoming ready; see $logs." }
                try { $null = Invoke-WebRequest $uri -TimeoutSec 2; $ready = $true; break } catch { Start-Sleep -Milliseconds 250 }
            }
            if (-not $ready) { throw "$name did not become ready; see $logs." }
            switch ($name) {
                'DynamicAddressFormDemo' {
                    $valid = Invoke-WebRequest "$uri/?CountryCode=ES&SampleId=es-valid"
                    $invalid = Invoke-WebRequest "$uri/?CountryCode=ES&SampleId=es-invalid-postal"
                    if ($valid.Content -notmatch 'data-portfolio-state="valid"' -or $invalid.Content -notmatch 'data-portfolio-state="invalid"') {
                        throw 'The address form did not distinguish valid and invalid samples.'
                    }
                    foreach ($asset in @('/css/site.css', '/lib/bootstrap/dist/css/bootstrap.min.css', '/lib/jquery/dist/jquery.min.js')) {
                        $null = Invoke-WebRequest "$uri$asset"
                    }
                }
                'CheckoutAddressApi' {
                    $address = @{ line1='10 Downing Street'; city='London'; postalCode='SW1A 2AA'; countryCode='GB' }
                    $valid = Invoke-RestMethod "$uri/checkout/validate-address" -Method Post -ContentType 'application/json' -Body ($address | ConvertTo-Json)
                    $address.postalCode = 'NOT-A-POSTCODE'
                    $invalid = Invoke-RestMethod "$uri/checkout/validate-address" -Method Post -ContentType 'application/json' -Body ($address | ConvertTo-Json)
                    if ($valid.status -ne 'Valid' -or $invalid.status -ne 'Invalid' -or @($invalid.issues).Count -eq 0) { throw 'Address API validation failed.' }
                }
                'CheckoutPricingApi' {
                    $request = @{ currency='GBP'; taxRate=0.2; rounding='AwayFromZero'; items=@(@{ sku='BOOK'; unitPrice=12.99; quantity=2 }, @{ sku='SHIP'; unitPrice=3.49; quantity=1 }) }
                    $quote = Invoke-RestMethod "$uri/quotes" -Method Post -ContentType 'application/json' -Body ($request | ConvertTo-Json -Depth 5)
                    $invalid = Invoke-WebRequest "$uri/currencies/bad-code" -SkipHttpErrorCheck
                    if ([decimal]$quote.total.amount -ne [decimal]35.36 -or $invalid.StatusCode -ne 400) { throw 'Pricing API quote or invalid-input response failed.' }
                }
                default { throw "Add an HTTP smoke check for $name." }
            }
        } finally {
            if (-not $process.HasExited) { Stop-Process -Id $process.Id }
            $process.Dispose()
        }
    }
    Write-Host "Passed: $name"
}
$provenance | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $runRoot 'package-provenance.json')
Write-Host "Public NuGet examples passed. Evidence: $runRoot"
