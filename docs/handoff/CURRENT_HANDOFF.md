# Current Project Handoff

Last updated: 2026-10-19 (Asia/Seoul)
Status: READY_WITH_WARNINGS / PERFORMANCE_CAUSE_STILL_UNCLEAR / HANCOM_3PAGE_MANUAL_REQUIRED

## 최신 세션 — PC 포맷 전 Git 보존 및 재개 준비 (2026-10-19)

- 사용자 요청: PC 포맷 후 재개할 수 있도록 상세 핸드오프를 작성하고 필요한 병합·커밋·GitHub push까지 수행한다. 추가 답변으로 진단 파일도 Git에 함께 보존하도록 명시 승인했다. 이전 진단 디렉터리 커밋 제외 지시는 이번 보존 범위에서 변경됐다.
- 저장소 `https://github.com/gim421453-sudo/PDF-TO-HWP.git`, 브랜치 `main`. 시작 HEAD와 fetch 후 origin/main은 모두 `75c99df72c421fc4caa8febe278ef5c0ae3d37f1`, ahead/behind `0 0`. Git fetch는 기본 sandbox의 `.git/FETCH_HEAD` 쓰기 제한으로 한 번 실패했고 승인된 권한으로 재실행해 성공했다.
- 병합 조사: 로컬 feature 브랜치 9개 모두 `git merge-base --is-ancestor <branch> main`이 성공했다. `git branch --no-merged main`은 비어 있고 연결된 feature worktree 8개 모두 clean이다. main에 없는 작업이나 합칠 새 커밋을 발견하지 않았다. 추가 merge/rebase/브랜치 전환은 필요하지 않다. 옛 worktree는 Temp 아래 경로이며 포맷 후 복구할 필요가 없다.
- 보존 범위: 이 핸드오프, `docs/handoff/REFORMAT_RECOVERY.md`, `docs/handoff/DIAGNOSTIC_ASSETS.csv`, `artifacts/compatibility-tests/diagnostic/README.md`, 기존 진단 자산 17개. 자산은 총 633,125바이트이며 파일 내용은 변경하지 않았다. production 소스, 의존성, 설정, AGENTS.md, 보호 원본/Golden/reference, 라이선스 원문은 변경하지 않았다.
- 복구 가이드: **[REFORMAT_RECOVERY.md](REFORMAT_RECOVERY.md)**에 재설치 환경, clone·SHA 확인·빌드·실행·배포 명령, Git 밖 개인 상태, 정확한 진단 재개 절차를 상세히 기록했다. **[DIAGNOSTIC_ASSETS.csv](DIAGNOSTIC_ASSETS.csv)**는 보존 자산 17개의 경로·바이트 길이·SHA-256 원장이다. 포맷 후 먼저 이 원장으로 모든 파일을 확인한다.
- 검증: manifest 17개 해시/길이 기록, HWPX 11개 ZIP 전체 entry 읽기 및 XML/HPF 구문 해석 PASS, PNG 6개 signature PASS, 보호 SHA 3경로 일치. 이 검사는 Hancom 호환성 또는 성능 PASS를 뜻하지 않는다. 기능 변경이 없어 canonical/publish/Hancom GUI 재실행은 NOT RUN이다. 과거 실제 canonical `20261012-140749/test.trx` counters는 이번에 다시 읽어 total/executed/passed 122, failed/notExecuted 0을 확인했다. 원본 로그는 ignored라 이번 push 대상이 아니다.
- 측정 근거 정정: 아래 2026-10-17의 약 1.3~2.2초는 정밀 first-paint 측정값이 아니며, 자동 대기와 일부 도구 호출 시간의 참고값이며 실제 paint 시점과 호출 사이 공백을 분리해 측정하지 못했다. 성능 상한/벤치마크로 사용하지 않는다. 특히 23A/23K의 warm 시도는 기존 창 ID·뷰포트가 유지돼 이미 열린 문서 재활성화일 가능성이 높다. 실제 정상 닫기→재로딩으로 검증된 warm 결과로 간주하지 않는다. 당시 문서 렌더링과 응답 관찰은 보존하되 원인 확정 근거로 확대하지 않는다.
- manual-test 주의: `diagnostic/manual-test/23D-low-resolution-background-manual.hwpx`는 의도치 않은 한글 한 글자 입력 가능성이 있는 forensic 자료다. 원본 23D와 SHA가 다르다. 성능/호환성/저장·재열기 PASS의 근거로 사용 금지. 이번에는 바이트 그대로 보존만 했다.
- 지원/게이트: 현재 실제 출력은 HWPX Visual Fidelity. OCR/Editable/Safe Hybrid/HWP binary/PDF 대 출력 자동 시각 비교는 미구현 또는 미검증이다. `HANCOM_3PAGE_MANUAL_REQUIRED` 유지, `FULL_COMPATIBILITY` 선언 금지. 성능 원인은 미확정이고 현재 증거만으로 production writer를 바꿀 근거가 없다.
- 환경 확인: PowerShell 7.6.5, 기본 .NET SDK 10.0.401, 추가 SDK 8.0.425. WPF target은 net8.0-windows/x64. Hancom Office 2024와 Windows Computer Use 제어는 포맷 후 재설치/정상 접근을 확인한다. 전역 Codex 플러그인과 개인 인증은 저장소 clone만으로 복구되지 않는다.
- 포맷 후 정확한 첫 작업: 아래 복구 가이드 순서대로 clone → main/origin 동기화 확인 → 보호 SHA/진단 manifest 확인 → PowerShell 7·.NET 환경 확인 → `eng/verify.ps1` 실행. 최소 122 tests, 실패 0, skip 0을 새 환경에서 확인한다. 그 뒤 화면 배율·프로세스·열기/정상 닫기 조건을 고정해 23A↔23K 및 필요시 04/15/21/23E/F/G/H/I/J 비교를 재개한다. 원본 #23은 읽기 전용이며 저장하지 않는다.
- Git checkpoint: 보존 커밋 메시지는 `docs: preserve reformat recovery handoff and Hancom diagnostics`. 이 문서의 자기 커밋 SHA는 미리 기록하지 않는다. 커밋·push 후 `git rev-parse HEAD`, `git rev-parse origin/main`, `git rev-list --left-right --count HEAD...origin/main`, `git status --short`로 동기화와 clean을 확인한다. 원격에 예상치 못한 커밋이 생기면 자동 이력 변경/force push하지 않는다.
- 교차 기기 상태 `READY_WITH_WARNINGS`: 이번 보존 커밋이 origin/main에 반영됐는지 최종 Git 확인 후 포맷한다. 소스/프로젝트 스킬/진단 파일은 Git으로 복구 가능하다. 개인 원본 PDF/결과물, `%LOCALAPPDATA%/PDF2HWP`의 설정·최근 파일·이력, Hancom 라이선스, GitHub/Codex 인증·개인 설정은 별도 백업 여부 미확인이다. `.agent-sync/`의 로컬 상태와 ignored 실행 산출물은 이번 보존 대상이 아니다. `docs/recovery/` 사용자 보호 자료는 열람/수정하지 않았다. PC 전체 백업이 완료됐다는 뜻은 아니다.

