# Current Project Handoff

Last updated: 2026-10-12 (Asia/Seoul)
Status: FOLLOWUP_BRANCHES_PREPARED / READY_WITH_WARNINGS

## 2026-10-12 후속 브랜치 통합 준비 상태

- 기준 `main`/`origin/main`은 `cd09492a0c50d46c8d3e727bad6f8cb050bc8491`이다. 다섯 대상은 모두 이 기준에서 독립적으로 시작했고 stacked 관계가 없다. 소스 변경은 서로 겹치지 않으며 AGENTS와 핸드오프 문서가 공통으로 겹친다. CHANGELOG도 세 브랜치에서 겹친다.
- P2-5는 Windows 파일 길이, 마지막 쓰기 시각, 파일 시스템 변경 시각을 SHA 캐시 지문에 포함한다. 변경 시각을 안전하게 얻지 못하면 캐시 재사용을 하지 않는다. 동일 경로·길이·마지막 쓰기 시각에서 PDF 본문만 바뀌는 회귀 테스트를 추가했다.
- P2-6은 출력 디렉터리 내부의 배타 예약 파일을 원자적으로 확보한 후에만 샘플 파일을 생성한다. 동시 예약은 하나만 성공하고, 기존 비어 있지 않은 디렉터리는 보존한다. 생성기는 실행하지 않았다.
- 임시 작업공간 테스트는 정상 완료, 취소, 렌더 실패, 검증 실패, 게시 실패를 실행해 workspace 및 active 표식 정리, 결과 파일 상태, 다음 작업 실행, 다른 작업 폴더 보존을 확인한다.
- 출력 문구 검토 결과 UI 형식과 패키지 설명은 HWPX Visual Fidelity 지원 상태에 맞고, HWP 바이너리 출력 지원을 주장하지 않는다.
- Hancom 수동 확인은 `HANCOM_3PAGE_MANUAL_REQUIRED`다. 수동 검증 전 `FULL_COMPATIBILITY` 선언 금지. 보호 샘플 #23 SHA-256: `CEF60905336D3AF0B1E2D308D54D16764B4CBF10DA90D1FAF06E26C1CD6D7DED`.
- SDK 10.0.401 `eng/verify.ps1`: P2-5 115/115, P2-6 116/116, Hancom 114/114, 출력 문구 114/114, 임시 작업공간 119/119. 전부 Restore/Release Build/Test PASS, 실패 0, 건너뜀 0. 각 브랜치 `git diff --check` PASS.
- 병합 전 준비 상태다. 예정 순서는 P2-5 → P2-6 → 임시 작업공간 → 출력 문구 → Hancom 메모이며, 각 병합 후와 통합 main 검증이 남았다. 푸시는 하지 않았다. READY_WITH_WARNINGS.

## Project

- 저장소: PDF2HWP (노트북 PDF-TO-HWP), Codex 담당. Windows x64 WPF / .NET 8, `PDF2HWP.sln`.
- 현재 지원 경로는 Visual Fidelity HWPX다. OCR, Editable 재구성, Safe Hybrid, PDF 대 결과물 비교, HWP 바이너리 출력은 미구현이다.

## 이번 수정

목표: P2-6 생성기의 고정 경로 덮어쓰기를 막는 안전 출력 경로 초안을 시작한다.

### 저장소 기준 상태

