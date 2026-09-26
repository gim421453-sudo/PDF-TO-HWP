# PDF2HWP — 1차 기반 구현

Windows x64 / .NET 8 / WPF 기반 PDF→HWPX 변환기의 배포용 아키텍처 초안입니다. 이 단계는 PDF 분석, PDFium 렌더러 연결, 최소 HWPX 패키지 생성 및 구조 검증까지 구현합니다.

## Build and run

```powershell
dotnet restore PDF2HWP.sln
dotnet build PDF2HWP.sln -c Release
dotnet test PDF2HWP.sln -c Release
dotnet run --project src/Pdf2Hwp.App/Pdf2Hwp.App.csproj
dotnet publish src/Pdf2Hwp.App/Pdf2Hwp.App.csproj -p:PublishProfile=win-x64-singlefile
```

## Current boundaries

- `Pdf2Hwp.Core`: UI·엔진·검증·한컴 연동 간 계약과 도메인 모델.
- `Pdf2Hwp.Infrastructure`: PdfPig 분석, PDFium 렌더링 어댑터, ZIP/XML 기반 HWPX writer, package inspector.
- `Pdf2Hwp.App`: 비동기 WPF 선택/drag-drop/취소/진행률 UI.
- `Pdf2Hwp.Tests`: HWPX 패키지 구조 검사 proof test.

`HWP` 저장은 별도 라이선스·설치 조건을 갖는 Hancom adapter 계약으로만 제공하며, 현재는 의도적으로 구현하지 않았습니다. 이 빌드는 한컴 설치나 시스템 설정을 바꾸지 않습니다.

## Dependency review

| Package | Purpose | License | Decision |
|---|---|---|---|
| PdfPig | positioned text/glyph extraction | Apache-2.0 | Adopted |
| PDFtoImage | PDFium page rasterization | MIT | Adopted, renderer serialized |
| PDFium native component | rasterization engine | BSD-style (upstream) | Must retain notices and verify shipped binary provenance |

Avoided: MuPDF/Ghostscript and iText community paths because AGPL creates distribution obligations unsuitable for an unreviewed proprietary deployment.

## Important risks / next work

The HWPX writer supports page geometry, embedded images, and page-background images; compatibility claims remain limited to the documented automated and Hancom-manual results above. Font/table/shape reconstruction, OCR, Safe Hybrid fallback, HWP export, and end-to-end PDF-vs-output rendering comparison remain unsupported or unverified. `PDFtoImage` serializes PDFium calls, so true render parallelism requires safely isolated worker processes or a renderer chosen after benchmarking.

## Current Status

**Project status: WARNING / Pre-release**

### Automated verification

- Restore: PASS
- Build: PASS
- Test: 88 / 88 PASS
- Windows x64 self-contained single-file publish: PASS
- Visual Fidelity regression tests: PASS
- Page-background semantic regression: PASS

### Current verified baseline

- Minimal HWPX text compatibility: PASS
- Embedded PNG/JPEG image compatibility: PASS
- Image physical geometry: PASS
- A4 portrait 1-page Visual Fidelity: Hancom Office 2024 manual PASS
- 3-page Visual Fidelity E2E: Internal automated PASS
- PDF page editor / multi-source workspace: Implemented
- Thumbnail rendering/cache: Implemented
- Undo/Redo: Implemented
- Conversion job / progress / cancellation: Implemented
- Settings / recent files / diagnostics / conversion history: Implemented
- win-x64 single-file EXE publish: Implemented

### Pending validation

- Hancom Office 2024 manual validation of the 3-page Visual Fidelity sample
- Final self-contained EXE startup verification in a Windows Application Control-permitted environment
- Full 100-page thumbnail batch execution
- Raster render dedup integration into the final conversion pipeline
- Additional UI/history retry automation coverage
- Landscape and mixed-orientation Visual Fidelity validation
- OCR / Editable / Safe Hybrid modes
- HWP binary export

### Important

This repository is still under active development.

The current build must **not** be considered:

- `PRODUCTION_READY`
- `FULL_COMPATIBILITY`
- `HANCOM_COMPATIBILITY_PASS`

until the remaining compatibility and manual validation gates are completed.