## 이전 세션 — Hancom 성능 원인 최종 분리 (2026-10-17)

- 목표: 동일한 Hancom Office 2024 프로세스에서 04, 15, 21, 23E, 23H의 first/warm open을 비교하고, 23A 배경 전용 3페이지와 23K 본문 텍스트+배경 3페이지를 비교한다. 보호 원본 #23과 Golden은 열거나 수정하지 않았다.
- 시작/종료 브랜치 `main`, HEAD와 `origin/main` 모두 `75c99df72c421fc4caa8febe278ef5c0ae3d37f1`. 코드, 의존성, 설정, AGENTS.md는 수정하지 않았다. 커밋/푸시/브랜치 변경은 없다.
- Computer Use로 Hancom 2024를 새로 실행해 같은 프로세스 세션에서 순서대로 비교했다. 04, 15, 21, 23E, 23H, 23A, 23K 각각 첫 열기와 재열기를 확인했다. 파일 선택 대화상자에서 Enter 후 첫 화면 캡처까지 관찰 지연은 대체로 약 1.3~2.2초였으며 정밀 타이머 벤치마크는 아니다. 파일 경로 선택/입력 시간은 이 수치에 포함하지 않았다. CPU/메모리 사용률은 측정하지 않았다.
- 관찰 결과: 04 1/1쪽 텍스트, 15 1/1쪽 이미지와 겹친 안내 텍스트, 21 1/1쪽 페이지 배경+본문, 23E 고해상도 1/1쪽, 23H 저해상도 1/1쪽이 모두 빠르게 표시됐다. 손상/복구 경고는 보이지 않았다. 04·21·23A·23K에서 스크롤이 응답했다. 21은 130% 확대에서 배경이 화면 밖으로 잘렸고 회색 영역도 보였으나 확대/뷰포트 관찰이며 문서 손상으로 판정하지 않았다. 빈 문서 시작 화면에 선택된 검은색 `가`가 잠시 보였지만 입력하거나 저장하지 않았고, 열어 본 샘플에는 나타나지 않았다.
- 23A는 배경만 있는 3쪽으로 열렸고 상태 표시줄에 1/3쪽이 표시됐다. 23K는 본문 텍스트와 배경이 함께 보이는 3쪽으로 열렸고 접근성 텍스트에 PAGE 1~3이 확인됐다. 23A와 23K의 first/warm 화면 표시 차이는 관찰 범위에서 크지 않았다. warm 재열기 시 이전 스크롤 위치(23A는 3쪽, 23K는 이전 위치)가 유지되어 첫 화면이 해당 위치에서 보였고, 23K는 위로 스크롤해 1쪽 텍스트와 배경을 다시 확인했다.
- 원인 판단: 이번 세션에서는 과거의 20~30초대 지연이 재현되지 않았다. 1쪽 고/저해상도(23E/23H) 사이의 뚜렷한 차이는 없었고 3쪽 배경 전용(23A)과 본문+배경(23K)도 관찰상 비슷했다. 따라서 이미지 해상도(H3), 본문/layout(H1), 3-section/background(H2) 중 하나를 원인으로 확정할 증거가 없다. Hancom 초기화/캐시/환경 영향도 가능성으로만 남는다. `PERFORMANCE_CAUSE_STILL_UNCLEAR`; 현재 측정만으로 production 코드 수정을 제안하지 않는다.
- 진단 샘플 `artifacts/compatibility-tests/diagnostic/23K-background-plus-text-3page.hwpx`를 새로 만들었다. 23A의 패키지/세 section/page background/ref를 유지하고 원본 #23의 같은 section별 기존 텍스트 run과 lineseg를 적용했다. body `hp:pic`은 없으며 A4 세로, 세 section, manifest 참조, ZIP/XML 구조 검사를 통과했다. 내부 `HwpxPackageInspector`에서 23A와 23K 모두 두 차례 `Pass`였다. 23K SHA-256 `EA03D78AE26BB2A1BFCB3AB352E77ABCC09112A6CB8EABBA43EB36FF3739CA03`. 진단 파일은 미추적 로컬 산출물이며 커밋 대상에 넣지 않는다.
- 보호 확인: 원본 #23 `CEF60905336D3AF0B1E2D308D54D16764B4CBF10DA90D1FAF06E26C1CD6D7DED`, Golden `artifacts/reference-hancom/hancom-minimal-reference.hwpx` `CE7D9151F38FAA90C32C7D7A11C866EE81FA478FC2963A236194A8DDCCF69F4D`. 기대 SHA와 일치한다. 원본 #23은 Hancom에서 열지 않았고 저장하지 않았다.
- Git 상태는 추적 파일 변경 없음, `artifacts/compatibility-tests/diagnostic/` 미추적이다. `git diff --check` PASS. AGENTS.md는 영구 운영 규칙만 유지했다. 핸드오프 변경과 새 진단 HWPX는 아직 커밋/푸시하지 않았다.
- 미완료/제한: 표준화한 정밀 초 단위 벤치마크와 CPU 관찰은 하지 않았다. 실제 보호 원본 #23의 Hancom 수동 호환성 검증도 이 작업에 포함하지 않았다. `HANCOM_3PAGE_MANUAL_REQUIRED` 유지, `FULL_COMPATIBILITY` 선언 금지.
- 다음 정확한 작업: 기존 장시간 지연이 재현되면 동일 PC/동일 Hancom 세션에서 화면 배율·첫 화면 위치를 고정하고 first/warm 타이밍 및 CPU를 기록해 비교를 반복한다. 그 전까지 성능만을 이유로 production writer를 변경하지 않는다. 원본 #23 수동 시각 검증은 별도 승인된 보호 절차에 따라 진행한다.
- 교차 기기 준비도 `READY_WITH_WARNINGS`: 핸드오프 수정분과 진단 디렉터리는 미커밋/미추적 상태이며 다른 기기 동기화가 확인되지 않았다.

