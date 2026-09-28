param(
    [string]$PublishDirectory = 'artifacts\win-x64-singlefile'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$audit = Join-Path $root 'artifacts\license-audit'
$publish = if ([System.IO.Path]::IsPathRooted($PublishDirectory)) { $PublishDirectory } else { Join-Path $root $PublishDirectory }
$expectedDllHash = 'D3D9F4B7C9DABE3363F30779C5C3C715C47332749FA590E4B4A2B8B6780CB1C4'
$expectedArchiveHash = '88276459349B291C41F10422DAD0210F007C04D919C8FA56472B6B7C6406ADF4'

function Assert-Equal([string]$actual, [string]$expected, [string]$label) {
    if ($actual -ine $expected) { throw "$label mismatch. Expected '$expected', got '$actual'." }
}
function Get-ZipEntryHash([string]$archivePath, [string]$entryName) {
    $zip = [System.IO.Compression.ZipFile]::OpenRead($archivePath)
    try {
        $entry = $zip.GetEntry($entryName)
        if ($null -eq $entry) { throw "Missing archive entry '$entryName' in '$archivePath'." }
        $stream = $entry.Open()
        $sha = [System.Security.Cryptography.SHA256]::Create()
        try { [Convert]::ToHexString($sha.ComputeHash($stream)) } finally { $stream.Dispose(); $sha.Dispose() }
    } finally { $zip.Dispose() }
}

New-Item -ItemType Directory -Force -Path $audit | Out-Null
$downloads = @(
    @{ Name='pdfium-win-x64.tgz'; Uri='https://github.com/bblanchon/pdfium-binaries/releases/download/chromium%2F7961/pdfium-win-x64.tgz' },
    @{ Name='pdfium-attestation.json'; Uri='https://github.com/bblanchon/pdfium-binaries/releases/download/chromium%2F7961/pdfium-attestation.json' },
    @{ Name='bblanchon.PDFium.Win32.152.0.7961.nupkg'; Uri='https://api.nuget.org/v3-flatcontainer/bblanchon.pdfium.win32/152.0.7961/bblanchon.pdfium.win32.152.0.7961.nupkg' },
    @{ Name='pdfpig.0.1.15.nupkg'; Uri='https://api.nuget.org/v3-flatcontainer/pdfpig/0.1.15/pdfpig.0.1.15.nupkg' },
    @{ Name='pdftoimage.5.4.0.nupkg'; Uri='https://api.nuget.org/v3-flatcontainer/pdftoimage/5.4.0/pdftoimage.5.4.0.nupkg' },
    @{ Name='skiasharp.4.150.1.nupkg'; Uri='https://api.nuget.org/v3-flatcontainer/skiasharp/4.150.1/skiasharp.4.150.1.nupkg' },
    @{ Name='skiasharp.nativeassets.win32.4.150.1.nupkg'; Uri='https://api.nuget.org/v3-flatcontainer/skiasharp.nativeassets.win32/4.150.1/skiasharp.nativeassets.win32.4.150.1.nupkg' },
    @{ Name='microsoft.netcore.app.runtime.win-x64.8.0.27.nupkg'; Uri='https://api.nuget.org/v3-flatcontainer/microsoft.netcore.app.runtime.win-x64/8.0.27/microsoft.netcore.app.runtime.win-x64.8.0.27.nupkg' },
    @{ Name='microsoft.windowsdesktop.app.runtime.win-x64.8.0.27.nupkg'; Uri='https://api.nuget.org/v3-flatcontainer/microsoft.windowsdesktop.app.runtime.win-x64/8.0.27/microsoft.windowsdesktop.app.runtime.win-x64.8.0.27.nupkg' }
)
foreach ($download in $downloads) {
    $path = Join-Path $audit $download.Name
    if (-not (Test-Path $path -PathType Leaf)) { Invoke-WebRequest -Uri $download.Uri -OutFile $path }
}
$releaseDirectory = Join-Path $audit 'release'
if (-not (Test-Path (Join-Path $releaseDirectory 'bin\pdfium.dll') -PathType Leaf)) {
    New-Item -ItemType Directory -Force -Path $releaseDirectory | Out-Null
    & tar.exe -xzf (Join-Path $audit 'pdfium-win-x64.tgz') -C $releaseDirectory
    if ($LASTEXITCODE -ne 0) { throw "Could not extract the official PDFium release archive (exit code $LASTEXITCODE)." }
}

$infraProject = Join-Path $root 'src\Pdf2Hwp.Infrastructure\Pdf2Hwp.Infrastructure.csproj'
[xml]$projectXml = Get-Content $infraProject -Raw
$projectPackages = @{}
foreach ($reference in $projectXml.SelectNodes('/Project/ItemGroup/PackageReference')) {
    $packageId = $reference.GetAttribute('Include')
    $packageVersion = $reference.GetAttribute('Version')
    if ($packageId) { $projectPackages[$packageId] = $packageVersion }
}
Assert-Equal $projectPackages['PdfPig'] '0.1.15' 'Direct PdfPig package version'
Assert-Equal $projectPackages['PDFtoImage'] '5.4.0' 'Direct PDFtoImage package version'

$assetsPath = Join-Path $root 'src\Pdf2Hwp.Infrastructure\obj\project.assets.json'
$assets = Get-Content $assetsPath -Raw | ConvertFrom-Json
foreach ($package in @('PDFtoImage/5.4.0','PdfPig/0.1.15','bblanchon.PDFium.Win32/152.0.7961','SkiaSharp/4.150.1','SkiaSharp.NativeAssets.Win32/4.150.1')) {
    if (-not $assets.libraries.PSObject.Properties.Name.Contains($package)) { throw "Resolved package missing from project.assets.json: $package" }
}
$windowsAssets = $assets.targets.'net8.0/win-x64'
if ($null -eq $windowsAssets) { throw 'The net8.0/win-x64 dependency target is missing.' }
if ($null -eq $windowsAssets.'bblanchon.PDFium.Win32/152.0.7961'.native.'runtimes/win-x64/native/pdfium.dll') {
    throw 'The expected win-x64 PDFium native runtime asset is missing.'
}
$appDepsPath = Join-Path $root 'src\Pdf2Hwp.App\bin\Release\net8.0-windows\win-x64\PDF2HWP.deps.json'
$appDeps = Get-Content $appDepsPath -Raw | ConvertFrom-Json
$publishedPackages = @($appDeps.libraries.PSObject.Properties | Where-Object { $_.Value.type -eq 'package' } | ForEach-Object Name)
foreach ($package in @('PdfPig/0.1.15','PDFtoImage/5.4.0','bblanchon.PDFium.Win32/152.0.7961','SkiaSharp/4.150.1','SkiaSharp.NativeAssets.Win32/4.150.1')) {
    if ($package -notin $publishedPackages) { throw "Resolved publish dependency missing from PDF2HWP.deps.json: $package" }
}
$appTargetName = $appDeps.runtimeTarget.name
$appTarget = $appDeps.targets.PSObject.Properties[$appTargetName].Value
$appEntry = $appTarget.PSObject.Properties | Where-Object Name -like 'PDF2HWP/*' | Select-Object -First 1
if ($null -eq $appEntry -or $appEntry.Value.dependencies.'runtimepack.Microsoft.NETCore.App.Runtime.win-x64' -ne '8.0.27' -or $appEntry.Value.dependencies.'runtimepack.Microsoft.WindowsDesktop.App.Runtime.win-x64' -ne '8.0.27') {
    throw 'The self-contained .NET Core/Desktop runtime pack versions in PDF2HWP.deps.json do not match the notice bundle.'
}

$archive = Join-Path $audit 'pdfium-win-x64.tgz'
$attestationPath = Join-Path $audit 'pdfium-attestation.json'
$nupkg = Join-Path $audit 'bblanchon.PDFium.Win32.152.0.7961.nupkg'
$releaseDll = Join-Path $releaseDirectory 'bin\pdfium.dll'
$projectDll = Join-Path $root 'src\Pdf2Hwp.App\bin\Release\net8.0-windows\win-x64\pdfium.dll'
foreach ($required in @($archive,$attestationPath,$nupkg,$releaseDll,$projectDll)) {
    if (-not (Test-Path $required -PathType Leaf)) { throw "Required local audit/build file is missing: $required" }
}
Assert-Equal (Get-FileHash $archive -Algorithm SHA256).Hash $expectedArchiveHash 'Official PDFium release archive SHA-256'
$payload = (Get-Content $attestationPath -Raw | ConvertFrom-Json).dsseEnvelope.payload
$statement = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($payload)) | ConvertFrom-Json
$subject = $statement.subject | Where-Object name -eq 'pdfium-win-x64.tgz' | Select-Object -First 1
if ($null -eq $subject) { throw 'The attestation does not name pdfium-win-x64.tgz.' }
Assert-Equal $subject.digest.sha256 $expectedArchiveHash 'Attested PDFium release archive digest'
Assert-Equal (Get-ZipEntryHash $nupkg 'runtimes/win-x64/native/pdfium.dll') $expectedDllHash 'NuGet package PDFium DLL SHA-256'
Assert-Equal (Get-FileHash $releaseDll -Algorithm SHA256).Hash $expectedDllHash 'Official release archive PDFium DLL SHA-256'
Assert-Equal (Get-FileHash $projectDll -Algorithm SHA256).Hash $expectedDllHash 'Project Release PDFium DLL SHA-256'
Assert-Equal (Get-Content (Join-Path $audit 'release\VERSION') -Raw).Trim() "MAJOR=152`nMINOR=0`nBUILD=7961`nPATCH=0" 'Official archive PDFium version'