- 병합 기준은 `main`/`origin/main`의 `7d5210b`였다. `codex/fix-history-delete`, `codex/fix-stale-history-snapshot`, `codex/fix-ui-export-errors`, `codex/fix-batch-pdf-import`를 일반 병합했고, 통합된 코드 기준 커밋은 `9e24dcb`다.
- 네 수정 모두 코드와 회귀 테스트를 보존했다. 같은 테스트 파일의 병합 충돌은 각 테스트를 모두 포함하도록 해결했다. 의존성 및 보호 자료는 변경하지 않았다.
- P2-1: JSON 재로드 객체의 중첩 목록 참조 비교로 기록 삭제가 실패하던 문제를 고쳤다. 저장소가 전체 필드 값을 비교해 선택 기록만 제거하며 결과 HWPX/PDF 출력은 유지한다.
- P2-2: 매 변환 시도 전에 스냅샷을 초기화하고 종료 때 정리해, 새 작업의 사전 검증 실패가 이전 작업 페이지를 실패 기록으로 남기지 않게 했다.
- P2-3: 결과 폴더 열기, 보고서 내보내기, 진단 열기의 동기 예외를 오류 메시지로 변환해 UI 상태에 표시한다. 결과 폴더 누락도 표시한다.
- P2-4: PDF 일괄 추가에서 파일별 예외를 격리해 실패 파일 이후 항목도 처리하고, 취소는 계속 전파하도록 했다.
- 이전 통합 커밋 `cd09492`는 `origin/main`에 푸시됐고 P2-2~P2-4 작업 브랜치도 원격에 게시됐다.
- 현재 `codex/p2-6-safe-sample-output`에서 생성기 기본 출력 폴더를 매 실행마다 고유한 `%TEMP%` 하위 경로로 분리하고, 사용자가 지정한 경로가 비어 있지 않으면 생성 전 중단하도록 초안을 작성했다. PDF/렌더 산출물도 동일 run root 아래로 옮긴다. 보호 샘플이나 생성기는 실행하지 않았다.
- `docs/recovery/`의 7개 파일은 Git 추적 대상이다. 보호 사용자 자료로 취급해 열람·수정하지 않았다.

### 검증 결과

- P2-1 회귀 `Removing_a_selected_history_entry_loaded_from_json_removes_only_that_entry`: 원래 구현에서 실패 재현, 수정 후 PASS.
- P2-2 회귀 `Starting_a_conversion_attempt_clears_the_previous_job_snapshot`: PASS. P2-3 오류 매핑 회귀 `User_operation_guard_returns_a_mapped_error_instead_of_throwing`: PASS. P2-4 파일별 실패 격리 회귀 `Batch_processing_continues_after_an_item_fails`: PASS.
- 각 작업 브랜치에서 SDK `10.0.401`, `VSTEST_CONNECTION_TIMEOUT=300`로 `eng/verify.ps1` 수행: Restore PASS / Release Build PASS / Test PASS, 각각 **111/111 통과, 실패 0, 건너뜀 0**. 산출물: `artifacts/verification/20261010-131019`, `20261010-131620`, `20261010-131741`, `20261010-131840`.
- 병합 충돌은 테스트 메서드 병합 및 문서 충돌 해결로 정리했다. 통합 후 `eng/verify.ps1` 재실행은 NOT RUN. 병합 단계의 `git diff --cached --check`는 PASS.
- P2-6 생성기 실행/build 검증은 NOT RUN. 이번 브랜치의 `git diff --check`도 최종 검토 전이다.
- 이전 P1 CropBox 회귀는 각 전체 테스트에 계속 포함됐다. 비대칭 CropBox의 4방향 회전 픽셀 검사는 PASS.
- 샘플 #23 자동 구조 테스트 `PdfPipelineTests.Sample23_is_inspected_without_modification_and_keeps_manual_gate`: PASS. 검사 전후 SHA-256 일치와 3개 섹션을 확인한다. 현재 SHA-256 `CEF60905336D3AF0B1E2D308D54D16764B4CBF10DA90D1FAF06E26C1CD6D7DED`.
- Hancom Office 2024 수동 열기는 NOT VERIFIED. 실행 파일은 설치돼 있지만 Computer Use helper가 `windows sandbox failed: helper_unknown_error: setup refresh had errors`로 두 번 초기화되지 않아 문서를 열지 못했다.
- `eng/verify-license-assets.ps1`: NOT RUN. 저장소 AGENTS 지침의 명시적 실행 금지를 따랐다. 현재 기본 PowerShell은 5.1.26100이며 PATH에서 관리 런타임 `pwsh 7.6.5`도 발견했으나, 사용 허가를 추론해 실행하지 않았다. 마지막 기록상 라이선스 자산 검증은 2026-09-28 PASS다. 의존성 변경은 감사 기준 이후 없었다.
- 게시 EXE 실행, 실제 WPF UI 스모크, 샘플 생성 도구는 실행하지 않았다. 샘플 생성 도구는 보호 샘플을 덮어쓸 수 있어 실행 금지다.