## 이전 세션 — 진단 문서 정리 (2026-10-17)

- 목표/완료: AGENTS.md의 영구 운영 규칙 정리를 그대로 유지하고, 이 문서의 빈 중복 제목과 최신 재개 순서에 충돌하는 문구만 정리했다. 과거 진단 데이터와 절차 기록은 보존했다. 이번에 편집한 파일은 이 핸드오프뿐이다. Hancom GUI 측정과 빌드/테스트는 NOT RUN이다.
- 브랜치 main, HEAD 29d8640837d13fcb57f858aa8fd8c256d9f4478a. 향후 문서 커밋 후보는 AGENTS.md와 docs/handoff/CURRENT_HANDOFF.md 두 파일뿐이다. git add/commit/push는 하지 않았다. git diff --check PASS. AGENTS.md, 보호 샘플 #23, Golden 및 diagnostic/ 전체 파일은 편집 전후 SHA-256이 같다.
- manual-test 주의: artifacts/compatibility-tests/diagnostic/manual-test/23D-low-resolution-background-manual.hwpx는 사용자 제공 이력상 이전 Computer Use 저장 테스트에서 의도치 않은 한글 한 글자가 입력됐을 가능성이 있다. 원본 diagnostic 23D와 byte-identical이 아니다. 호환성/성능 및 정상 저장/재열기 PASS의 근거로 사용하지 않는다. forensic/reference 목적으로 로컬 보존하며 추가 수정·삭제하지 않는다.
- diagnostic/ 정책: HWPX 샘플, extracted/lowres PNG, manual-test를 포함한 artifacts/compatibility-tests/diagnostic/ 전체는 기존 미추적/로컬 상태로 유지하고 이번 문서 커밋에서 제외한다. .gitignore는 변경하지 않았다. 성능 진단 완료 후 보존/삭제/fixture 편입 여부를 결정한다.
- 다음 정확한 작업: Windows 앱 제어가 가능한 Computer Use와 한글 2024를 준비한 뒤, 동일 Hancom 세션에서 23E highres 1P → 23F highres 2P → 23G highres 3P → 23H lowres 1P → 23I lowres 2P → 23J lowres 3P 순서로 각 파일의 first/warm open을 비교한다. 문서는 저장하지 않는다. 이전 Computer Use 차단 이력과 원인 미확정 상태는 아래 진단 기록을 참조한다. HANCOM_3PAGE_MANUAL_REQUIRED를 유지하며 FULL_COMPATIBILITY를 선언하지 않는다.
- 교차 기기 준비도 READY_WITH_WARNINGS: 두 문서는 추적 중이지만 현재 수정분은 미커밋이며 진단 파일은 미추적이다. 다른 기기 동기화는 확인되지 않았다.

