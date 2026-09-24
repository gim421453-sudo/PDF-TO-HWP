# HWPX compatibility samples

- `01-minimal-text.hwpx` — forensic artifact; Hancom reported `파일이 손상되었습니다.` (`HANCOM_OPEN_FAIL`).
- `02-minimal-text-fixed.hwpx` — Hancom open/text structure passed, but visual structure failed with confirmed unwanted lines (`HANCOM_OPEN_PASS`, `VISUAL_STRUCTURE_FAIL`).
- `03-minimal-text-no-borders.hwpx` — regenerated with explicit no-border definitions.
- `03-minimal-text-no-borders.hwpx` — Hancom open/text/no-border passed, but orientation failed (`NARROWLY` with portrait dimensions; `PAGE_ORIENTATION_FAIL`).
- `04-minimal-text-a4-portrait.hwpx` — corrected `WIDELY` portrait semantics; manual validation required.
- `05-image-png-basic.hwpx` — PNG binary/placement sample; internal validation PASS, Hancom manual validation required.
- `06-image-jpeg-basic.hwpx` — JPEG binary/placement sample; internal validation PASS, Hancom manual validation required.
- `05-image-png-basic.hwpx` and `06-image-jpeg-basic.hwpx` — forensic artifacts; Hancom process crash confirmed (`HANCOM_PROCESS_CRASH`, `IMAGE_COMPATIBILITY_FAIL`).
- `07-image-png-fixed.hwpx` — corrected OWPML picture sequence; internal validation PASS, manual validation required.
- `08-image-jpeg-fixed.hwpx` — corrected OWPML picture sequence; internal validation PASS, manual validation required.
- `07-image-png-fixed.hwpx` / `08-image-jpeg-fixed.hwpx` — embedded flag missing; validator now reports `HWPX_IMAGE_NOT_EMBEDDED` and these remain forensic artifacts.
- `09-image-png-embedded-fixed.hwpx` — `isEmbeded="1"` embedded PNG sample.
- `10-image-jpeg-embedded-fixed.hwpx` — `isEmbeded="1"`, Hancom-compatible `image/jpg` JPEG sample.
- `09-image-png-embedded-fixed.hwpx` / `10-image-jpeg-embedded-fixed.hwpx` — embedding passes internally, but the 1×1 source was deliberately placed at 2:1 and is not a valid aspect-ratio acceptance fixture (`ASPECT_RATIO_TEST_INADEQUATE / PLACEMENT_STRETCHED`).
- `11-image-png-800x400-aspect.hwpx` — diagnostic 800×400 PNG, PreserveAspectRatio placement (100 mm × 50 mm); internal/reopen validation PASS; Hancom manual validation required.
- `12-image-jpeg-800x400-aspect.hwpx` — diagnostic 800×400 JPEG with the same geometry policy; internal/reopen validation PASS; Hancom manual validation required.
- `13-image-png-100x50mm-fixed.hwpx` — object-unit corrected 100×50 mm PNG; internal/reopen validation PASS; Hancom manual size confirmation required.
- `14-image-jpeg-100x50mm-fixed.hwpx` — object-unit corrected 100×50 mm JPEG; internal/reopen validation PASS; Hancom manual size confirmation required.
- `15-image-png-100x50mm-golden-fixed.hwpx` — Golden-derived picture geometry; internal/reopen validation PASS; PNG must be manually checked in Hancom before physical PASS.
- `16-image-jpeg-100x50mm-golden-fixed.hwpx` — Golden-derived picture geometry; internal/reopen validation PASS; JPEG manual validation remains blocked until PNG is confirmed.

The requested `artifacts/reference-hancom/hancom-image-100x50mm-reference.hwpx` is not present yet; no synthetic Golden was substituted.

Diagnostic sources: `C:\dev\Hwp\artifacts\image-fixtures\diagnostic-800x400.png` and `diagnostic-800x400.jpg`.

Geometry acceptance requires source ratio 2:1 and HWPUNIT placement ratio 2:1 within 0.1%. Visual Fidelity remains intentionally unimplemented.

Golden references are available at `C:\dev\Hwp\artifacts\reference-hancom\hancom-image-png-reference.hwpx` and `C:\dev\Hwp\artifacts\reference-hancom\hancom-image-jpeg-reference.hwpx`. Semantic comparison found no remaining P0/P1 structure difference for 07/08.

Expected for 03: one A4 portrait page, five logical paragraphs, no table/image/shape, and no visible paragraph borders.

Manual status: `04/09/10 = HANCOM_MANUAL_VALIDATION_REQUIRED`; `05/06 = HANCOM_PROCESS_CRASH`; `07/08 = HANCOM_IMAGE_PATH_DIALOG`

Visual Fidelity pagination investigation: `19` and `20a`~`20e` remain regression
artifacts; Hancom 2024 produced a second blank page for every body full-page
variant. No `21` sample was generated because a page-background Golden is absent.
Status: `PAGE_BACKGROUND_GOLDEN_REQUIRED` (see
`docs/visual-fidelity-page-background-investigation.md`).

`22-visual-fidelity-a4-portrait-3pages.hwpx`: multi-section target with three
page-background resources (`image0`, `image1`, `image2`) and no body full-page
picture. Internal package/reopen inspection PASS; Hancom manual validation is
required.

`23-visual-fidelity-a4-portrait-3pages-e2e.hwpx`: 실제 3-page PDF를 PDFium
200 DPI로 렌더한 E2E sample. 렌더는 `23-page1/2/3-render-200dpi.png`이며
각각 1653×2338 px, 서로 다른 SHA-256이다. 3 sections와 3 background
resource의 내부/reopen validator는 PASS. Hancom 2024 수동 검증 전에는
`HANCOM_3PAGE_MANUAL_REQUIRED` 상태다.

`21-visual-fidelity-a4-page-background.hwpx` was generated from the existing
writer package using semantic `hh:borderFill` id 3 + `hc:imgBrush`/`hc:img`
(`binaryItemIDRef=image0`) and `hp:pageBorderFill` BOTH reference. It contains
no body `hp:pic`; pagePr remains A4 portrait. Internal package/reopen inspection
is required before Hancom manual validation.
