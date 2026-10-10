OWNER: Codex

## 프로젝트 담당표 (고정, 변경 계획 없음)

| 담당 | 저장소 / 노트북 |
|---|---|
| Claude | HoliDay-management / program-hub-v0.1 (노트북 program-hub) / firesystem-sim (노트북 fire-simulation) / apartment-simulator / Drane / school-selfstudy-attendance |
| Codex | Web Record (노트북 Web-Record) / chat-service / Hwp / PDF2HWP (노트북 PDF-TO-HWP) / MeMo / Print-Scan (노트북 PrintScan) / My Web (노트북 My-Web) / SchoolControl (school.-control) |
| HOLD | Book-First (BookCoverStudio) / backup-control-center |
| 공유 | Obsidian (별도 규칙) |

## 규칙
- 이 저장소의 OWNER와 다른 AI는 이 저장소를 수정, 생성, 삭제하지 않는다(읽기만 가능). 담당 변경은 하지 않는다.
- 다른 담당의 프로젝트 폴더는 열어서 수정하지 않는다. 필요한 변경은 사용자에게 알린다.
- HOLD 프로젝트는 사용자가 풀기 전까지 아무도 수정하지 않는다.
- 작업 시작 전 AGENTS.md, 핸드오프, git status, 현재 브랜치를 읽는다. 작은 단위로 수정 → 확인 → 커밋. 큰 변경 전에는 git-rollback.cmd mark <name>. 백업 파일(.bak)은 만들지 않는다.
- 삭제, 강제 푸시, 히스토리 수정, 데이터 삭제는 먼저 묻는다. API 키/비밀번호/토큰은 코드에 넣지 않고 .env로 분리한다.
- 작업이 끝나면 핸드오프와 AGENTS.md를 함께 갱신한다. 실행/테스트하지 않은 것은 "동작한다"고 쓰지 않는다.

## 연동 규칙 (지금은 구현하지 않음)
- 모든 프로젝트는 다른 프로젝트 없이 단독으로 빌드, 실행, 테스트가 통과해야 한다. 연동 코드는 지금 만들지 않는다.
- 나중 연동을 위한 입구만 남긴다: 외부와 주고받는 경계(API 엔드포인트, 이벤트, 설정값)는 한 모듈에 모으고, 연동 대상 주소와 키는 .env로 분리한다. 기능 플래그가 꺼져 있으면 연동 코드는 동작하지 않는다.
- 예정된 연동은 docs/integration.md 한 파일에 적는다: 상대 프로젝트, 방향(누가 호출), 요청/응답 형태, 상태(계획/미착수/구현).
- 상대 프로젝트의 담당이 다른 AI여도 저장소는 읽기만 하고, 필요한 변경은 docs/integration.md에 요청으로 남긴다. 상대 저장소를 직접 수정하지 않는다.
- 연동 때문에 현재 프로젝트의 구조를 크게 바꾸지 않는다.

## 현재 저장소 상태 (2026-10-10)
- 이번 작업에서 담당표와 문서 지침을 정비했다. 코드/의존성 변경은 없고 현재 브랜치는 `main`, HEAD는 `f13fea5e56a9f69e628ad2b4e7831b7ced3f7265` (`connect`)다.
- 완료: OWNER/담당표/공통 지침 반영, Claude 수정 차단 설정 생성, 핸드오프 최신화. `CLAUDE.md`의 `@AGENTS.md` import는 이미 정상이다.
- 남은 일: 사용자 Git 동기화 후 타 기기에서도 변경 문서가 보이는지 확인한다. 한컴오피스 2024의 기존 샘플 #23 수동 검증은 여전히 필요하다.
- 알려진 제한: .NET SDK 8 단독 환경 검증은 미확인. OCR/Editable/Safe Hybrid/HWP 바이너리 출력은 범위 밖 기능으로 미구현이다.
- 다음 단계: 필요 시 한컴오피스 2024에서 기존 샘플 #23을 읽기 전용으로 검증하고 결과를 기록한다. 샘플 #23과 Golden 참조는 덮어쓰지 않는다.
- 검증: 시작 시 `git status --short`는 깨끗했다. 문서/설정 편집 후 `git diff --check`는 통과했다(LF/CRLF 정규화 경고만 표시). 설정 JSON 파싱도 통과했다. 빌드/테스트는 실행하지 않았다.
- 파일 분류: 시작 시 미추적/변경 파일 없음. 이번에 수정/생성한 파일은 `AGENTS.md`, `docs/handoff/CURRENT_HANDOFF.md`, `.claude/settings.json`이며 모두 커밋 대상이다.
- 동기화 상태: 시작 시 `main`이 `origin/main`과 일치했다. 이번 변경은 아직 미커밋이므로 타 기기 동기화는 Git 커밋/푸시 후 확인해야 한다 (`READY_WITH_WARNINGS`).
# Cross-device continuation policy

<!-- PROJECT_HANDOFF_POLICY_START -->

## Session startup

Before performing meaningful project work:

1. Read `docs/handoff/CURRENT_HANDOFF.md` when present.
2. Read and follow the `project-handoff` skill.
3. Treat the handoff file as continuation context, not unquestionable truth.
4. Verify important state against the actual repository/workspace before making changes.
5. If the handoff conflicts with verified repository state, trust the verified state and record the discrepancy in the next handoff update.

## Mandatory session handoff

After any meaningful:

- implementation
- bug fix
- refactor
- configuration change
- architecture/design work
- testing/debugging
- deployment preparation
- migration work
- project planning that changes future implementation

