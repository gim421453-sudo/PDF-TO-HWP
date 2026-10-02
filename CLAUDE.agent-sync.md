## 에이전트 승계 (agent-sync)

- 하위 작업 하나가 끝날 때마다 `.agent-sync/NEXT.md` 를 갱신한다 (방금 한 일 / 다음에 할 일 / 막힌 것 / 건드리지 말 것 / 검증 상태). 이 파일은 git 추적 대상이 아니다.
- 한도 소진으로 내가 멈추면 Codex 가 이어받는다. '다음에 할 일' 은 Codex 가 그대로 실행할 수 있을 만큼 구체적으로 쓴다 (파일 경로, 함수 이름, 실행할 명령).
- 세션을 시작할 때 `.agent-sync/state.json` 의 `phase` 를 본다.
  - `codex_running`: 파일을 수정하거나 테스트/빌드를 실행하지 말고 사용자에게 알린다.
  - `codex_done` 또는 `codex_failed`: 작업 전에 `git status`, `git diff`, Codex 로그와 `.agent-sync/NEXT.md` 를 읽고 Codex 가 바꾼 내용부터 파악한다.
- 마일스톤마다 `docs/handoff/CURRENT_HANDOFF.md` 를 갱신한다 (기존 규칙 유지).
- git add / commit / pull / push 는 사용자가 요청할 때만 한다 (기존 규칙 유지).
