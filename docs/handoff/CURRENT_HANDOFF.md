# Current Project Handoff

Last updated: 2026-09-28 (Asia/Seoul)
Status: LICENSE_COMPLIANCE_VERIFIED / COMMITTED; pre-release development

## Project and architecture

- PDF2HWP is a Windows x64 WPF desktop application targeting .NET 8 for PDF-to-HWPX conversion; HWP binary output remains a separate Hancom adapter boundary.
- Solution: `PDF2HWP.sln`. Core defines contracts/domain; Infrastructure contains PdfPig analysis, PDFtoImage/PDFium rendering, conversion orchestration, HWPX writing/inspection; App is WPF; Tests is xUnit.
- The supported UI path is Visual Fidelity. Safe Hybrid, Editable, OCR and PDF-vs-output visual comparison remain unsupported and are disabled/labeled in the UI.

## Current session

Objective: audit actual redistributed dependencies and notices, verify PDFium provenance, rerun canonical validation and publish, and commit the verified work. Functional source changes from the prior repair were preserved; this session made no feature-code changes.

- Baseline branch/HEAD: `main` / `dcea1905f2b4eb17bb3e45ec4758519fe9ba1371`.
- WPF production conversion now calls `VisualFidelityConverter`, job-scoped raster cache and HWPX page-background resources; unsupported mode/verification controls are disabled.
- PDF MediaBox/CropBox/rotation are preserved through analysis, workspace and PDFium raster rendering. Cancellation is checked after validation/reopen and before final non-overwriting publish.
- Retry/workspace reentry, preview shared-cancellation, re-add-after-empty, report path association, settings/recent actions, corrupt-state preservation and tautological pipeline tests were addressed with regression coverage.
- `docs/recovery/` forensic records are pre-existing untracked user data and remain untouched. Golden files and sample #23 remain unchanged.

## Redistribution audit (2026-09-28)

- Verified chain: `Pdf2Hwp.Infrastructure` direct reference `PDFtoImage 5.4.0` -> transitive `bblanchon.PDFium.Win32 152.0.7961` -> `runtimes/win-x64/native/pdfium.dll`. NuGet, official release archive, and project Release DLL SHA-256 all match (`D3D9F4B7C9DABE3363F30779C5C3C715C47332749FA590E4B4A2B8B6780CB1C4`). Release archive hash (`88276459349B291C41F10422DAD0210F007C04D919C8FA56472B6B7C6406ADF4`) matches its GitHub attestation subject; `gh attestation verify` passed for `bblanchon/pdfium-binaries` and the build-all workflow.
- Official package metadata: PDFtoImage 5.4.0 MIT; bblanchon.PDFium.Win32 152.0.7961 Apache-2.0 package expression. The exact release archive's distributor MIT license, PDFium upstream terms and all 15 bundled component license/notice files are preserved as original texts under `licenses/third-party/`. PdfPig, SkiaSharp and the .NET/WPF runtime package notices are also included. Seven package/runtime package signatures passed `dotnet nuget verify --all`.
- `THIRD_PARTY_NOTICES.md` records dependency tiers, exact versions, sources, hashes, attestation, and release packaging. `eng/verify-license-assets.ps1` checks resolved versions/assets, signature verification, hash/provenance/notice identity, and the published notice bundle. Result: `LICENSE_ASSETS_VERIFIED`.
- `eng/publish-win-x64.ps1` retains the win-x64 self-contained single-file EXE and creates a release package with `THIRD_PARTY_NOTICES.md` and `licenses/third-party/`. The audited release directory contains these materials; redistribution without them is not permitted by this project's packaging policy.
- PID 41464 was not terminated. It was absent when checked; retain `ENVIRONMENT_TEST_RESIDUE` as historical classification, not a source defect.
- PID 41464 was not terminated. It was not present at the final process check; retain its classification as `ENVIRONMENT_TEST_RESIDUE`, not a source defect. This check does not imply a force-stop.

## Verification

