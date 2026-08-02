param(
    [string]$Version = ""
)

$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$publishDirectory = Join-Path $repositoryRoot "artifacts\win-x64"
$packageDirectory = Join-Path $repositoryRoot "artifacts\LumaKnob-win-x64"
$archivePath = Join-Path $repositoryRoot "artifacts\LumaKnob-win-x64.zip"

if (Test-Path $publishDirectory) {
    Remove-Item -Recurse -Force $publishDirectory
}

$publishArguments = @(
    "publish",
    (Join-Path $repositoryRoot "src\LumaKnob.Host.Windows\LumaKnob.Host.Windows.csproj"),
    "--configuration", "Release",
    "--runtime", "win-x64",
    "--self-contained", "true",
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-p:EnableCompressionInSingleFile=true",
    "-p:DebugSymbols=false",
    "-p:DebugType=None",
    "--output", $publishDirectory
)

if (-not [string]::IsNullOrWhiteSpace($Version)) {
    $publishArguments += "-p:Version=$Version"
}

& dotnet @publishArguments
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

Get-ChildItem -Path $publishDirectory -Filter "*.pdb" -File -Recurse |
    Remove-Item -Force

$executablePath = Join-Path $publishDirectory "LumaKnob.exe"
$maximumExecutableBytes = 60000000
$executableBytes = (Get-Item $executablePath).Length
if ($executableBytes -gt $maximumExecutableBytes) {
    throw "LumaKnob.exe is $executableBytes bytes; the release limit is $maximumExecutableBytes bytes."
}

if (Test-Path $archivePath) {
    Remove-Item $archivePath
}

if (Test-Path $packageDirectory) {
    Remove-Item -Recurse -Force $packageDirectory
}

New-Item -ItemType Directory -Path $packageDirectory | Out-Null
Copy-Item (Join-Path $publishDirectory "LumaKnob.exe") $packageDirectory
Copy-Item (Join-Path $repositoryRoot "README.md") $packageDirectory
Copy-Item (Join-Path $repositoryRoot "PROJECT.md") $packageDirectory
Copy-Item (Join-Path $repositoryRoot "LICENSE") $packageDirectory
Copy-Item (Join-Path $repositoryRoot "THIRD_PARTY_NOTICES.md") $packageDirectory
Copy-Item (Join-Path $repositoryRoot "presets") $packageDirectory -Recurse

$globalPackagesListing = @(& dotnet nuget locals global-packages --list)
if ($LASTEXITCODE -ne 0) {
    throw "Could not locate the NuGet global-packages directory."
}

$globalPackagesLine = $globalPackagesListing |
    Where-Object { $_ -match "^global-packages\s*:" } |
    Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($globalPackagesLine)) {
    throw "dotnet did not report a NuGet global-packages directory."
}

$separatorIndex = $globalPackagesLine.IndexOf(":")
$globalPackagesDirectory = $globalPackagesLine.Substring($separatorIndex + 1).Trim()
$nativeNoticePath = Join-Path $globalPackagesDirectory `
    "skiasharp.nativeassets.win32\3.119.4\THIRD-PARTY-NOTICES.txt"
$skiaSharpLicensePath = Join-Path $globalPackagesDirectory `
    "skiasharp.nativeassets.win32\3.119.4\LICENSE.txt"
$angleLicensePath = Join-Path $globalPackagesDirectory `
    "avalonia.angle.windows.natives\2.1.27548.20260419\LICENSE"

foreach ($requiredNotice in @($nativeNoticePath, $skiaSharpLicensePath, $angleLicensePath)) {
    if (-not (Test-Path $requiredNotice)) {
        throw "Required third-party notice was not restored: $requiredNotice"
    }
}

Copy-Item $nativeNoticePath `
    (Join-Path $packageDirectory "NATIVE_THIRD_PARTY_NOTICES.txt")
Copy-Item $skiaSharpLicensePath `
    (Join-Path $packageDirectory "SKIASHARP_LICENSE.txt")
Copy-Item $angleLicensePath `
    (Join-Path $packageDirectory "ANGLE_LICENSE.txt")

Compress-Archive -Path (Join-Path $packageDirectory "*") -DestinationPath $archivePath
$archiveHash = (Get-FileHash $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
"$archiveHash  LumaKnob-win-x64.zip" |
    Set-Content (Join-Path $repositoryRoot "artifacts\SHA256SUMS.txt") -Encoding ascii
Write-Host "Created matching folder $packageDirectory"
Write-Host "Created archive $archivePath"
Write-Host "Created SHA256SUMS.txt"
Write-Host "LumaKnob.exe size: $executableBytes bytes"
