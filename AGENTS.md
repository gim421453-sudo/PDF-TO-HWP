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