## 확인된 활성 결함 및 위험

| 우선순위 | 상태 | 진단과 근거 |
|---|---|---|
| P1-1 | 해결 및 회귀 테스트 PASS | PDFium 기본 렌더링이 페이지 CropBox를 적용하므로 별도 `Bounds`에 CropBox 절대좌표를 재전달하지 않도록 했다 (`src/Pdf2Hwp.Infrastructure/PdfiumPageRenderer.cs`). 새 테스트가 오프셋 비대칭 CropBox `[20,10,120,60]`의 모든 픽셀과 회전별 크기를 확인한다 (`tests/Pdf2Hwp.Tests/PdfPipelineTests.cs`). 전체 검증 110/110 PASS. |
| P2-1 | 해결 및 회귀 테스트 PASS | UI 목록 인덱스 대신 저장소가 JSON에서 선택 레코드와 전체 필드 값이 같은 항목을 원자적으로 제거한다 (`src/Pdf2Hwp.Core/ApplicationContracts.cs`, `src/Pdf2Hwp.App/MainWindow.xaml.cs`). 테스트가 기존 실패를 재현하고 수정 후 통과한다. 출력 파일은 그대로 유지한다. |
| P2-2 | 해결 및 회귀 테스트 PASS, GUI 재현 NOT RUN | 변환 시도 시작 전 `ConversionAttemptSnapshot`을 비우고 종료 시 정리한다 (`src/Pdf2Hwp.Core/ApplicationContracts.cs`, `src/Pdf2Hwp.App/MainWindow.xaml.cs`). 이전 작업 페이지가 새 사전 검증 오류의 결과로 기록되지 않는다. |
| P2-3 | 해결 및 회귀 테스트 PASS, GUI 재현 NOT RUN | `UserOperationGuard.CaptureError`와 UI 경계를 통해 결과 폴더 열기/보고서 내보내기/진단 열기 오류를 상태 메시지로 표시한다 (`src/Pdf2Hwp.Core/ApplicationContracts.cs`, `src/Pdf2Hwp.App/MainWindow.xaml.cs`). |
| P2-4 | 해결 및 회귀 테스트 PASS, 혼합 PDF GUI 시나리오 NOT RUN | `BatchProcessor.ContinueOnFailureAsync`가 개별 실패를 모으고 나머지 파일을 계속 처리하며 취소 예외를 보존한다 (`src/Pdf2Hwp.Core/BatchProcessor.cs`, `src/Pdf2Hwp.App/MainWindow.xaml.cs`). |
| P2-5 | 코드 경로 확인, 성능 영향은 입력 의존 | 페이지마다 `JobRenderCache.RenderAsync`가 원본 전체 SHA-256을 다시 계산한다 (`PreviewThumbnails.cs:27-30`). 미리보기 cache miss는 캐시 계층과 내부 renderer에서 각각 해시할 수 있다 (`:69,106`). 대형 다중 페이지 PDF에서 불필요한 전체 파일 I/O가 반복된다. 렌더 PNG도 페이지 리소스 바이트로 메모리에 모인다. |
| P2-6 | 안전 출력 초안, 검증 NOT RUN | 생성기의 기본 결과 경로를 고유 임시 run root로 분리하고 명시적 출력 root가 비어 있지 않으면 거부한다 (`tools/Pdf2Hwp.CompatibilitySampleGenerator/Program.cs`). writer는 여전히 덮어쓰기 API를 사용하므로 이 preflight와 모든 출력 경로를 정적으로 확인해야 하며, 실제 생성기는 실행하지 않았다. |

### P3 및 제품 정합성

