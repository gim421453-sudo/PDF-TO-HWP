# PC 포맷 후 PDF2HWP 복구·작업 재개

작성일: 2026-10-19 (Asia/Seoul). 이 문서는 소스·진단 자료 복구 절차다. 제품 출시 승인이나 Hancom 호환성 통과 선언이 아니다.

## 1. 복구 기준과 우선 읽기

- 저장소: `https://github.com/gim421453-sudo/PDF-TO-HWP.git`, 작업 브랜치 `main`.
- 이번 보존 커밋의 부모 소스 기준: `75c99df72c421fc4caa8febe278ef5c0ae3d37f1`. 이번 작업은 문서와 기존 진단 자산 보존이며 production 코드는 변경하지 않았다. 보존 커밋의 정확한 SHA는 아래 명령으로 확인한다. 문서 안에 자기 자신의 커밋 SHA를 미리 기록하지 않는다.
- 재개 순서: `AGENTS.md` → `docs/handoff/CURRENT_HANDOFF.md` 맨 위 → 이 문서 → `artifacts/compatibility-tests/diagnostic/README.md`.
- OWNER는 Codex. `CLAUDE.md`는 `@AGENTS.md`를 import한다. `.claude/settings.json`과 저장소 `.agents/skills/`는 추적되므로 clone으로 복구된다.
- 과거 README/CLAUDE.md의 106 tests, SDK 10.0.300, push 전 상태와 핸드오프 하단의 과거 origin SHA는 현재 기준이 아니다. Git 실제 상태와 최신 핸드오프를 우선한다.

## 2. 설치할 환경

| 항목 | 포맷 전 확인값 / 용도 |
|---|---|
| OS | Windows x64. WPF GUI와 Windows PDFium 동작에 필요 |
| Git | Git for Windows 및 GitHub 인증. 인증 토큰은 저장소/이 문서에 넣지 않음 |
| PowerShell | 현재 실행 셸 7.6.5. 복구 후 `pwsh` 사용 권장 |
| .NET SDK | 기본 선택 10.0.401, 8.0.425도 설치됨. `global.json`은 없음 |
| .NET 대상 | Core/Infrastructure/Tests `net8.0`, WPF App `net8.0-windows`, x64 |
| Desktop Runtime | 현재 `Microsoft.WindowsDesktop.App 8.0.31` 등 설치. 개발 실행 시 호환되는 .NET 8 Desktop Runtime 필요 |
| Hancom | Hancom Office 2024. 수동 시각/성능 검증에만 필요. 정상 설치 및 사용자 라이선스 재인증 필요 |
| GUI 자동화 | Codex의 `computer-use` 스킬과 Windows 앱을 실제 노출하는 도구. 브라우저 전용 제어만 제공되면 Windows 자동화는 차단 상태로 기록 |
| 네트워크 | NuGet restore, GitHub 동기화 및 선택적 라이선스 출처 감사에 필요 |

SDK와 런타임 숫자는 포맷 전 재현 기준이다. 재설치 시 실제 버전을 기록하고, 달라졌다면 전체 검증 전 통과를 가정하지 않는다. Windows 설정/보안 정책/한컴 설정을 우회하거나 임의 변경하지 않는다.

## 3. clone 및 동기화 확인

PowerShell 7에서 실행한다. 기존 파일이 있는 폴더를 덮어쓰지 않는다.

```powershell
New-Item -ItemType Directory -Force C:\dev | Out-Null
git clone https://github.com/gim421453-sudo/PDF-TO-HWP.git C:\dev\PDF-TO-HWP
Set-Location C:\dev\PDF-TO-HWP
git switch main
git fetch origin
git status --short
git rev-parse HEAD
git rev-parse origin/main
git rev-list --left-right --count HEAD...origin/main
git log -5 --oneline
```