## 이전 세션 — Hancom 렉 3차 성능 비교 (2026-10-15)

- 목표: 한글 2024를 유지한 동일 세션에서 23E→23F→23G→23H→23I→23J를 각각 first/warm 두 번 열어 페이지 수 및 해상도 영향을 비교한다.
- 결과: HANCOM_COMPUTER_USE_BLOCKED. 현재 Computer Use의 cua.getState()는 apps=[]와 브라우저 두 개만 반환했다. getApp/listApps/listWindows는 모두 undefined였다. 요청의 중단 조건에 따라 Windows 앱 제어를 우회하지 않고 측정을 중단했다. Hancom을 실행하거나 문서를 열지 않았으므로 이번 세션의 여섯 파일 first/warm 및 페이지 이동·스크롤·경고·배경·잘림·CPU·Memory·Disk 결과는 전부 NOT RUN이다. Hancom의 FREEZE 또는 손상으로 판정한 것이 아니다.
- 기존 참고값(정밀 측정 아님): 04 first 약 25초 / warm 약 12초, 15 약 20초, 21 약 30초, 23E 약 20초, 23F 약 22초. 이번 동일 세션 측정으로 재검증하지 않았고 현재 결과와 섞어 사용하지 않는다.
- 진단 파일 23E/23F/23G/23H/23I/23J 모두 존재한다. SHA-256 비교로 23G==23A, 23J==23D를 확인했다. 새 샘플 또는 semantic을 만들지 않았다.
- 원인 판정: section scaling, image resolution, page-background semantic, body text는 UNKNOWN. Hancom initialization/cache는 기존 04 first/warm 차이로 조사 후보이나 HIGH/MEDIUM/LOW를 확정할 자료가 부족하다. PERFORMANCE_CAUSE_STILL_UNCLEAR. 측정 없이 production writer 변경 방향을 정하지 않는다.
- 문서 정리: AGENTS.md의 날짜별 현재 저장소 상태 블록을 제거하고 상태/측정은 이 핸드오프에만 기록하도록 영구 운영 규칙을 정리했다. 기존 담당표·보호 경로·승계 정책을 보존했고 수동 검증 전 FULL_COMPATIBILITY 선언 금지 규칙은 유지했다. 이전 상태 내용은 아래 역사 기록에 보존되어 있다.
- 시작/종료 브랜치 main, HEAD 29d8640837d13fcb57f858aa8fd8c256d9f4478a. 코드·의존성·설정·샘플 변경 없음. 문서 변경은 AGENTS.md와 이 핸드오프뿐이며 artifacts/compatibility-tests/diagnostic/는 기존 미추적 상태다. commit/push 또는 브랜치 전환은 하지 않았다. 빌드/테스트 NOT RUN(성능 측정 작업이며 기능 변경 없음).
- 보호 샘플 #23 SHA-256 전/후: CEF60905336D3AF0B1E2D308D54D16764B4CBF10DA90D1FAF06E26C1CD6D7DED. Golden SHA-256 전/후: CE7D9151F38FAA90C32C7D7A11C866EE81FA478FC2963A236194A8DDCCF69F4D. 둘 다 기존 기대값 일치. git diff --check PASS.
- HANCOM_3PAGE_MANUAL_REQUIRED 유지. FULL_COMPATIBILITY 선언하지 않음.
- 다음 정확한 작업: Windows 앱 제어를 실제 제공하는 Computer Use 세션에서 한글 2024를 먼저 준비하고 다른 문서를 정상 닫은 뒤, 동일 프로세스에서 23E/F/G/H/I/J 각각 first/warm를 측정한다. 문서 저장 없이 정상 닫고 전체 여섯 파일을 같은 조건으로 다시 측정한다. 완료 후 보호 해시와 Git 상태를 확인하고 이 핸드오프만 갱신한다.
- 교차 기기 준비도 READY_WITH_WARNINGS: AGENTS.md와 핸드오프는 추적 중이지만 수정분을 commit/push하지 않았고 진단 파일은 미추적이므로 다른 기기 동기화는 미확인이다.

## 이전 세션 — Hancom Visual Fidelity 2차 성능 분리 (2026-10-15)