$pdfiumLicenseSource = Join-Path $audit 'release\licenses'
$pdfiumLicenseTarget = Join-Path $root 'licenses\third-party\PDFium\152.0.7961\licenses'
$archiveLicenseNames = @(Get-ChildItem $pdfiumLicenseSource -File | Sort-Object Name | Select-Object -ExpandProperty Name)
if ($archiveLicenseNames.Count -ne 15) { throw "Expected 15 official PDFium component license files; found $($archiveLicenseNames.Count)." }
foreach ($name in $archiveLicenseNames) {
    $source = Join-Path $pdfiumLicenseSource $name
    $target = Join-Path $pdfiumLicenseTarget $name
    if (-not (Test-Path $target -PathType Leaf)) { throw "PDFium original notice was not preserved: $name" }
    Assert-Equal (Get-FileHash $source -Algorithm SHA256).Hash (Get-FileHash $target -Algorithm SHA256).Hash "Preserved PDFium original '$name'"
}

$nupkgPaths = @(
    (Join-Path $audit 'bblanchon.PDFium.Win32.152.0.7961.nupkg'),
    (Join-Path $audit 'pdfpig.0.1.15.nupkg'),
    (Join-Path $audit 'pdftoimage.5.4.0.nupkg'),
    (Join-Path $audit 'skiasharp.4.150.1.nupkg'),
    (Join-Path $audit 'skiasharp.nativeassets.win32.4.150.1.nupkg'),
    (Join-Path $audit 'microsoft.netcore.app.runtime.win-x64.8.0.27.nupkg'),
    (Join-Path $audit 'microsoft.windowsdesktop.app.runtime.win-x64.8.0.27.nupkg')
)
foreach ($packagePath in $nupkgPaths) { if (-not (Test-Path $packagePath -PathType Leaf)) { throw "Package archive is missing for signature verification: $packagePath" } }
$verifyOutput = & dotnet nuget verify --all $nupkgPaths 2>&1
$verifyExitCode = $LASTEXITCODE
$verifyLog = Join-Path $audit 'nuget-package-signature-verification.log'
$verifyOutput | Set-Content $verifyLog
if ($verifyExitCode -ne 0) { throw "NuGet package signature verification failed with exit code $verifyExitCode. See artifacts/license-audit/nuget-package-signature-verification.log." }