정상 기준: HEAD와 origin/main이 같고 ahead/behind가 `0 0`, 작업 트리 clean. 보존 커밋 메시지는 `docs: preserve reformat recovery handoff and Hancom diagnostics`다. 이후 main에 정상 후속 커밋이 생겼다면 보존 커밋이 main 이력에 포함되는지도 확인한다.

모든 로컬 작업 브랜치 9개는 2026-10-19 검사에서 main의 조상이었다. 연결 worktree 8개는 모두 clean이었다. 따라서 추가 병합은 필요하지 않았다. Temp 아래 옛 worktree 경로와 `.git/worktrees`를 복사할 필요는 없다. 포맷 후 main clone에서 계속한다. 옛 브랜치를 재병합하지 않는다.

## 4. 보호 파일과 진단 파일 무결성

| 보호 파일 | SHA-256 |
|---|---|
| `artifacts/compatibility-tests/23-visual-fidelity-a4-portrait-3pages-e2e.hwpx` | `CEF60905336D3AF0B1E2D308D54D16764B4CBF10DA90D1FAF06E26C1CD6D7DED` |
| `artifacts/reference-hancom/hancom-minimal-reference.hwpx` | `CE7D9151F38FAA90C32C7D7A11C866EE81FA478FC2963A236194A8DDCCF69F4D` |
| `artifacts/hwpx-diff/20260909-020850/golden-hancom-minimal-reference.hwpx` | 위 Golden과 동일 |

진단 원본 17개(10 HWPX, 6 PNG, 1 manual-test HWPX)의 경로·크기·SHA는 `docs/handoff/DIAGNOSTIC_ASSETS.csv`에 보존했다. 2026-10-19 사용자가 진단 파일도 Git에 올리도록 명시해 기존 미추적 유지 정책을 이번 보존 작업에서 변경했다. 샘플 바이트를 수정하거나 새 fixture로 승인한 것은 아니다.

```powershell
$ErrorActionPreference = 'Stop'
Set-Location C:\dev\PDF-TO-HWP
$assets = Import-Csv docs/handoff/DIAGNOSTIC_ASSETS.csv
if ($assets.Count -ne 17) { throw '진단 자산 수 불일치' }
foreach ($asset in $assets) {
    $file = Get-Item -LiteralPath $asset.Path
    $hash = (Get-FileHash -LiteralPath $asset.Path -Algorithm SHA256).Hash
    if ($file.Length -ne [long]$asset.Bytes -or $hash -ne $asset.SHA256) {
        throw "진단 자산 불일치: $($asset.Path)"
    }
}
Get-FileHash -Algorithm SHA256 -LiteralPath `
  artifacts/compatibility-tests/23-visual-fidelity-a4-portrait-3pages-e2e.hwpx, `
  artifacts/reference-hancom/hancom-minimal-reference.hwpx
git diff --check
```

보호 SHA 불일치 시 샘플 생성/수동 저장/변환을 중단하고 실제 경로와 Git 변경부터 조사한다. SHA를 새 값에 맞춰 갱신하거나 보호 파일을 재생성하지 않는다.

## 5. 빌드·실행·배포 검증

이번 포맷 준비에서는 기능 변경이 없어 canonical 빌드/테스트/publish를 재실행하지 않았다. 마지막 실제 canonical 결과는 2026-10-12, SDK 10.0.401, Restore/Release Build/Test PASS, 122/122, failed 0, skipped 0이다. 로컬 `artifacts/verification/20261012-140749/test.trx`의 counters도 이번에 다시 읽어 확인했다. 그 로그 디렉터리는 ignored이며 clone으로 복구되지 않는다.

복구 후 새 환경을 검증한다:

```powershell
dotnet --info
pwsh --version
$env:VSTEST_CONNECTION_TIMEOUT = '300'
pwsh -NoProfile -File eng/verify.ps1
if ($LASTEXITCODE -ne 0) { throw 'canonical 검증 실패' }
git diff --check
dotnet run --project src/Pdf2Hwp.App/Pdf2Hwp.App.csproj
```