- 목표: 문서 페이지 수와 배경 이미지 해상도에 따른 Hancom Office 2024 표시 지연을 비교한다. 현재 측정은 GUI에서 눈으로 확인한 대략값이며 정밀 벤치마크가 아니다.
- 앞서 완료한 기준 샘플 측정: 04 첫 열기 약 25초, 15 약 20초, 21 약 30초. 앱을 띄워 둔 뒤 04 재열기는 약 12초였다. 당시 손상/복구 경고는 없고 UI는 응답했다. 21은 130% 확대에서 화면 오른쪽이 잘려 보여 문서 손상으로 판정하지 않았다. 이 샘플들의 과거 Hancom PASS 기록은 발견되지 않았다.
- 고해상도 확장 측정: 23E(1페이지)는 약 20초 대기 중 첫 페이지가 표시됐다. 23F(2페이지)는 약 22초 대기 중 첫 페이지가 표시됐고, 2페이지 이동은 약 2.5초였다. 상태 표시줄은 각각 1/1, 1/2였으며 경고 없이 UI가 응답했다. CPU/메모리는 측정하지 않았다. 수치는 표시 확인 시점 기준의 대략값이다.
- 미완료: 고해상도 3페이지 23G와 저해상도 1·2·3페이지 23H/23I/23J의 GUI 비교. 23G는 23A와, 23J는 23D와 각각 바이트 동일한 진단본이므로 해당 짝은 한 파일만 열어도 같은 패키지다. 이번 재시도에서 `cua.getState()`가 앱 목록을 비우고 브라우저만 반환했으며 현재 Computer Use 런타임은 브라우저 API만 제공했다. `cua.listApps`도 사용할 수 없어 Windows Hancom 창에 접근할 수 없었다. 이는 Hancom 열기 실패나 응답 없음 판정이 아니라 제어 기능 미노출이다.
- 이번 재시도 중 문서 저장, Hancom 설정 변경, 프로세스 강제 종료, 코드/의존성/설정 변경은 없었다. 실제 GUI 비교는 미완료이며 원인 가설을 확정하지 않는다.
- 보호 원본 #23 SHA-256은 `CEF60905336D3AF0B1E2D308D54D16764B4CBF10DA90D1FAF06E26C1CD6D7DED`, Golden은 `CE7D9151F38FAA90C32C7D7A11C866EE81FA478FC2963A236194A8DDCCF69F4D`로 현재 확인값이 기대값과 일치한다. `23G`/`23A`, `23J`/`23D`의 SHA-256도 각각 동일하다.
- 브랜치 `main`, HEAD `29d8640837d13fcb57f858aa8fd8c256d9f4478a`. 현재 변경 표시는 `AGENTS.md`, `docs/handoff/CURRENT_HANDOFF.md`, 미추적 `artifacts/compatibility-tests/diagnostic/`이다. 코드 변경, 커밋, 푸시는 없다. 진단 파일이 미추적이므로 교차 기기 동기화는 미확인 (`READY_WITH_WARNINGS`).
- 상태: `HANCOM_3PAGE_MANUAL_REQUIRED` 유지. `FULL_COMPATIBILITY` 선언 금지.
- 다음 단계: Computer Use가 Windows 네이티브 앱을 노출하는 세션에서 Hancom을 준비하고, 23G(또는 바이트 동일 23A) 뒤 23H, 23I, 23J(23D와 동일)를 열어 페이지 이동/스크롤/경고/첫 표시를 기록한다. 각 문서는 저장하지 않고 정상 종료한다. 이후 H1~H4 가설을 비교하고 보호 샘플 #23과 Golden 해시를 재확인한다.

## 이전 세션 — Hancom sample #23 렉 원인 격리 (2026-10-12)