$noticeSource = Join-Path $root 'THIRD_PARTY_NOTICES.md'
$licensesSource = Join-Path $root 'licenses\third-party'
$noticeOutput = Join-Path $publish 'THIRD_PARTY_NOTICES.md'
$licensesOutput = Join-Path $publish 'licenses\third-party'
if (-not (Test-Path $noticeOutput -PathType Leaf)) { throw 'Published package does not contain THIRD_PARTY_NOTICES.md.' }
Assert-Equal (Get-FileHash $noticeSource -Algorithm SHA256).Hash (Get-FileHash $noticeOutput -Algorithm SHA256).Hash 'Published THIRD_PARTY_NOTICES.md'
foreach ($sourceFile in Get-ChildItem $licensesSource -Recurse -File) {
    $relative = $sourceFile.FullName.Substring($licensesSource.Length).TrimStart('\')
    $publishedFile = Join-Path $licensesOutput $relative
    if (-not (Test-Path $publishedFile -PathType Leaf)) { throw "Published package is missing original license/notice: $relative" }
    Assert-Equal (Get-FileHash $sourceFile.FullName -Algorithm SHA256).Hash (Get-FileHash $publishedFile -Algorithm SHA256).Hash "Published original license/notice '$relative'"
}

$gh = Get-Command gh -ErrorAction SilentlyContinue
if ($null -eq $gh) { throw 'GitHub CLI is required to cryptographically verify the downloaded release attestation.' }
$env:GH_CONFIG_DIR = Join-Path $audit 'gh-config'
$attestationLog = Join-Path $audit 'pdfium-attestation-verification.log'
$ghOutput = & gh attestation verify $archive --bundle $attestationPath --repo bblanchon/pdfium-binaries --signer-workflow bblanchon/pdfium-binaries/.github/workflows/build-all.yml --source-ref refs/heads/master 2>&1
$ghExitCode = $LASTEXITCODE
$ghOutput | Set-Content $attestationLog
if ($ghExitCode -ne 0) { throw "PDFium release attestation verification failed with exit code $ghExitCode. See artifacts/license-audit/pdfium-attestation-verification.log." }

Write-Output 'LICENSE_ASSETS_VERIFIED'
Write-Output "PDFium archive SHA-256: $expectedArchiveHash"
Write-Output "PDFium DLL SHA-256 (NuGet/release/project): $expectedDllHash"
Write-Output "PDFium component license files: $($archiveLicenseNames.Count)"
Write-Output "NuGet packages with verified signatures: $($nupkgPaths.Count)"
Write-Output "Published notice bundle: $noticeOutput + $licensesOutput"