결과는 새 `artifacts/verification/<timestamp>/`의 summary와 TRX에서 확인한다. 기준은 Restore/Build/Test PASS, 최소 122 tests, failed 0, skipped 0. 과거 샌드박스 testhost 연결 시간 초과 이력이 있으므로 실행 환경 오류와 실제 테스트 실패를 구별한다. 테스트를 약화하거나 skip해서 통과시키지 않는다.

배포물을 다시 만들 때:

```powershell
pwsh -NoProfile -File eng/publish-win-x64.ps1
if ($LASTEXITCODE -ne 0) { throw 'publish 실패' }
```

산출물은 `artifacts/win-x64-singlefile/PDF2HWP.exe`. EXE와 함께 `THIRD_PARTY_NOTICES.md`, `licenses/third-party/`가 필요하다. 빌드 산출물과 NuGet 캐시는 Git에 없으므로 다시 생성한다. 새 EXE는 창 표시, 제목 PDF2HWP, 즉시 crash 없음, 정상 종료를 실제로 확인해야 한다.

라이선스: 기존 `eng/verify-license-assets.ps1`은 2026-10-12 `LICENSE_ASSETS_VERIFIED`였다. CRLF/CR→LF만 허용하는 text canonical 비교이며 DLL/archive/nupkg/attestation은 byte-exact다. 보호 라이선스 원문은 수정하지 않는다. AGENTS.md의 '이 PC는 PowerShell 5.1이므로 실행 금지' 문구는 현재 셸 7.6.5 확인값과 다르다. 이번에는 실행하지 않았다. 재감사가 필요한 배포 작업에서만 실제 PowerShell 7 환경과 스크립트 선행 조건을 확인한 후 수행한다.

## 6. 현재 구현·남은 개발

- 구현: HWPX Visual Fidelity, PDF 다중 입력/페이지 편집, thumbnail/cache, Undo/Redo, 비동기 변환/취소/진행률, 설정/최근 파일/이력, single-file publish.
- 통합 완료: batch import 실패 후 계속 처리, 이력 삭제, stale snapshot 방지, UI export/open 오류 처리, PDF hash 변경 감지, 샘플 출력 예약, workspace cleanup 실패 경로 테스트, HWPX-only 문구, Hancom manual gate.
- 미구현/미검증: OCR, Editable, Safe Hybrid, HWP 바이너리 출력, PDF 대 출력물의 자동 시각 비교. UI 비활성 기능을 임의로 켜지 않는다.
- 추가 검토 후보: inspector의 모든 section별 배경 참조 검증 깊이, 회전/크롭 UI 조작 노출, landscape/mixed-orientation 검증. 우선순위는 사용자와 정한다.
- `HANCOM_3PAGE_MANUAL_REQUIRED` 유지. `FULL_COMPATIBILITY`, `PRODUCTION_READY`, 전체 Hancom PASS 선언 금지.

## 7. Hancom 진단 해석과 정확한 다음 작업

2026-10-17 관찰에서는 04/15/21/23E/23H와 23A/23K가 렌더링되고 UI가 응답했으며 긴 지연이 재현되지 않았다. 단 이전 1.3~2.2초 수치는 자동 대기/캡처 관찰 간격을 포함한 참고값이다. 파일 열기부터 최초 paint까지 측정한 성능 수치로 인용하지 않는다. 도구 호출 사이 시간도 포함되지 않아 성능 상한으로 사용할 수 없다.

23A/23K의 'warm reopen'은 파일이 열린 채 같은 경로를 다시 선택해 기존 창을 활성화했을 가능성이 높다. 창 ID와 스크롤 위치가 유지됐으므로 실제 닫기→다시 읽기가 검증됐다고 보지 않는다. 이 증거 한계는 이전 세션의 단정적인 표현보다 우선한다. 21의 130% 잘림/회색 영역도 실제 레이아웃 결함인지 화면 배율 영향인지 미확정이다. CPU/메모리 수치는 없다.

