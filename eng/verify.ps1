param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$out = Join-Path $root "artifacts\verification\$stamp"
New-Item -ItemType Directory -Force -Path $out | Out-Null
$env:HTTP_PROXY = $null; $env:HTTPS_PROXY = $null; $env:ALL_PROXY = $null; $env:GIT_HTTP_PROXY = $null; $env:GIT_HTTPS_PROXY = $null
$common = @('-m:1','-nr:false','-tl:off','-p:UseSharedCompilation=false','-p:NuGetAudit=false','-p:RestoreIgnoreFailedSources=true')
function Invoke-Logged([string]$name, [string[]]$arguments) { & dotnet $arguments *> (Join-Path $out "$name.log"); $code = $LASTEXITCODE; Set-Content (Join-Path $out "$name.exitcode.txt") $code; return $code }
$restore = Invoke-Logged restore (@('msbuild', "$root\PDF2HWP.sln",'-t:Restore','-v:diag',"-bl:$out\restore.binlog") + $common)
$build = if ($restore -eq 0) { Invoke-Logged build (@('msbuild', "$root\PDF2HWP.sln",'-t:Build',"-p:Configuration=$Configuration",'-v:diag',"-bl:$out\build.binlog") + $common) } else { -1 }
$dll = Join-Path $root "tests\Pdf2Hwp.Tests\bin\$Configuration\net8.0\Pdf2Hwp.Tests.dll"
$test = if ($build -eq 0 -and (Test-Path $dll)) { Invoke-Logged test @('vstest',$dll,'--logger:trx;LogFileName=test.trx',"--ResultsDirectory:$out") } else { -1 }
$summary = [ordered]@{ timestamp=(Get-Date).ToString('o'); sdk=(dotnet --version); restoreExitCode=$restore; buildExitCode=$build; testExitCode=$test; result=if($restore -eq 0 -and $build -eq 0 -and $test -eq 0){'PASS'}else{'FAIL'} }
$summary | ConvertTo-Json | Set-Content (Join-Path $out 'verification-summary.json')
if ($summary.result -eq 'PASS') { exit 0 }; exit 1
