# Current Project Handoff

Last updated: 2026-10-10 (Asia/Seoul)
Status: DOCUMENTATION_UPDATED / READY_WITH_WARNINGS

## Project and architecture

- 저장소: PDF2HWP (노트북 PDF-TO-HWP), Codex 담당.
- PDF를 HWPX로 변환하는 Windows x64 WPF 데스크톱 앱이며 .NET 8 솔루션은 `PDF2HWP.sln`이다. Core / Infrastructure / App / Tests 구조를 사용하고 HWP 바이너리 출력은 별도 한컴 어댑터 경계로 분리한다.
- 지원 변환 경로는 Visual Fidelity다. Safe Hybrid, Editable, OCR, PDF 대 출력물 시각 비교, HWP 바이너리 내보내기는 의도적으로 미구현이며 비활성 상태다.

## 현재 세션 (2026-10-10)

목표: 고정 담당표에 따라 저장소 OWNER를 확인하고, 저장소 지침과 인계 문서를 현재 Git 상태 기준으로 정비한다.

- 완료: `AGENTS.md` 맨 위에 OWNER/고정 담당표/공통 규칙/연동 규칙을 추가하고 현재 상태 요약을 반영했다. Claude의 쓰기 및 지정 Git 명령을 차단하는 `.claude/settings.json`을 생성했다. 기존 `CLAUDE.md`의 `@AGENTS.md` import는 이미 정상이라 변경하지 않았다.
- 연동: 연동 메모 중 이 저장소 항목이 없어 `docs/integration.md`는 만들지 않았다.
- 파일 분류: 작업 시작 시 변경·미추적 파일은 없었다. 이번 작업에서 수정한 `AGENTS.md`, `docs/handoff/CURRENT_HANDOFF.md`와 새 `.claude/settings.json`은 모두 커밋 대상으로 분류한다. `.gitignore` 대상이나 사용자 확인 필요 파일은 확인되지 않았다.
- 결정: 구체적인 5단계의 명시적 요청에 따라 Claude 차단 설정을 만들었다. 8단계의 포괄적인 설정 변경 금지와 충돌하는 경우 구체적인 파일 생성 지시를 우선 적용했다.
- 변경 전 상태: `main` / `f13fea5e56a9f69e628ad2b4e7831b7ced3f7265`; `main`은 `origin/main`과 일치했고 작업 트리는 깨끗했다. 최근 로그는 `f13fea5 connect`, `7a715cd CLAUDE`, `ca66318 11`, `a193197 docs: finalize PDF2HWP handoff after compliance commit`, `cd7ced4 fix: harden PDF2HWP production conversion pipeline`이다.
- 검증: 문서/설정 편집 후 `git diff --check`는 통과했다(LF/CRLF 정규화 경고만 표시). 설정 JSON 파싱도 통과했다. 빌드/테스트는 실행하지 않았다.

## 직전 검증 및 보존할 기록

- 2026-09-28 기록상 `eng/verify.ps1` Release 빌드 및 테스트가 통과했고 106/106 테스트가 통과했다. `eng/publish-win-x64.ps1`와 `eng/verify-license-assets.ps1`도 당시 통과 기록이 있다. 이번 세션에서는 재실행하지 않았다.
- 2026-09-28 당시 의존성/라이선스 감사와 PDFium 출처 검증이 완료된 것으로 기록돼 있다. 패키지나 배포 입력이 바뀌지 않으면 반복하지 않는다.
- 기존 한컴오피스 2024 수동 검증용 3페이지 샘플 #23 및 Golden 참조는 수정·덮어쓰기 금지다. `docs/recovery/`는 보호 사용자 자료다.

## 알려진 이슈와 남은 일

- 한컴오피스 2024에서 기존 샘플 #23의 수동 호환성 검증이 필요하다. 샘플/Golden은 읽기 전용으로 취급한다.
- .NET SDK 8만 설치된 환경에서의 별도 검증은 미확인이다. 이전 검증은 SDK `10.0.300`을 사용했다.
- OCR, Editable 재구성, Safe Hybrid 대체, PDF 대 출력물 시각 비교, HWP 바이너리 출력은 범위상 미구현이다.

## 다음 단계

1. 사용자가 Git 커밋/동기화를 수행한 뒤 다른 기기에서 문서 변경이 보이는지 확인한다.
2. 필요 시 한컴오피스 2024에서 기존 샘플 #23을 수정 없이 열어 검증하고 결과를 기록한다.
3. SDK 8 단독 검증이 필요할 때 별도 환경에서 `eng/verify.ps1`을 실행한다.

## 환경 및 교차 기기 상태

- 요구 환경: Windows x64, .NET SDK/runtime로 net8.0 프로젝트 복원·빌드·테스트, NuGet 복원. 수동 호환성 검증에 한컴오피스 2024가 필요하다.
- 필수 외부 서비스: 확인된 바 없음. 비밀값이나 `.env` 내용은 기록하지 않는다.
- 상태: `READY_WITH_WARNINGS`. 현재 확인한 브랜치는 `main`이며 시작 시 `origin/main`과 일치했다. 이번 수정은 아직 미커밋이므로 원격/타 기기 동기화는 사용자의 커밋 및 푸시 후 확인해야 한다.

## 현재 Git 상태

- 기준 HEAD: `f13fea5e56a9f69e628ad2b4e7831b7ced3f7265` (`connect`), 브랜치 `main`.
- 이번 변경 파일: `AGENTS.md`, `docs/handoff/CURRENT_HANDOFF.md`, `.claude/settings.json`.
- 커밋/푸시/브랜치 전환은 수행하지 않는다. 사용자에게 제안할 커밋 명령: `git add AGENTS.md docs/handoff/CURRENT_HANDOFF.md .claude/settings.json; git commit -m "docs: 정비 저장소 담당 및 인계 지침"`
