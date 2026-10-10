# Current Project Handoff

Last updated: 2026-10-10 (Asia/Seoul)
Status: P1_CROPPING_FIXED / READY_WITH_WARNINGS

## Project

- 저장소: PDF2HWP (노트북 PDF-TO-HWP), Codex 담당. Windows x64 WPF / .NET 8, `PDF2HWP.sln`.
- 현재 지원 경로는 Visual Fidelity HWPX다. OCR, Editable 재구성, Safe Hybrid, PDF 대 결과물 비교, HWP 바이너리 출력은 미구현이다.

## 이번 수정

목표: 정밀진단에서 확인한 PDF CropBox 좌표 재적용으로 인한 픽셀 손실을 해결하고 회귀를 방지한다.

### 저장소 기준 상태

- 브랜치 `main`, HEAD `b5285fd7744214bad8a9763396f411a11e326ed3` (`docs: 정비 저장소 담당 및 인계 지침`). `origin/main` 로컬 추적 ref보다 1커밋 앞섬. 원격 최신 상태는 fetch하지 않아 확인하지 않았다.
- 시작 시 앞선 진단에서 남긴 `AGENTS.md`, `docs/handoff/CURRENT_HANDOFF.md` 변경이 있었다. 이번 작업은 렌더러, 테스트, CHANGELOG, AGENTS, 핸드오프를 수정했다. 의존성과 보호 자료는 변경하지 않았다.
- 2026-10-02 감사 이후 렌더러에는 관련 수정이 없었다. 새 픽셀 회귀는 기존 결함을 네 회전 모두에서 재현했다.
- `docs/recovery/`의 7개 파일은 Git 추적 대상이다. 보호 사용자 자료로 취급해 열람·수정하지 않았다.

### 검증 결과

- `eng/verify.ps1`, SDK `10.0.401`, `VSTEST_CONNECTION_TIMEOUT=300`.
- 결과: Restore PASS / Release Build PASS / Test PASS, **110/110 통과, 실패 0, 건너뜀 0**. 산출물 `artifacts/verification/20261010-120127/verification-summary.json` 및 `test.trx`.
- 신규 `Renderer_preserves_pixels_for_offset_asymmetric_cropbox`: 먼저 기존 코드에서 0/90/180/270도 모두 실패함을 확인했다. 수정 후 전체 CropBox가 검은 픽셀로 렌더되고 출력 크기도 각 회전에서 기대값과 일치한다.
- `git diff --check`: PASS (줄바꿈 정규화 안내만 있음).
- 샘플 #23 자동 구조 테스트 `PdfPipelineTests.Sample23_is_inspected_without_modification_and_keeps_manual_gate`: PASS. 검사 전후 SHA-256 일치와 3개 섹션을 확인한다. 현재 SHA-256 `CEF60905336D3AF0B1E2D308D54D16764B4CBF10DA90D1FAF06E26C1CD6D7DED`.
- Hancom Office 2024 수동 열기는 NOT VERIFIED. 실행 파일은 설치돼 있지만 Computer Use helper가 `windows sandbox failed: helper_unknown_error: setup refresh had errors`로 두 번 초기화되지 않아 문서를 열지 못했다.
- `eng/verify-license-assets.ps1`: NOT RUN. 저장소 AGENTS 지침의 명시적 실행 금지를 따랐다. 현재 기본 PowerShell은 5.1.26100이며 PATH에서 관리 런타임 `pwsh 7.6.5`도 발견했으나, 사용 허가를 추론해 실행하지 않았다. 마지막 기록상 라이선스 자산 검증은 2026-09-28 PASS다. 의존성 변경은 감사 기준 이후 없었다.
- 게시 EXE 실행, 실제 WPF UI 스모크, 샘플 생성 도구는 실행하지 않았다. 샘플 생성 도구는 보호 샘플을 덮어쓸 수 있어 실행 금지다.

## 확인된 활성 결함 및 위험

