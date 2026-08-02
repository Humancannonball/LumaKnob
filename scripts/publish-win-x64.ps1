param([string]$Version = "")

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\LumaKnob.Host.Windows\LumaKnob.Host.Windows.csproj"
$output = Join-Path $root "artifacts"
$exe = Join-Path $output "LumaKnob.exe"
$staging = Join-Path ([System.IO.Path]::GetTempPath()) ("LumaKnob-" + [guid]::NewGuid())

New-Item -ItemType Directory -Force $output | Out-Null

$arguments = @(
    "publish", $project,
    "--configuration", "Release",
    "--runtime", "win-x64",
    "--self-contained", "true",
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-p:EnableCompressionInSingleFile=true",
    "-p:DebugSymbols=false",
    "-p:DebugType=None",
    "--output", $staging
)

if ($Version) {
    $arguments += "-p:Version=$Version"
}

try {
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed."
    }

    Copy-Item (Join-Path $staging "LumaKnob.exe") $exe -Force
}
finally {
    Remove-Item $staging -Recurse -Force -ErrorAction SilentlyContinue
}

$size = (Get-Item $exe).Length
if ($size -gt 60000000) {
    throw "LumaKnob.exe is larger than the 60 MB release limit."
}

$hash = (Get-FileHash $exe -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Host "Created $exe ($size bytes)"
Write-Host "SHA-256: $hash"
