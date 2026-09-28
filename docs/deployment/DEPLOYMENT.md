# PDF2HWP 배포 및 운영

## 대상 및 현재 배포 상태

- 대상: Windows x64 데스크톱 WPF 앱.
- 배포 형식: self-contained 단일 실행 파일 publish 스크립트가 있음.
- 설치 프로그램, 배포 채널, 호스팅, 운영 환경: 구성되지 않음 / 배포 미확인.

## 개발 및 게시 환경

- 개발 빌드에는 Windows와 .NET 8 SDK가 필요하다.
- `eng/verify.ps1`은 canonical Restore / Build / Test 검증 스크립트다.
- `eng/publish-win-x64.ps1`은 `win-x64`, self-contained, single-file 게시를 수행한다. 기본 출력 위치는 `artifacts/win-x64-singlefile`이다.
- 게시 스크립트는 native library self-extract를 포함하고 trimming을 끈다.
- 스크립트 출력은 `PDF2HWP.exe` single-file과 `THIRD_PARTY_NOTICES.md`, `licenses/third-party/` 원문 고지 자료로 구성된 release package다.
- single-file은 실행 파일의 형식이다. 외부 배포 패키지는 실행 파일 외에 필요한 라이선스/고지 파일을 포함한다.
- `THIRD_PARTY_NOTICES.md`에는 검증한 dependency, PDFium provenance와 번들 구성요소가 기록되어 있다. `eng/verify-license-assets.ps1`은 공식 PDFium attestation, package signatures, hash 일치 및 publish notice 동봉을 재검사한다.
- PDFium 재배포 증거는 현재 resolved dependency 버전에 한정된다. dependency 버전이나 publish runtime이 바뀌면 notice와 audit 검증도 다시 수행해야 한다.

## 설정 및 외부 서비스

- 빌드/앱 실행에 필요한 외부 클라우드 서비스: 확인된 필수 항목 없음.
- 런타임 배포 설정 변수: 확인된 항목 없음.
- 비밀정보는 이 문서에 기록하지 않는다.

## 배포 순서 및 데이터 변경

- 설치/배포 채널과 배포 순서: 아직 정해지지 않음.
- 데이터베이스, 마이그레이션, 도메인 또는 라우팅: 해당 구성 없음.

## 게시 후 검증

- 배포 성공 이력: 확인되지 않음.
- 게시 실행 파일의 최종 시작 검증은 Windows Application Control이 허용되는 환경에서 완료해야 한다.
- HWPX 호환성의 미완료 수동 검증은 `docs/handoff/CURRENT_HANDOFF.md`를 참고한다.

## 롤백 및 운영 제한

- 설치/업데이트 체계가 없어 자동 롤백 절차는 구성되지 않음.
- 프로덕션 배포 완료 또는 전체 한컴 호환성은 주장하지 않는다.
