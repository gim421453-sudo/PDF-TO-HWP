# Hancom 성능 원인 분리 진단 자산

2026-10-19 사용자의 포맷 대비 보존 승인으로 기존 로컬 진단 파일 17개를 Git에 추가했다. 자동 회귀 fixture나 호환성 PASS 자료로 승격한 것은 아니다. 파일 내용은 변경하지 않았다.

| 파일 | 목적 / 제한 |
|---|---|
| 23A-background-only.hwpx | 고해상도 배경 전용, 3 sections/3 pages |
| 23B-current-clone.hwpx | 보호 원본 #23의 byte-identical 대조본 |
| 23D-low-resolution-background.hwpx | 23A의 배경 PNG만 저해상도로 바꾼 3페이지 대조본 |
| 23E / 23F / 23G | 고해상도 배경 전용 1/2/3페이지. 23G는 23A와 byte-identical |
| 23H / 23I / 23J | 저해상도 배경 전용 1/2/3페이지. 23J는 23D와 byte-identical |
| 23K-background-plus-text-3page.hwpx | 23A의 배경 구조 + 원본의 기존 텍스트 run/lineseg, body hp:pic 없음 |
| image0~2-original.png | 원본 배경 PNG 3장, 1653×2338 |
| image0~2-lowres.png | 저해상도 배경 PNG 3장, 636×900 |
| manual-test/23D-low-resolution-background-manual.hwpx | 이전 저장 테스트의 forensic 자료. 의도치 않은 한글 한 글자가 입력됐을 가능성이 있으며 원본 23D와 byte-identical이 아님. 호환성·성능·저장/재열기 PASS 근거로 사용 금지. 추가 수정/삭제 금지 |

23C single-section 3pages는 검증된 semantic이 없어 생성하지 않았다 (`NOT_SAFE_TO_GENERATE`). 파일이 없는 것이 정상이다.

무결성: 저장소 루트의 `docs/handoff/DIAGNOSTIC_ASSETS.csv`로 17개 원본의 길이와 SHA-256을 확인한다. README 자체는 원본 자산 목록에 포함하지 않는다. 복구/비교 방법과 측정 한계는 `docs/handoff/REFORMAT_RECOVERY.md`를 따른다.

보호 원본 #23과 Golden/reference는 이 폴더의 진단 결과로 대체하지 않는다. `HANCOM_3PAGE_MANUAL_REQUIRED` 유지. `FULL_COMPATIBILITY` 선언 금지. 이번 보존 작업은 샘플을 다시 생성하거나 Hancom에서 저장하지 않았다.
