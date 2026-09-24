param([string]$OutputDirectory = 'artifacts\win-x64-singlefile')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$out = if ([System.IO.Path]::IsPathRooted($OutputDirectory)) { $OutputDirectory } else { Join-Path $root $OutputDirectory }
$project = Join-Path $root 'src\Pdf2Hwp.App\Pdf2Hwp.App.csproj'
dotnet publish $project -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false -p:DebugSymbols=false -p:DebugType=None -p:CopyOutputSymbolsToPublishDirectory=false -p:CopyDebugSymbolFilesFromPackages=false -p:NuGetAudit=false -p:RestoreIgnoreFailedSources=true -m:1 -nr:false -p:UseSharedCompilation=false -o $out
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE." }
$expected = Join-Path $out 'PDF2HWP.exe'
if (-not (Test-Path $expected)) {
    $legacy = Join-Path $out 'Pdf2Hwp.App.exe'
    if (Test-Path $legacy) { Copy-Item $legacy $expected -Force }
}
if (-not (Test-Path $expected)) { throw 'Publish output executable PDF2HWP.exe was not created.' }
Get-Item $expected | Select-Object FullName,Length