정확한 재개 절차:

1. 보호 SHA와 manifest 확인, canonical 검증으로 새 PC 환경을 먼저 확인한다.
2. Hancom Office 2024 설치/버전과 Computer Use의 Windows 창 제어를 확인한다. 잠금 화면·보안 프롬프트는 우회하지 않는다.
3. 동일 Hancom 프로세스, 동일 화면 배율/창 크기/첫 화면 위치를 유지한다. 각 파일을 정상 닫아 창과 파일 잠금이 해제된 것을 확인한 후 재열어 warm 조건을 만든다. 이미 열린 창 재활성화는 reopen으로 세지 않는다.
4. 배경/기본 경로 비교: 04 → 15 → 21 → 23E → 23H. 페이지 수/해상도 비교가 필요하면 23E→23F→23G→23H→23I→23J. 본문 통제 비교: 23A ↔ 23K. 각 파일 first/warm을 별도 기록한다.
5. 실제 첫 paint와 UI 응답 시점을 기록하고 스크롤, 각 페이지 이동, 배경, 빈 쪽, 손상 경고를 관찰한다. 가능하면 Task Manager로 CPU를 함께 관찰한다. 정확히 재지 못한 시간은 범위와 방법을 적는다.
6. H1 본문/layout, H2 section/background, H3 해상도, H4 section/background XML의 가설을 근거로 비교한다. 현재 판정은 `PERFORMANCE_CAUSE_STILL_UNCLEAR`; 원인을 확정하지 않는다.
7. 보호 원본 #23 수동 검증은 읽기 전용. Ctrl+S/덮어쓰기 금지, 닫을 때 저장 안 함. 완전 FREEZE 시 강제 종료하지 말고 중단·보고한다. 완료 후 보호 SHA 재확인.
8. 진단 저장이 별도로 필요하면 원본 대신 새 이름으로 저장한다. 기존 manual-test는 증거로 사용하지 않는다. 테스트 후 핸드오프를 갱신한다.

## 8. Git에 없는 자료와 포맷 주의

이번 Git 보존 범위는 프로젝트 문서와 진단 자산이다. PC 전체 백업은 아니다.

- `%LOCALAPPDATA%/PDF2HWP/settings.json`, `recent.json`, `history.json`: 앱 개인 상태. 소스에서 경로만 확인했으며 내용 열람·복사·업로드하지 않았다. 필요하면 사용자가 암호화된 개인 백업에 따로 보존한다. `Cache/Preview`는 재생성 가능한 캐시다.
- 사용자의 원본 PDF/변환 결과, 문서 폴더, 서명 인증서, 비밀값, GitHub 인증, Hancom 라이선스, Codex 개인 설정/플러그인·채팅은 이 저장소 push로 보존되지 않는다. 필요 여부와 별도 백업은 미확인이다.
- ignored `.agent-sync/`의 로컬 실행 상태·프롬프트·로그는 이번에 업로드하지 않았다. 최신 continuation은 추적된 핸드오프다. 옛 실행 중 플래그를 새 PC에 복사해 재시작하지 않는다.
- ignored `bin/obj`, license-audit, verification 원본 로그, publish EXE는 재생성 대상이다. 과거 검증 사실/제한은 핸드오프에 보존했다.
- `docs/recovery/`는 보호 사용자 자료다. 이번 작업에서 열람/수정/추가하지 않았다. 별도 사용자 백업 여부는 미확인이다.
- 저장소 스킬은 clone되지만 Codex 전역 Computer Use 스킬/설치본은 재설치가 필요할 수 있다. 비밀값/인증정보를 Git에 올리지 않는다.

교차 기기 준비도는 `READY_WITH_WARNINGS`: Git에 보존한 개발·진단 자료로 작업은 재개할 수 있으나, 위 개인 자료와 라이선스/인증의 포맷 전 별도 보존 여부까지 확인한 것은 아니다.