| 우선순위 | 상태 | 진단과 근거 |
|---|---|---|
| P1-1 | 해결 및 회귀 테스트 PASS | PDFium 기본 렌더링이 페이지 CropBox를 적용하므로 별도 `Bounds`에 CropBox 절대좌표를 재전달하지 않도록 했다 (`src/Pdf2Hwp.Infrastructure/PdfiumPageRenderer.cs`). 새 테스트가 오프셋 비대칭 CropBox `[20,10,120,60]`의 모든 픽셀과 회전별 크기를 확인한다 (`tests/Pdf2Hwp.Tests/PdfPipelineTests.cs`). 전체 검증 110/110 PASS. |
| P2-1 | 재현 기록 있음, 현 코드에서도 활성 | 기록 삭제가 저장소에서 새로 불러온 `History` 목록에서 선택 레코드의 위치를 찾아 `RemoveAt`한다 (`MainWindow.xaml.cs:175,280`). 10-02 감사 프로브에서 인덱스 -1과 삭제 실패를 재현했다. |
| P2-2 | 코드 경로 확인, 별도 재현 NOT RUN | `Convert`가 `ValidateOutput()`을 `_activeSnapshot` 갱신보다 먼저 실행하고, catch에서 `RecordOutcome()`을 호출한다 (`MainWindow.xaml.cs:47,52,75`). 이전 작업 스냅샷이 남아 있으면 다음 작업의 사전 검증 실패/취소가 이전 페이지 집합을 결과 기록으로 남길 수 있다. 기록 출력 경로도 `PDF2HWP_combined.hwpx`로 고정된다 (`:261-265`). |
| P2-3 | 코드 경로 확인, GUI 재현 NOT RUN | `OpenResults`, `ExportReport`, `ExportDiagnostics` 호출 경로에 예외 처리 경계가 없고 앱 전역 Dispatcher 예외 처리기도 없다 (`MainWindow.xaml.cs:79,80,101,252,267`, `App.xaml.cs`). 잘못된 경로나 쓰기 권한 문제에서 처리되지 않은 예외로 앱이 종료될 가능성이 있다. |
| P2-4 | 코드 경로 확인, 재현 NOT RUN | 여러 입력을 순차 처리하는 `MainViewModel.AddFilesAsync`에서 한 파일 분석 예외를 파일별로 처리하지 않는다 (`MainWindow.xaml.cs:190-`). 바깥의 단일 catch 때문에 실패 PDF 뒤의 유효한 파일이 추가되지 않을 수 있다 (`:29-33`). |
| P2-5 | 코드 경로 확인, 성능 영향은 입력 의존 | 페이지마다 `JobRenderCache.RenderAsync`가 원본 전체 SHA-256을 다시 계산한다 (`PreviewThumbnails.cs:27-30`). 미리보기 cache miss는 캐시 계층과 내부 renderer에서 각각 해시할 수 있다 (`:69,106`). 대형 다중 페이지 PDF에서 불필요한 전체 파일 I/O가 반복된다. 렌더 PNG도 페이지 리소스 바이트로 메모리에 모인다. |
| P2-6 | 덮어쓰기 경로 확인, 실행 금지 유지 | 호환성 샘플 생성기가 보호 대상 샘플 #23과 관련 fixture를 고정 경로에 작성한다 (`tools/Pdf2Hwp.CompatibilitySampleGenerator/Program.cs:34-42`). 이미지 패키지 writer는 출력 교체에 `File.Move(..., true)`를 사용한다 (`HwpxImagePackageWriter.cs:38,70,110`). 실행하면 보호 파일을 덮어쓸 수 있으므로 실행하지 않는다. |

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

1. 다음 작업은 P2-1 변환 기록 삭제 오류를 재현 테스트와 함께 수정한다.
2. 이후 P2-2 잘못된 스냅샷 기록, P2-3 UI 예외 처리, P2-4 파일별 PDF 추가 실패 격리, P2-5 원본 해시 재사용, P2-6 샘플 생성기의 안전한 출력 경로 순으로 해결한다.
3. Hancom 수동 검증은 helper 복구 후 기존 샘플을 열기만 하고 저장하지 않는다.

## 환경 및 Git 상태

- Windows x64, .NET SDK 8.0.425 선택 검증 PASS. 현재 PC에는 SDK 10.0.401도 설치돼 있으므로 SDK 8만 설치된 시스템과 동일하지 않다.
- 기본 셸 PowerShell 5.1.26100; 관리 경로에서 `pwsh 7.6.5` 발견. 외부 서비스는 로컬 검증에 필요하지 않다. 비밀값은 기록하지 않는다.
- 브랜치 `main`, HEAD `b5285fd7744214bad8a9763396f411a11e326ed3`, `origin/main`보다 1커밋 앞섬. 원격 상태는 fetch 금지 정책에 따라 미확인.
- 현재 수정 파일: `AGENTS.md`, `CHANGELOG.md`, `docs/handoff/CURRENT_HANDOFF.md`, `src/Pdf2Hwp.Infrastructure/PdfiumPageRenderer.cs`, `tests/Pdf2Hwp.Tests/PdfPipelineTests.cs`. 검증 산출물은 ignore 대상 `artifacts/verification/20261010-120127/`. 의존성 및 보호 파일은 미변경. 교차 기기 상태 `READY_WITH_WARNINGS`; 변경은 미커밋이며 Git 작업은 수행하지 않았다.