- 목표: 보호 샘플 #23과 Golden을 보존하면서 한컴 지연 원인 확인용 비교 샘플을 만든다. 브랜치 `main`, 시작/종료 HEAD `29d8640837d13fcb57f858aa8fd8c256d9f4478a`. 코드, 의존성, 설정은 변경하지 않았고 커밋/푸시는 하지 않았다.
- 원본 #23은 읽기 전용으로 검사했다. 세 section 모두 A4 세로 `59528×84189` HWPUNIT, section별 `hp:p=1`, `hp:run=2`, `hp:t=1`, `hp:pic=0`이다. 본문 텍스트는 각각 `PAGE 1 / P1 TL / P1 TR / P1 CENTER / P1 BL / P1 BR` 대응 문자열이며 세 배경 PNG는 모두 `1653×2338`, 총 압축 전 이미지 119,426바이트다.
- `artifacts/compatibility-tests/diagnostic/23A-background-only.hwpx`: 기존 `HwpxImagePackageWriter.WritePageBackgroundsAsync` 경로에서 생성한 검증된 최소 section XML만 원본 패키지의 section0~2에 적용했다. 본문 텍스트/lineseg를 제거했으며 원본 패키지의 다른 항목의 압축 해제 내용, `pagePr`, header, manifest, 배경 PNG와 background refs는 보존했다. 원본과 내부 ZIP 항목 내용 비교 시 달라진 항목은 section0~2 XML뿐이다. ZIP 압축 스트림은 재생성됐다.
- `artifacts/compatibility-tests/diagnostic/23B-current-clone.hwpx`: 원본을 다른 이름으로 복제한 byte-identical 대조군이다. 현재 background writer는 본문 텍스트를 제거하므로 원본 텍스트와 배경을 함께 재생성할 검증 경로가 없어, 보존 우선으로 정확 복제했다.
- `23C-single-section-3pages.hwpx`는 만들지 않았다. 현재 writer는 입력 페이지마다 section을 만들고 단일 section 내 서로 다른 세 logical page 배경을 만드는 검증된 semantic/API가 없다. body-anchored full-page picture의 기존 rejected path는 사용하지 않았다. 판정: `NOT_SAFE_TO_GENERATE`.
- `artifacts/compatibility-tests/diagnostic/23D-low-resolution-background.hwpx`: 23A와 같은 section XML/기타 패키지 구조를 유지하고 배경 PNG 3개만 `636×900`으로 교체했다. 23A와 압축 해제 ZIP 항목 내용 비교 시 달라진 항목은 BinData/image0~2.png뿐이며 pagePr는 같다.
- 자동 검증: 원본, 23A, 23B, 23D 전부 ZIP 전체 entry 읽기, XML parse, content.hpf manifest/spine refs, section 및 pagePr, background imgBrush/ref/BinData 존재, no body `hp:pic`, internal `HwpxPackageInspector` 2회 재검사에서 PASS. 실제 Hancom 열기/재열기는 하지 않았다.

| 대상 | Sections/논리 페이지 | 본문 hp:t | hp:pic | 배경 | PNG 해상도 | HWPX 크기 | 압축 전 이미지 바이트 | 내부 검사 |
|---|---:|---:|---:|---:|---|---:|---:|---|
| 원본 #23 | 3/3 | 3 | 0 | 3 | 1653×2338 | 62,956 | 119,426 | PASS |
| 23A | 3/3 | 0 | 0 | 3 | 1653×2338 | 66,409 | 119,426 | PASS |
| 23B | 3/3 | 3 | 0 | 3 | 1653×2338 | 62,956 | 119,426 | PASS |
| 23C | 생성 안 함 | — | — | — | — | — | — | `NOT_SAFE_TO_GENERATE` |
| 23D | 3/3 | 0 | 0 | 3 | 636×900 | 30,469 | 37,245 | PASS |

- 보호 원본 #23 SHA-256은 작업 전후 `CEF60905336D3AF0B1E2D308D54D16764B4CBF10DA90D1FAF06E26C1CD6D7DED`, Golden은 `CE7D9151F38FAA90C32C7D7A11C866EE81FA478FC2963A236194A8DDCCF69F4D`로 기존 값과 같다. `HANCOM_3PAGE_MANUAL_REQUIRED` 유지. `FULL_COMPATIBILITY` 선언 금지.
- 원인 가설은 Hancom 수동 비교 전까지 미확정이다. 첫 확인 후보는 H3(이미지 디코딩)다. 원본 세 장은 약 11.59M 픽셀, 23D는 약 1.72M 픽셀로 약 85% 감소한다. 23A와 23D는 PNG만 달라 H3를 비교한다. 23A와 원본/23B의 차이는 section XML뿐이며 본문 테스트 텍스트, lineseg, run 구조가 함께 제거되므로 23A가 빨라도 H1(본문/layout)과 section XML 차이를 완전히 분리하지는 못한다. 세 샘플이 모두 비슷하게 느리면 3-section/background 구조(H2) 또는 Hancom 반응(H4)을 더 조사한다. 내부 검사 PASS는 한컴 호환성/성능 PASS를 의미하지 않는다.
- 다음 단계: Hancom에서 `23D-low-resolution-background.hwpx` → `23A-background-only.hwpx` → `23B-current-clone.hwpx` → 보호 원본 #23 순으로 열어 체감 등급(`FAST`, `SLIGHT_DELAY`, `SLOW`, `VERY_SLOW`, `FREEZE`), 첫 페이지 표시/스크롤/페이지 이동/저장/종료/재열기, 손상 경고, 페이지 수/배경 유지, CPU 지속 여부를 기록한다. 샘플을 저장하지 않는다. C는 생성되지 않아 열지 않는다. 결과에 따라 H1~H4를 좁히고 그 전까지 게이트를 유지한다.
- `git status`: `docs/handoff/CURRENT_HANDOFF.md` 수정, `artifacts/compatibility-tests/diagnostic/` 미추적. 소스와 AGENTS.md 변경은 없다. 산출물과 핸드오프는 커밋하지 않아 교차 기기 동기화는 확인되지 않았다 (`READY_WITH_WARNINGS`).

## Project