- P3-1 라이선스 검사기는 PowerShell 7 API를 사용한다. 저장소 지침상 이번에는 실행하지 않았다. PS7.6.5 런타임이 발견된 사실을 이전의 “pwsh 미설치” 기록과 구분해야 한다.
- P3-2 비정상 종료 시 `%TEMP%\PDF2HWP\{jobId}` 렌더 폴더가 남을 수 있다. 정상 완료/예외 경로는 `finally`로 지우지만, 시작 시 stale cleanup은 `.pdf2hwp-workspace` 표식 폴더만 대상으로 한다 (`ConversionJobService.cs:66,105`, `ConversionWorkspaceFactory.cs`, `App.xaml.cs:9`).
- P3-3 UI 부제와 앱 패키지 설명은 HWP 고정밀 변환을 표방하지만 현재 앱은 HWPX만 출력하고 HWP adapter는 미지원 예외를 던진다 (`MainWindow.xaml:3`, `Pdf2Hwp.App.csproj:9`, `HancomAdapter.cs`).
- P3-4 Core에는 회전 연산이 있으나 UI에 회전/크롭 조작이 보이지 않는다. README의 “PDF page editor: Implemented”와 실제 노출 기능의 범위를 확인할 필요가 있다.
- P3-5 `run.err`, `run.out`, `section18.xml`이 Git에 추적되고 있다. 이 진단에서는 내용이나 파일을 건드리지 않았다. 커밋 제목 `11`도 변경 목적을 전달하지 못한다.
- P3-6 HWPX 검사기는 섹션의 페이지 배경 참조를 모은 뒤 첫 참조 중심으로만 확인한다 (`HwpxPackageInspector.cs:119-120`). 각 섹션의 배경 이미지 리소스 연결/비어 있지 않은 픽셀 내용까지 보장하지 못해 구조 PASS와 시각적 정확성은 별개다.

## 보존할 기존 검증/미검증

- 2026-09-28 기록: 패키지/라이선스 출처 감사, `eng/publish-win-x64.ps1`, `eng/verify-license-assets.ps1` 통과. 감사 이후 패키지나 배포 입력 변경은 확인되지 않았다.
- 현재도 NOT VERIFIED: Hancom Office 2024 샘플 #23 수동 렌더링, WPF GUI 스모크, 게시 EXE 실행, 위 P2-2~P2-5 동작 재현, 라이선스 검사기 재실행.
- 자동 테스트 110개 PASS는 이 공백들을 대신하지 않는다. CropBox 회귀 테스트는 합성 PDF 및 PDFium 렌더 경로를 검증한다.

## 우선 다음 단계

1. 출력 경로가 모두 `outputRoot` 아래에 있고 고유 run root가 보호 샘플 경로를 가리키지 않는지 정적으로 확인한다.
2. 사용자가 지정한 output root가 비어 있지 않을 때 첫 파일 쓰기 전에 중단하는지 검토한다. 사용자가 검토하기 전에는 생성기를 실행하거나 커밋하지 않는다.
3. 같은 generator 안전성 변경의 단위 검증을 추가하고 허가 후 검증한다.
4. Hancom 수동 검증은 UI helper 복구 후 기존 샘플 #23을 열기만 하고 저장하지 않는다.

## 환경 및 Git 상태

- Windows x64, .NET SDK 8.0.425 선택 검증 PASS. 현재 PC에는 SDK 10.0.401도 설치돼 있으므로 SDK 8만 설치된 시스템과 동일하지 않다.
- 기본 셸 PowerShell 5.1.26100; 관리 경로에서 `pwsh 7.6.5` 발견. 외부 서비스는 로컬 검증에 필요하지 않다. 비밀값은 기록하지 않는다.
- 브랜치 `codex/p2-6-safe-sample-output`, 분기 기준/원격 `main` HEAD `cd09492`. 현재 변경은 미커밋·미푸시다. 이전 main/feature push는 완료됐다.
- 이전 수정 브랜치와 `main`은 원격 푸시 완료. 현재 branch worktree는 수정 파일 미커밋·미푸시 상태다.
- 이번 브랜치 변경 파일: `tools/Pdf2Hwp.CompatibilitySampleGenerator/Program.cs`, `CHANGELOG.md`, `AGENTS.md`, `docs/handoff/CURRENT_HANDOFF.md`. 보호 샘플, `docs/recovery/`, 의존성은 미변경.
- 이번 생성기 코드는 실행하지 않았고 빌드/테스트도 NOT RUN이다. 교차 기기 준비도 `READY_WITH_WARNINGS`다.
