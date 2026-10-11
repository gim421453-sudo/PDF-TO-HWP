# Current Project Handoff

Last updated: 2026-10-12 (Asia/Seoul)
Status: INTEGRATION_READY / READY_WITH_WARNINGS

## Project

- 저장소: PDF2HWP (노트북 PDF-TO-HWP), Codex 담당. Windows x64 WPF / .NET 8, PDF2HWP.sln.
- 현재 지원 출력은 HWPX Visual Fidelity다. HWP 바이너리 출력, OCR, Editable 재구성, Safe Hybrid, PDF 대 결과물 비교는 미구현이다.
- 현재 브랜치는 main이다. 마지막 코드 통합 HEAD는 6c701c7이며, 이 핸드오프/AGENTS 동기화가 그 뒤에 추가됐다. origin/main은 cd09492로 유지되고 푸시는 하지 않았다.

## 목표 및 완료 작업

목표: 다섯 후속 개발 브랜치를 관계 조사, 위험 수정, 정적 검토, 테스트 후 main에 통합한다.

### 브랜치 관계 및 통합

공통 분기 기준은 cd09492a0c50d46c8d3e727bad6f8cb050bc8491이다. 다섯 브랜치는 각자 기준 커밋의 직접 자식이며 서로의 커밋을 포함하지 않은 독립 브랜치다. 소스 변경 파일은 겹치지 않았다. AGENTS.md와 docs/handoff/CURRENT_HANDOFF.md가 공통 문서 충돌 지점이었고, CHANGELOG.md도 여러 브랜치에서 수정됐다. 공통 문서는 최종 통합 상태로 정리하고 CHANGELOG 항목은 모두 보존했다.

| 작업 | 브랜치 커밋 | main 병합 커밋 | 결과 |
|---|---|---|---|
| P2-5 PDF SHA 캐시 | ba06748 | ae887f7 | 병합 및 검증 완료 |
| P2-6 안전 출력 예약 | d3ed42e | a861dc4 | 병합 및 검증 완료 |
| P3 임시 작업공간 | c8f10b3 | ef53763 | 병합 및 검증 완료 |
| P3 HWPX-only 문구 | e39474a | 13fe1ac | 병합 및 검증 완료 |
| Hancom 수동 게이트 | 9270ca3 | 6c701c7 | 문서 변경을 포함한 병합 커밋 |

통합 순서는 P2-5 → P2-6 → P3 임시 작업공간 → P3 출력 문구 → Hancom 수동 게이트였다. 예상된 공통 문서 충돌만 있었고 소스 코드 충돌은 없었다.

### 변경 파일

- P2-5: src/Pdf2Hwp.Infrastructure/PreviewThumbnails.cs, tests/Pdf2Hwp.Tests/AutomationExpansionTests.cs.
- P2-6: tools/Pdf2Hwp.CompatibilitySampleGenerator/Program.cs, src/Pdf2Hwp.Infrastructure/CompatibilityOutputReservation.cs, tests/Pdf2Hwp.Tests/CompatibilityOutputReservationTests.cs.
- P3 임시 작업공간: src/Pdf2Hwp.Infrastructure/ConversionJobService.cs, tests/Pdf2Hwp.Tests/ConversionJobServiceTests.cs, docs/application-architecture.md.
- P3 출력 문구: src/Pdf2Hwp.App/MainWindow.xaml, src/Pdf2Hwp.App/Pdf2Hwp.App.csproj.
- 문서 동기화: AGENTS.md, CHANGELOG.md, docs/handoff/CURRENT_HANDOFF.md. 의존성, 샘플 #23, Golden, docs/recovery는 변경하지 않았다.

### 위험 수정과 검토

- P2-5: JobRenderCache의 SHA-256 재사용 지문에 파일 길이, 마지막 쓰기 시각, Windows GetFileInformationByHandleEx의 ChangeTime을 포함했다. 신뢰할 변경 시각을 읽지 못하면 캐시된 SHA를 재사용하지 않고 다시 계산한다. 회귀 테스트는 같은 PDF 경로·길이·LastWriteTime을 유지한 채 PDF 본문만 변경하고, 새 SHA와 새 렌더를 확인한다. 현재 Windows에서는 변경 후에도 캐시 항목이 유지되는 것을 확인했다.
- P2-6: CompatibilityOutputReservation이 출력 폴더 내 표식을 FileMode.CreateNew로 획득하고 보유한 동안 생성기를 실행하도록 했다. 같은 경로 동시 예약 테스트는 한 실행만 성공하고 다른 실행은 IOException으로 실패하며, 기존 파일과 부분 파일 부재를 확인한다. 실제 샘플 생성기는 보호 자료를 덮어쓸 가능성이 있어 실행하지 않았다.
- 임시 작업공간: ConversionJobService에 IConversionWorkspaceFactory 주입 지점을 추가해 실제 ConversionWorkspaceFactory를 사용하는 테스트를 작성했다. 정상 완료, 취소, 렌더 실패, 검증 실패, 게시 실패에서 workspace와 .active 표식 정리, 최종/부분 출력 상태, 다음 작업 성공, 다른 job 임시 폴더 보존을 확인했다.
- 출력 형식: MainWindow.xaml의 부제와 Pdf2Hwp.App.csproj Description이 PDF → HWPX Visual Fidelity 지원 상태를 표시한다. 형식 UI도 HWPX이며 HWP 바이너리 출력 기능은 변경하지 않았다. 정적 검토와 전체 빌드를 통과했다.
- Hancom: 샘플 #23 수동 열기는 수행되지 않았으며 HANCOM_3PAGE_MANUAL_REQUIRED 상태다. 수동 한컴 검증 전 FULL_COMPATIBILITY를 선언하지 않는다.

