# 복구 감사 조치 기록

## 수행한 조치

- 저장소 경계 안에서만 Git 상태/diff, 프로젝트 설정, 관련 소스·테스트·문서와 canonical 검증 결과를 조사했다.
- 초기 변경 4개 구현/테스트 파일은 정상 목적 변경으로 판정해 보존했다.
- 격리 검증 과정에서 생성된 `.local-verification-20260927/`의 `.keep` 6개는 삭제하지 않았다. 해당 scratch 폴더만 Git 대상에서 제외하도록 `.gitignore`에 정확한 경로 규칙을 추가했다.
- README의 테스트 개수를 최신 canonical 결과인 89/89로 갱신하고, CHANGELOG `Unreleased`에 경고 상태 보존 변경을 기록했다.
- 오래된 내용이 현재 branch/HEAD 및 작업 상태와 충돌하던 `docs/handoff/CURRENT_HANDOFF.md`를 검증된 상태로 갱신했다.
- `eng/verify.ps1`를 실행해 Restore/Release Build/Test 각 종료 코드 0을 확인했다. TRX의 총 89건 전부 통과했으며 새 warning 회귀 테스트와 기존 동시 설정 저장 테스트도 통과했다.
- WPF UI 실행 smoke와 Hancom 수동 검증은 실행하지 않았다. 한컴 호환성 결과를 추정하지 않았다.

## 변경 경로

- 보존된 기존 개발 변경: `src/Pdf2Hwp.App/MainWindow.xaml.cs`, `src/Pdf2Hwp.Core/ApplicationContracts.cs`, `src/Pdf2Hwp.Infrastructure/ConversionJobService.cs`, `tests/Pdf2Hwp.Tests/ConversionJobServiceTests.cs`.
- 이번 감사 조치: `.gitignore`, `README.md`, `CHANGELOG.md`, `docs/handoff/CURRENT_HANDOFF.md`, `docs/recovery/` 감사 기록.
- Git add/commit/push/pull/fetch/merge/rebase/switch/checkout/reset/clean/stash는 수행하지 않았다.

## 삭제/복구

- 삭제한 프로젝트 파일: 없음.
- 되돌린 정상 개발 변경: 없음.
- Dependency/project 설정 변경: 없음.
- ACL, 보안 정책, OS 전역 설정 변경: 없음.
