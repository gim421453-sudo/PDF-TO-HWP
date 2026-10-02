# CLAUDE.md — Hwp (PDF2HWP)

이 파일은 Claude Code 가 세션마다 읽는 프로젝트 지침이다. 사용자는 한국어로 대화한다. 답변과 문서는 한국어로 쓴다.

@AGENTS.md

## 프로젝트

- **목적**: PDF 를 HWPX 로 변환하는 Windows 데스크톱 앱. HWP 바이너리 출력은 한컴 어댑터 경계로 분리돼 있다.
- **스택**: WPF + .NET 8 (`PDF2HWP.sln`). Core / Infrastructure(PdfPig, PDFtoImage·PDFium, HWPX 작성) / App(WPF) / Tests(xUnit)

## 작업 시작 전 (매 세션)

1. `AGENTS.md` 가 있으면 먼저 읽고 따른다 (위 import 로 이미 불러와진다). 없으면 만들지 않는다.
2. `docs/handoff/CURRENT_HANDOFF.md` 는 **맨 위(최신) 섹션만** 읽는다. 파일이 길고 아래는 과거 기록일 수 있다.
3. `git status -sb` 와 `git log -5 --oneline` 으로 **실제 상태**를 확인한다. 핸드오프와 다르면 실제 상태를 기준으로 하고 사용자에게 알린다.
4. 다른 프로젝트와의 관계나 전체 맥락이 필요하면 `../PROJECTS.md` 에서 이 프로젝트 섹션만 읽는다. 전체를 읽지 않는다.

## 검증 명령 (핸드오프에서 확인된 것)

- `eng/verify.ps1` — Restore + Release Build + Test, 106/106
- `eng/publish-win-x64.ps1` — 단일 파일 EXE + 고지 번들
- `eng/verify-license-assets.ps1` — 라이선스 자산 검증
- `git diff --check`

실제로 실행해서 결과를 확인하기 전에는 통과라고 보고하지 않는다. 명령이 바뀌었으면 위 목록과 `AGENTS.md` 를 함께 고친다.

## 이 프로젝트 주의사항

- 지원 모드는 Visual Fidelity 뿐이다. Safe Hybrid, Editable, OCR, PDF-출력물 비교, HWP 바이너리 내보내기는 **의도적 미구현**이며 UI에서 비활성이다. 임의로 켜지 않는다.
- 한컴오피스 2024 수동 검증용 3페이지 샘플 #23 과 Golden 참조 파일은 **수정·덮어쓰기 금지**다 (`HANCOM_3PAGE_MANUAL_REQUIRED`).
- 배포물에는 `THIRD_PARTY_NOTICES.md` 와 `licenses/third-party/` 를 반드시 포함한다. 없이 재배포하지 않는다.
- 패키지나 배포 입력이 바뀌지 않는 한 의존성 출처/서명 감사를 반복하지 않는다.
- 프로젝트는 .NET 8 대상이지만 검증 SDK 는 10.0.300 이었다. SDK 8 단독 환경 확인이 필요하다.
- `docs/recovery/` 는 미추적 사용자 자료다. 건드리지 않는다.
- 수정 커밋(`cd7ced4`)이 로컬에만 있을 수 있다. 푸시 여부를 확인한다.

## 공통 규칙

- 작업은 **이 저장소 폴더 안에서만** 한다. 다른 프로젝트 폴더는 읽지도 수정하지도 않는다.
- `git add` / `commit` / `push` / `pull` / `reset` / `clean` / `stash` 는 **사용자가 명시적으로 요청할 때만** 한다.
- `docs/recovery/` 는 사용자 자료이므로 건드리지 않는다.
- Windows 보안 설정(Smart App Control, Defender, WDAC)은 변경하거나 우회하지 않는다.
- 운영(production) 서비스·데이터에 쓰기·배포하지 않는다. 검증은 `demo-*` 프로젝트와 Emulator로 한다.
- 비밀값(키, 토큰, `.env`)은 읽어도 문서·로그·채팅에 출력하지 않는다.
- 검증하지 않은 것은 통과라고 쓰지 않고 `NOT VERIFIED` / `NOT RUN` 으로 남긴다. 테스트를 약화시켜서 통과시키지 않는다.
- 의존성 설치·삭제, `npm audit fix --force` 같은 큰 변경은 먼저 사용자에게 묻는다.
- 큰 수정은 먼저 계획을 보여주고 승인받은 뒤 진행한다.

## 문서 갱신 규칙

- 의미 있는 작업을 끝내면 `docs/handoff/CURRENT_HANDOFF.md` 를 **기존 형식 그대로** 갱신한다. 새 내용은 맨 위 최신 섹션에 쓰고, 해결된 과거 내용은 정리한다.
- **AGENTS.md 동기화 (사용자가 중단 지시하기 전까지 유지)**: 명령어, 폴더 구조, 규칙, 주의사항 같은 프로젝트 사실이 바뀌면 같은 변경을 `AGENTS.md` 에도 반영한다. 기존 내용과 형식은 보존하고 필요한 부분만 최소 수정한다. Codex 전용 문구는 임의로 지우지 않는다. `AGENTS.md` 가 없으면 만들지 말고 사용자에게 묻는다.
- `CHANGELOG.md` 가 있으면 사용자에게 보이는 변경만 기록한다. `DEVLOG_AUTO_UPDATE=false` 이면 개발일지를 만들지 않는다.

## 에이전트 승계 (agent-sync)

- 하위 작업 하나가 끝날 때마다 `.agent-sync/NEXT.md` 를 갱신한다 (방금 한 일 / 다음에 할 일 / 막힌 것 / 건드리지 말 것 / 검증 상태). 이 파일은 git 추적 대상이 아니다.
- 한도 소진으로 내가 멈추면 Codex 가 이어받는다. '다음에 할 일' 은 Codex 가 그대로 실행할 수 있을 만큼 구체적으로 쓴다 (파일 경로, 함수 이름, 실행할 명령).
- 세션을 시작할 때 `.agent-sync/state.json` 의 `phase` 를 본다.
  - `codex_running`: 파일을 수정하거나 테스트/빌드를 실행하지 말고 사용자에게 알린다.
  - `codex_done` 또는 `codex_failed`: 작업 전에 `git status`, `git diff`, Codex 로그와 `.agent-sync/NEXT.md` 를 읽고 Codex 가 바꾼 내용부터 파악한다.
- 마일스톤마다 `docs/handoff/CURRENT_HANDOFF.md` 를 갱신한다 (기존 규칙 유지).
- git add / commit / pull / push 는 사용자가 요청할 때만 한다 (기존 규칙 유지).