- 저장소: PDF2HWP (노트북 PDF-TO-HWP), Codex 담당. Windows x64 WPF / .NET 8, PDF2HWP.sln.
- 현재 지원 출력은 HWPX Visual Fidelity다. HWP 바이너리 출력, OCR, Editable 재구성, Safe Hybrid, PDF 대 결과물 비교는 미구현이다.
- 현재 브랜치는 main이다. 작업 시작 HEAD는 95e0c1505d4a483d96e3b8acbebd7e71a7952d4a였고, blocker 수정 commit은 `fee8e0a16a3011702422819e3400449840ebdaf7`이다. origin/main은 cd09492a0c50d46c8d3e727bad6f8cb050bc8491이며, push는 하지 않았다.

## 라이선스 검증 blocker 수정 (2026-10-12)

- 목표: 라이선스 텍스트의 줄바꿈만 canonical 비교로 허용하고, 바이너리 및 provenance 해시 검증을 그대로 유지한다.
- 검증 로직 변경 파일은 `eng/verify-license-assets.ps1`이다. `Get-RawSha256`와 `Get-CanonicalTextSha256`를 분리했다. canonical 경로는 엄격한 UTF-8로 읽고 CRLF와 CR만 LF로 바꾼 뒤 BOM 없는 UTF-8 바이트로 SHA-256을 계산한다. 공백, 탭, 빈 줄, 문장, 대소문자, Unicode 문자는 정규화하지 않는다. 원시 SHA가 같으면 더 강한 byte-exact 일치로 통과해 비 UTF-8 고지 파일을 변환하지 않는다.
- PDFium 공식 archive, attestation digest, nupkg 내부 PDFium DLL, release/project PDFium DLL은 계속 raw byte SHA-256 exact 검증이다. GitHub attestation 서명 검증과 NuGet 서명 검증도 유지했다.
- 자체 회귀 검사 `LICENSE_TEXT_NORMALIZATION_SELF_TESTS_PASS`: `A\r\nB\r\n` 대 `A\nB\n` canonical 일치, `B` 대 `C` 내용 변경 canonical 불일치, 바이너리 1바이트 변경 raw hash 불일치를 확인했다.
- 공식 PDFium archive의 추출 `abseil.txt` raw SHA-256은 `F54FFF0B905DF5B3464527C652A30E903B172D6DCAB4D89B5E6F105D5E4A4603`, 저장소 보존본 raw SHA-256은 `C79A7FEA0E3CAC04CD43F20E7B648E5A0FF8FA5344E644B0EE09CA1162B62747`이다. 양쪽 canonical SHA-256은 `C79A7FEA0E3CAC04CD43F20E7B648E5A0FF8FA5344E644B0EE09CA1162B62747`로 같다. 공식본은 CRLF 203개, 보존본은 LF 203개다. 다른 PDFium 텍스트 자산의 EOL 차이도 canonical 비교에서 통과했고, 내용 차이는 없다.
- 방금 publish한 산출물을 대상으로 `eng/verify-license-assets.ps1` PASS: `LICENSE_ASSETS_VERIFIED`, PDFium 라이선스 15개, NuGet 서명 검증 7개, PDFium GitHub attestation 검증 성공.
- `eng/verify.ps1` 최종 PASS: `artifacts/verification/20261012-140749`, SDK 10.0.401, Restore 0 / Release Build 0 / Test 0, 122/122 통과, 실패 0, 건너뜀 0. 샌드박스 안의 두 실행은 testhost 연결 시간 초과로 케이스 실행 전 중단됐으나 샌드박스 밖 재실행은 통과했다.
- `eng/publish-win-x64.ps1` PASS (`RuntimeFrameworkVersion=8.0.27`), 산출물 `artifacts/win-x64-singlefile/PDF2HWP.exe`, 187,718,118 bytes. publish 구성을 바꾸지 않았으므로 직전 WPF GUI 스모크 PASS(창 표시, 제목 `PDF2HWP`, 정상 종료)를 재사용했다.
- 보호 샘플 #23 SHA-256 `CEF60905336D3AF0B1E2D308D54D16764B4CBF10DA90D1FAF06E26C1CD6D7DED`, Golden SHA-256 `CE7D9151F38FAA90C32C7D7A11C866EE81FA478FC2963A236194A8DDCCF69F4D`; 모두 추적 상태이며 변함없다. 한컴 수동 검증은 하지 않았고 `HANCOM_3PAGE_MANUAL_REQUIRED`를 유지한다.
- AGENTS.md의 이전 push 검증 상태 diff는 일회성 기록이므로 해당 hunk만 기존 내용으로 되돌렸다. 현재 AGENTS.md diff는 없다. 보호 라이선스 원문, 기능 코드, 의존성은 수정하지 않았다.
- `git diff --check` PASS. 현재 판정은 `PUSH_READY`; 로컬 blocker 수정 commit `fee8e0a16a3011702422819e3400449840ebdaf7`을 만들었고 push하지 않았다.

### 다음 단계