## 검증 결과

모든 eng/verify.ps1 실행에서 Restore, Release Build, Test가 PASS했다. SDK는 10.0.401, VSTEST_CONNECTION_TIMEOUT=300이었다.

| 범위 | 산출물 | 테스트 | 결과 |
|---|---|---:|---|
| P2-5 브랜치 | artifacts/verification/20261012-015221 | 115/115 | PASS, 실패 0, 건너뜀 0 |
| P2-6 브랜치 | artifacts/verification/20261012-014955 | 116/116 | PASS, 실패 0, 건너뜀 0 |
| Hancom 브랜치 | artifacts/verification/20261012-015303 | 114/114 | PASS, 실패 0, 건너뜀 0 |
| 출력 문구 브랜치 | artifacts/verification/20261012-015243 | 114/114 | PASS, 실패 0, 건너뜀 0 |
| 임시 작업공간 브랜치 | artifacts/verification/20261012-015027 | 119/119 | PASS, 실패 0, 건너뜀 0 |
| P2-5 병합 후 | artifacts/verification/20261012-015916 | 115/115 | PASS, 실패 0, 건너뜀 0 |
| P2-6 병합 후 | artifacts/verification/20261012-020048 | 117/117 | PASS, 실패 0, 건너뜀 0 |
| 임시 작업공간 병합 후 | artifacts/verification/20261012-020148 | 122/122 | PASS, 실패 0, 건너뜀 0 |
| 출력 문구 병합 후 | artifacts/verification/20261012-020246 | 122/122 | PASS, 실패 0, 건너뜀 0 |
| Hancom 병합 후 | artifacts/verification/20261012-020338 | 122/122 | PASS, 실패 0, 건너뜀 0 |
| 최종 main | artifacts/verification/20261012-020423 | 122/122 | PASS, 실패 0, 건너뜀 0 |

다섯 feature worktree와 각 병합 상태에서 git diff --check를 실행해 모두 PASS했다. 보호 파일 변경은 없다.

## 보호 자료 및 검증 게이트

- 보호 샘플 #23: artifacts/compatibility-tests/23-visual-fidelity-a4-portrait-3pages-e2e.hwpx. SHA-256 CEF60905336D3AF0B1E2D308D54D16764B4CBF10DA90D1FAF06E26C1CD6D7DED. 통합 전후 값이 일치한다.
- Golden 참조: artifacts/hwpx-diff/20260909-020850/golden-hancom-minimal-reference.hwpx. SHA-256 CE7D9151F38FAA90C32C7D7A11C866EE81FA478FC2963A236194A8DDCCF69F4D. Git 추적 상태이며 변경 표시가 없다.
- docs/recovery/의 추적 파일은 열람·수정하지 않았다. CompatibilitySampleGenerator는 실행하지 않았다.
- eng/verify-license-assets.ps1은 저장소 지침에 따라 실행하지 않았다. 의존성 변경은 없다.

## 남은 확인 및 알려진 문제

- Hancom Office 2024에서 보호 샘플 #23을 실제로 열어 페이지 수와 렌더 결과를 확인하는 수동 검증은 NOT VERIFIED다. 이전 시도는 Computer Use helper 초기화 오류로 중단됐으므로 수동 검증 후 게이트 상태를 별도로 갱신해야 한다.
- WPF GUI 스모크와 게시 EXE 실행은 수행하지 않았다.
- 비정상 프로세스 종료로 임시 작업공간이 남을 수 있으며, 앱 시작 시 marked workspace stale cleanup이 처리하는 구조다. 이번 테스트는 정상 서비스 종료의 finally 정리 경로를 검증했다.
- HWPX inspector의 모든 섹션별 배경 리소스 연결을 깊게 확인하는 범위, UI의 회전/크롭 조작 노출, 추적 진단 파일 run.err/run.out/section18.xml 정리는 이 작업에 포함되지 않았다.
- 라이선스 자산 검사는 마지막 기록이 2026-09-28이며 이번에는 재실행하지 않았다.

## 다음 단계

1. Computer Use helper 복구 후 샘플 #23을 읽기 전용으로 열고 저장하지 않는다. 열기 전·후 SHA-256을 비교하고 실제 페이지 렌더를 기록한다. 실패하면 HANCOM_3PAGE_MANUAL_REQUIRED를 유지한다.
2. 원격 동기화는 사용자 요청 후 진행한다. 현재 origin/main은 cd09492이며 로컬 main에 후속 브랜치 병합과 문서 동기화가 있다.
3. 이후 별도 작업에서는 HWPX inspector의 섹션별 리소스 검증과 UI에서 회전/크롭 편집을 노출할지 우선순위를 정한다.

## 환경 및 교차 기기 상태

- Windows x64, .NET SDK 10.0.401로 최종 Restore/Release Build/Test를 검증했다. 저장소의 기본 앱 대상은 .NET 8이다.
- PowerShell 5.1.26100에서 eng/verify.ps1을 실행했으며 VSTEST_CONNECTION_TIMEOUT=300을 설정했다.
- 로컬 main은 다섯 merge commit과 다섯 feature commit을 포함한다. origin/main은 cd09492이고 푸시하지 않았다. 최종 상태는 READY_WITH_WARNINGS다.
- 비밀값이나 .env 내용은 기록하지 않았다.