- `eng/verify.ps1`: PASS; Restore 0 / Release Build 0 / Test 0. Latest run: `artifacts/verification/20260928-205457` (SDK `10.0.300`).
- TRX: 106 / 106 passed, 0 failed, 0 skipped or not-executed; no test reduction.
- `git diff --check`: PASS (Git reports only LF-to-CRLF normalization warnings).
- Temporary targeted 100-page thumbnail batch: PASS, 100/100 distinct pages rendered at the 300px long-edge policy with concurrency capped at 2; temporary test source removed afterward.
- `eng/publish-win-x64.ps1`: PASS; `artifacts/win-x64-singlefile/PDF2HWP.exe`, 187,706,854 bytes, plus the notice and original-license bundle. `eng/verify-license-assets.ps1`: PASS, seven signed NuGet packages/runtime packs and all 15 PDFium component files verified. Final visible WPF main-window startup and graceful-close smoke had passed in the prior verification; not rerun during this license-only session.
- A separate hidden-window smoke harness did not expose a top-level window and left its test process running; that invocation is not counted as a production-path PASS. It was not force-terminated.
- Canonical tests exercise 100-page thumbnails, crop/rotation pixels, duplicate raster reuse, multi-source order, validation cancellation and package reopen. Sample #23 is inspected read-only and remains under `HANCOM_3PAGE_MANUAL_REQUIRED`.
- The verifier reported SDK `10.0.300`; projects target .NET 8. Confirm an SDK 8-only environment separately if required for release qualification.

## Integrity findings (2026-09-28)

- Prior HIGH/MEDIUM/LOW functional findings are fixed and covered by the passing suite. Unsupported OCR/Editable/Safe Hybrid/visual comparison features remain explicit non-capabilities rather than selectable options.

## Known limitations / pending work

- Hancom Office manual validation of the 3-page Visual Fidelity sample #23 remains pending; do not overwrite it or the Golden references.
- Hancom Office 2024 manual validation of 3-page sample #23 remains pending; preserve `HANCOM_3PAGE_MANUAL_REQUIRED`. It is not a license or commit blocker.
- `ENVIRONMENT_TEST_RESIDUE`: PID 41464 was previously reported as the hidden smoke harness residue; it was not force-terminated and was absent at the latest check. It is not classified as a source defect.
- Hancom Office 2024 manual validation of 3-page sample #23 remains required; do not modify the sample or Golden references.
- OCR, Editable reconstruction, Safe Hybrid fallback, PDF-vs-output rendering comparison and HWP binary export remain unimplemented by scope.

## Exact next action

On the next session, first verify this commit is present after the user's Git synchronization (`main` should contain the final compliance commit); then request and record the Hancom Office 2024 manual validation of existing sample #23 without modifying the sample or Golden references. Do not repeat the dependency provenance/signature audit unless resolved packages or publish inputs change.

## Environment and cross-device readiness

- Required: Windows x64, .NET SDK/runtime supporting the net8.0 projects, NuGet restore; Hancom Office 2024 only for manual compatibility checks.
- External services: none identified as required for local build/test.
- Secrets: none recorded.
- Current synchronization readiness: `READY_WITH_WARNINGS`; the verified repair is committed locally but has not been pushed, and pre-existing `docs/recovery/` remains untracked and untouched. Cross-device availability depends on user Git synchronization.

## Git state

- Branch: `main`; pre-task HEAD: `dcea1905f2b4eb17bb3e45ec4758519fe9ba1371`; production repair commit: `cd7ced419d23c00910c77456a30b3284cb089491`.
- Expected preserved tracked changes: `src/Pdf2Hwp.App/MainWindow.xaml.cs`, `src/Pdf2Hwp.Core/ApplicationContracts.cs`, `src/Pdf2Hwp.Infrastructure/ConversionJobService.cs`, `tests/Pdf2Hwp.Tests/ConversionJobServiceTests.cs`.
- The verified repair, audit, license texts, docs, tests and publish/audit scripts are committed on `main`; push was not run. Pre-existing `docs/recovery/` remains untracked and untouched. No reset/clean/stash/pull/push/fetch/branch operation was run.