1. 현재 재개 절차는 최신 세션 섹션을 따른다: 동일 Hancom 세션에서 23E → 23F → 23G → 23H → 23I → 23J 순서로 각 파일의 first/warm open을 비교한다. 23D → 23A → 23B → 원본 #23 순서는 위의 과거 진단 계획 기록이며 최신 재개 순서가 아니다. 각 샘플을 저장하지 않으며 `HANCOM_3PAGE_MANUAL_REQUIRED`를 유지한다.
2. 진단 결과로 원인이 좁혀진 뒤 별도 작업에서 수정 방향을 결정한다. push는 사용자 요청 후 진행한다.

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
| 라이선스 blocker 수정 후 main | artifacts/verification/20261012-140749 | 122/122 | PASS, 실패 0, 건너뜀 0 |

다섯 feature worktree와 각 병합 상태에서 git diff --check를 실행해 모두 PASS했다. 보호 파일 변경은 없다.

## 보호 자료 및 검증 게이트

- 보호 샘플 #23: artifacts/compatibility-tests/23-visual-fidelity-a4-portrait-3pages-e2e.hwpx. SHA-256 CEF60905336D3AF0B1E2D308D54D16764B4CBF10DA90D1FAF06E26C1CD6D7DED. 이번 push 직전 검사에서도 기존 값과 일치했다.
- Golden 참조: artifacts/hwpx-diff/20260909-020850/golden-hancom-minimal-reference.hwpx. SHA-256 CE7D9151F38FAA90C32C7D7A11C866EE81FA478FC2963A236194A8DDCCF69F4D. 이번 push 직전 검사에서도 Git 변경 표시가 없고 해시가 기존 값과 일치했다.
- docs/recovery/의 추적 파일은 열람·수정하지 않았다. CompatibilitySampleGenerator는 실행하지 않았다.
- 최신 `eng/verify-license-assets.ps1` 실행은 `LICENSE_ASSETS_VERIFIED`로 통과했다. 이전 `abseil.txt` raw SHA 불일치는 공식본 CRLF와 저장본 LF 차이였으며 canonical 내용은 동일하다. 현재 검증은 이를 명시적으로 진단하고 통과시킨다.

## 남은 확인 및 알려진 문제

- Hancom Office 2024에서 보호 샘플 #23을 실제로 열어 페이지 수와 렌더 결과를 확인하는 수동 검증은 NOT VERIFIED다. 이전 시도는 Computer Use helper 초기화 오류로 중단됐으므로 수동 검증 후 게이트 상태를 별도로 갱신해야 한다.
- 최종 게시 EXE의 WPF GUI 스모크는 창 표시, 제목, 정상 종료까지 확인했다.
- 비정상 프로세스 종료로 임시 작업공간이 남을 수 있으며, 앱 시작 시 marked workspace stale cleanup이 처리하는 구조다. 이번 테스트는 정상 서비스 종료의 finally 정리 경로를 검증했다.
- HWPX inspector의 모든 섹션별 배경 리소스 연결을 깊게 확인하는 범위, UI의 회전/크롭 조작 노출, 추적 진단 파일 run.err/run.out/section18.xml 정리는 이 작업에 포함되지 않았다.
- `HANCOM_3PAGE_MANUAL_REQUIRED`가 남아 있다. 실제 한컴 확인 전까지 `FULL_COMPATIBILITY`를 선언하지 않는다. 라이선스 감사는 현재 통과했고 보호 파일은 변경하지 않았다.

## 통합·라이선스 작업 당시 다음 단계 (과거 기록)

1. 이번 라이선스 검증 수정과 핸드오프를 로컬 main에 commit한다. Git push는 하지 않았다.
2. Computer Use helper 복구 후 샘플 #23을 읽기 전용으로 열고 저장하지 않는다. 열기 전·후 SHA-256을 비교하고 실제 페이지 렌더를 기록한다. 실패하면 HANCOM_3PAGE_MANUAL_REQUIRED를 유지한다.
3. 원격 동기화는 사용자 요청 후 진행한다. 현재 origin/main은 cd09492이며 로컬 main에 후속 브랜치 병합과 문서 동기화가 있다.
4. 이후 별도 작업에서는 HWPX inspector의 섹션별 리소스 검증과 UI에서 회전/크롭 편집을 노출할지 우선순위를 정한다.

## 환경 및 교차 기기 상태

- Windows x64, .NET SDK 10.0.401로 최종 Restore/Release Build/Test를 검증했다. 저장소의 기본 앱 대상은 .NET 8이다.
- PowerShell 7.6.5에서 `eng/verify.ps1`을 실행했다. 샌드박스 안에서는 testhost 연결 시간 초과가 났고 샌드박스 밖 실행에서 통과했다.
- 로컬 main에 blocker 수정 commit `fee8e0a16a3011702422819e3400449840ebdaf7`이 포함됐다. origin/main은 cd09492이며 push하지 않았다. push 검증 상태는 PUSH_READY다. 한컴 수동 검증이 남아 있어 교차 기기 상태는 READY_WITH_WARNINGS다.
- 비밀값이나 .env 내용은 기록하지 않았다.
