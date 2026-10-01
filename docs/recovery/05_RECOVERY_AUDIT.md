# PDF2HWP 변경 무결성 감사

기준 커밋: `dcea1905f2b4eb17bb3e45ec4758519fe9ba1371` (`main`)
저장소: `C:/dev/Hwp` only. 외부 저장소/상위 폴더는 열지 않았다.
감사 범위: 기준 HEAD 이후 초기 `git diff HEAD`의 추적 변경과 초기 미추적 파일 전부.

## 분류 기준 및 결론

- **VALID_AND_VERIFIED (4):** PDF2HWP의 validator 경고가 PASS처럼 보이지 않도록 수정한 정상 목적 변경. canonical Restore/Build/Test 및 관련 회귀 테스트가 통과했다.
- **GENERATED_NOISE (6):** 이전 저장소 내 .NET 격리 시도에서 만들어진 `.keep` 파일. 기능 영향 없음. 삭제하지 않고 기록하며 좁은 ignore 규칙을 추가했다.
- **SUSPICIOUS / FOREIGN_PROJECT_CONTAMINATION / REGRESSION:** 변경 diff와 해당 기능 계약을 비교한 결과 0개 확인. 전체 canonical suite도 통과했으나 WPF runtime/manual compatibility는 이번 감사 범위에서 실행하지 않았다.

## 파일별 감사

| File | Git state / HEAD | Classification | Evidence and role | Risk / planned action / verification |
|---|---|---|---|---|
| `src/Pdf2Hwp.App/MainWindow.xaml.cs` | M, HEAD 파일 있음, +13/-2 | VALID_AND_VERIFIED | `ValidationStatus.Pass/Warning/Fail`을 history status 및 Korean UI 상태에 명시적으로 대응한다. | Release build 및 canonical test 통과. UI 직접 실행은 NOT RUN. |
| `src/Pdf2Hwp.Core/ApplicationContracts.cs` | M, HEAD 파일 있음, +2/-2 | VALID_AND_VERIFIED | `CompletedWithWarnings`를 enum 끝에 추가하고 history label을 경고로 표시한다. 기존 `Cancelled=6`, `Failed=7`의 수치를 보존한다. | 회귀 테스트 및 전체 canonical test 통과. |
| `src/Pdf2Hwp.Infrastructure/ConversionJobService.cs` | M, HEAD 파일 있음, +6/-2 | VALID_AND_VERIFIED | 임시 output 검사 결과를 모아 aggregate하고 warning issue를 보고/progress에 전달한다. FAIL은 `File.Move` 전 throw하는 기존 순서를 보존한다. | 관련 경고 경로 및 전체 canonical test 통과. |
| `tests/Pdf2Hwp.Tests/ConversionJobServiceTests.cs` | M, HEAD 파일 있음, +17/-0 | VALID_AND_VERIFIED | warning report/message/progress/history 및 enum numeric compatibility 확인. | 새 경고 테스트와 기존 동시 설정 저장 테스트 포함 89/89 PASS. |
| `.local-verification-20260927/.keep` | ??, HEAD 없음, 17 bytes | GENERATED_NOISE | 격리 검증용 scratch 표식만 포함. | 기능 영향 없음. 보존; `.gitignore`에서 제외. |
| `.local-verification-20260927/canonical/.keep` | ??, HEAD 없음, 42 bytes | GENERATED_NOISE | 결과 출력 디렉터리 표식. | 기능 영향 없음. 자동 삭제 금지; 위와 동일. |
| `.local-verification-20260927/cli-home/.dotnet/tools/.keep` | ??, HEAD 없음, 45 bytes | GENERATED_NOISE | 로컬 CLI home 폴더 표식. | 기능 영향 없음. 자동 삭제 금지; 위와 동일. |
| `.local-verification-20260927/nuget-http-cache/.keep` | ??, HEAD 없음, 39 bytes | GENERATED_NOISE | 로컬 HTTP cache 폴더 표식. | 기능 영향 없음. 자동 삭제 금지; 위와 동일. |
| `.local-verification-20260927/nuget-packages/.keep` | ??, HEAD 없음, 42 bytes | GENERATED_NOISE | 로컬 package cache 폴더 표식. | 기능 영향 없음. 자동 삭제 금지; 위와 동일. |
| `.local-verification-20260927/nuget-plugins-cache/.keep` | ??, HEAD 없음, 41 bytes | GENERATED_NOISE | 로컬 plugin cache 폴더 표식. | 기능 영향 없음. 자동 삭제 금지; 위와 동일. |

## 구조 영향 검사

- 초기 `git diff HEAD` 변경 경로는 4개 구현/테스트 파일뿐이다. `.sln`, `.csproj`, dependency/package 설정, publish 설정, README/docs/rules, HWPX writer/inspector, Golden, compatibility sample 23은 변경되지 않았다.
- 추가/삭제 dependency 없음. Target framework, WPF/x64, writer semantic, workspace/source identity, cancellation/output publish 정책은 이 diff에서 변경되지 않았다.
- 추가 테스트는 assertion을 삭제/skip하지 않으며 validation warning propagation을 강화한다.
- 후속 정적 검색 및 Golden/sample 확인 결과는 `07_FINAL_INTEGRITY_REPORT.md`에 기록한다.