you MUST use the `project-handoff` skill before giving the final response.

Update:

`docs/handoff/CURRENT_HANDOFF.md`

This is a completion requirement, not an optional documentation step.

Do not finish the task first and leave the handoff for a later session.

## Handoff scope

The handoff must capture enough verified state for another device or session to continue without asking the user to restate prior work.

At minimum record:

- session objective
- completed work
- files changed
- decisions made
- verification/tests
- known issues
- exact next action
- environment/config requirements
- cross-device readiness

## Cross-device readiness

Do not mark the project cross-device ready solely because files exist in the
current workspace. Check whether the handoff and required continuation rules
are tracked or otherwise confirmed to synchronize. If required files are
untracked or target-device synchronization is unknown, record that limitation
and use `READY_WITH_WARNINGS` or `BLOCKED` as appropriate. Do not run Git add,
commit, push, pull, or remote operations; the user owns those actions.

## Secrets

Never place secret values, credentials, tokens, passwords,
private keys, cookies, session data, or `.env` contents in handoff documentation.

Allowed:

- environment variable names
- secret file names
- service names
- Firebase/GCP/Cloudflare project aliases or IDs when non-secret
- configuration requirements

## Git policy

Codex may inspect Git state when necessary for accurate documentation.

Actual Git operations such as:

- clone
- init
- add
- commit
- pull
- push
- remote changes

are performed by the user unless the user explicitly changes this policy.

## Final response requirement

After updating the handoff, include a short confirmation:

`Cross-device handoff updated.`

If the handoff could not be updated, explicitly state why.

<!-- PROJECT_HANDOFF_POLICY_END -->

<!-- PROJECT_DOCUMENTATION_POLICY_START -->
## Automatic project documentation
Before finishing meaningful project-changing work, use the `project-docs-manager` skill and read `PROJECT_DOCS.config`.

Managed documents:
- `README.md`
- `CHANGELOG.md`
- `docs/deployment/DEPLOYMENT.md`
- development logs under the configured root
- `docs/handoff/CURRENT_HANDOFF.md`

Rules:
1. README: update only when project purpose, setup, usage, major features, architecture, configuration, supported platforms, or useful repository structure materially changes.
2. CHANGELOG: update only for meaningful release-relevant changes. Use `Unreleased` when no verified release version is known.
3. Deployment docs: update only when build/release, hosting, environment, migration, domain/routing, rollback/recovery, or production verification behavior changes.
4. Development-log automation is controlled by `DEVLOG_AUTO_UPDATE` in `PROJECT_DOCS.config`.
5. When `DEVLOG_AUTO_UPDATE=false`, do not create or update development logs unless the user explicitly requests one.
6. Cross-device handoff remains mandatory after meaningful project-changing work.
7. Preserve existing project-specific documentation; never replace it with generic content unnecessarily.
8. Never write secret values into managed documents.
<!-- PROJECT_DOCUMENTATION_POLICY_END -->

## Claude 승계 규칙 (agent-sync)

Codex 는 두 가지 방식으로만 동작한다.

### 리뷰 모드 (사용자가 명시적으로 리뷰를 요청한 경우)
- 파일을 수정하지 않는다.
- 실제 작업 트리와 `git diff` 를 기준으로 기능 결함, 회귀, 보안/권한, 데이터 손실, 테스트 누락을 찾는다.
- 결과는 한국어로 보고하고, 근거(파일 경로와 코드 위치 또는 명령 출력)를 붙인다.

### 승계 모드 (프롬프트에 `FALLBACK` 이 있고 Claude 가 한도로 중단된 경우)
1. `AGENTS.md`, `docs/handoff/CURRENT_HANDOFF.md` 맨 위 섹션, `.agent-sync/NEXT.md`, 프롬프트가 알려준 스냅샷을 읽는다.
2. `git status`, `git diff` 로 실제 상태를 확인한다. 핸드오프를 그대로 믿지 않는다.
3. NEXT.md 의 미완료 항목만 이어서 한다. 새 기능을 시작하지 않는다. 기존 미커밋 변경을 보존한다.
4. 아래 **보호 경로**는 수정하지 않는다.
5. git add / commit / pull / push, 브랜치 생성·전환, 의존성 설치·삭제, 운영 접근·배포, 보안 설정 변경을 하지 않는다.
6. 끝나면 아래 검증 명령을 실행하고, 이번 작업으로 생긴 실패만 최소 수정한다.
7. `docs/handoff/CURRENT_HANDOFF.md` 맨 위 섹션과 `.agent-sync/NEXT.md` 를 갱신한다.
8. 최종 보고는 한국어로 한다.

### 보호 경로 (Hwp)
- `docs/recovery/` 전체
- 한컴 수동 검증용 샘플 #23 과 Golden 참조 파일 (경로는 테스트 폴더와 핸드오프에서 확인하고 절대 덮어쓰지 않는다)
- `licenses/third-party/`, `THIRD_PARTY_NOTICES.md` (의존성이 바뀌지 않는 한 수정 금지)
- 서명 인증서, `.env*`, 비밀값이 들어 있는 모든 파일
- 샘플 생성 도구는 보호 샘플을 덮어쓸 수 있으니 실행하지 않는다

### 검증 명령 (Hwp)
- `eng/verify.ps1` (Restore + Release Build + Test)
- `git diff --check`
- `eng/verify-license-assets.ps1` 는 PowerShell 7 이 필요해서 이 PC(5.1)에서는 실패한다. 실행하지 않는다.
