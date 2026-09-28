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
$noticeSource = Join-Path $root 'THIRD_PARTY_NOTICES.md'
$licensesSource = Join-Path $root 'licenses\third-party'
if (-not (Test-Path $noticeSource -PathType Leaf)) { throw 'THIRD_PARTY_NOTICES.md is required for a redistributable package.' }
if (-not (Test-Path $licensesSource -PathType Container)) { throw 'The original third-party license bundle is missing.' }
Copy-Item -LiteralPath $noticeSource -Destination (Join-Path $out 'THIRD_PARTY_NOTICES.md') -Force
$licenseOutput = Join-Path $out 'licenses\third-party'
New-Item -ItemType Directory -Force -Path $licenseOutput | Out-Null
Copy-Item -Path (Join-Path $licensesSource '*') -Destination $licenseOutput -Recurse -Force
$requiredNotices = @(
    'licenses\third-party\PDFium\152.0.7961\LICENSE',
    'licenses\third-party\PDFium\152.0.7961\licenses\abseil.txt',
    'licenses\third-party\PDFium\152.0.7961\licenses\pdfium.txt',
    'licenses\third-party\SkiaSharp\4.150.1\THIRD-PARTY-NOTICES.txt',
    'licenses\third-party\Microsoft.NETCore.App.Runtime.win-x64\8.0.27\THIRD-PARTY-NOTICES.TXT'
)
foreach ($relativePath in $requiredNotices) {
    if (-not (Test-Path (Join-Path $out $relativePath) -PathType Leaf)) {
        throw "Required license/notice file was not packaged: $relativePath"
    }
}
Get-Item $expected,(Join-Path $out 'THIRD_PARTY_NOTICES.md') | Select-Object FullName,Length
