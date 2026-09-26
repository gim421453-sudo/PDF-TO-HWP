# Current Project Handoff

Last updated: 2026-09-27 (Asia/Seoul)
Status: WARNING / pre-release development; repository automation files prepared for user-managed commit

## Project

- Project: PDF2HWP, Windows desktop PDF-to-HWPX converter.
- Purpose: preserve PDF appearance while producing the most editable HWPX output possible; HWP binary export is a separate Hancom adapter boundary.
- Repository: solution root (`PDF2HWP.sln`).
- Target: Windows x64, .NET 8, WPF.

## Session objective

Consolidate cross-device handoff and documentation automation at the Git root, remove only verified installer-generated duplicates, initialize generic document templates from verified project facts, and prepare the files for user-managed Git publication without changing features.

## Completed this session

- Inspected root `AGENTS.md`, the project-handoff skill and template, the previous root handoff, and `CHATGPT_PROJECT_INSTRUCTIONS.md`.
- Verified the real solution structure, current UI-to-conversion call, package references/licenses from local package metadata, latest canonical verification artifacts, publish output, fixtures/samples, and Git state.
- Replaced the uninitialized root handoff with this evidence-based continuation state.
- Updated root and component continuation instructions so every session must read/follow the skill, verify actual state, and record synchronization readiness.
- Replaced five duplicate component handoff templates with pointers to the one solution-root canonical handoff.
- No application/source feature code, Golden reference, PDF fixture, or compatibility sample was changed.
- On 2026-09-27, verified this checkout is the solution-root Git repository on `main` at the recorded HEAD; all requested root documentation automation skills/configuration are present and development-log automation is OFF.
- Corrected one stale README risk statement that said page geometry and images were unsupported, despite the current writer and recorded compatibility baseline supporting them. Other documents were left unchanged.
- Consolidated automation under the repository root. Confirmed none of the five source/test/tool component directories is an independent Git repository.
- Removed 5 sets of exact duplicate `.agents` skills (50 files), 5 duplicated root-policy/config/chat instructions, 5 README/CHANGELOG/deployment templates, 5 component handoff pointers, 5 nested AGENTS backup files, and the root timestamped AGENTS backup. Backup content was identical and its handoff rules remain in root `AGENTS.md`.
- Added explicit `HANDOFF_AUTO_UPDATE=true`, repaired the malformed `.DS_Store` ignore entry, and added ignore rules for generated AGENTS backups, local secrets, and generated dependency/build directories.
- Replaced the root CHANGELOG and deployment placeholder templates with verified PDF2HWP-specific content. Kept root README and its real project content.
- No application/source feature code, Golden reference, PDF fixture, or compatibility sample was changed. Development logs remain disabled and untouched.

## Files changed

### Retained root automation and project documents to include in Git

- `AGENTS.md`, `CHATGPT_PROJECT_INSTRUCTIONS.md`, `PROJECT_DOCS.config`
- `.agents/skills/project-handoff/`, `project-docs-manager/`, `project-readme/`, `project-changelog/`, `project-deployment/`, `project-devlog/`
- `docs/handoff/CURRENT_HANDOFF.md`, `docs/deployment/DEPLOYMENT.md`, `CHANGELOG.md`
- Existing `README.md` (tracked; current working copy has the previous verified capability wording correction)
- `.gitignore`

### Deleted verified generated duplicates

- All five nested `.agents/` trees and component-level `AGENTS.md`, `CHATGPT_PROJECT_INSTRUCTIONS.md`, and `PROJECT_DOCS.config` copies.
- Five nested README files proven byte-identical to the project-readme placeholder; five nested CHANGELOG files proven byte-identical to the changelog template; five nested deployment templates and five nested handoff pointers.
- Root and five component `AGENTS.md.backup-20260927-024605` files. Their prior cross-device policy content is retained in the root `AGENTS.md`; no unique component rules were found.
- No source files, existing root README, tracked compatibility artifacts, or third-party documentation were deleted. No Git operations performed.

## Decisions / architecture

- `docs/handoff/CURRENT_HANDOFF.md` at the solution root is the only changing project handoff. Non-independent component directories inherit root rules and contain no duplicate handoff/docs installation.
- Session startup and completion both explicitly invoke `.agents/skills/project-handoff/SKILL.md`; handoff claims must be checked against current state. `PROJECT_DOCS.config` explicitly enables README, CHANGELOG, DEPLOYMENT and HANDOFF management while leaving DEVLOG disabled.
- Readiness must account for whether the handoff/rules are tracked or otherwise known to sync. The user owns Git add/commit/push/pull/remote operations.
- Core contains domain/contracts; Infrastructure contains PDF analysis/rendering, conversion orchestration and HWPX writing/inspection; App is WPF UI; Tests is xUnit; CompatibilitySampleGenerator creates package test artifacts. HWP is an adapter boundary only.
- Main dependencies observed: PdfPig 0.1.15 (Apache-2.0) and PDFtoImage 5.4.0 (MIT; confirmed from local NuGet nuspec); PDFium native distribution notices/provenance remain a packaging concern. Tests use xUnit 2.9.2 and Microsoft.NET.Test.Sdk 17.11.1.
- Documentation is imprecise: root `README.md` calls PDFtoImage MIT, while `THIRD_PARTY_NOTICES.md` groups “PDFtoImage / PDFium” under “BSD-3-Clause / PDFium notices.” NuGet metadata confirms PDFtoImage itself is MIT; separate its notice from PDFium/native notices and verify the bundled native binary provenance before distribution.

## Current implementation state

- Async WPF PDF selection/drop, workspace page list and editing operations (reorder/remove/duplicate/include/exclude), undo/redo, lazy viewport thumbnails, cancellation/progress, settings/recent/history/diagnostics, and output collision/atomic temporary-file handling are present.
- Infrastructure includes positioned PDF text analysis (PdfPig), PDFium rendering, HWPX package writer/inspector, image/page-background writing, Visual Fidelity converter and conversion orchestration.
- Compatibility history includes minimal text, PNG/JPEG image and geometry checks, A4 portrait single-page page-background manual pass, and internal 3-page Visual Fidelity E2E sample.
- Important UI limitation verified in `MainWindow.xaml` / `MainWindow.xaml.cs`: Safe Hybrid/Visual Fidelity/Editable, OCR Auto, and source comparison controls are displayed, but the conversion call currently forwards only the workspace snapshot, output directory/name, render quality, progress and cancellation. These selected options are not forwarded to `ConversionJobService`; the service currently analyzes PDFs, writes HWPX, and performs package inspection. Do not interpret UI selections or package PASS as OCR/mode dispatch or visual comparison validation.
- HWP binary export, OCR, actual Safe Hybrid/Editable dispatch, and complete rendered PDF-vs-output comparison are not implemented/verified. Do not claim production readiness or full compatibility.

## Verification

- Canonical `eng/verify.ps1`: last run in this session on 2026-09-26; exit code 0. Artifact: `artifacts/verification/20260926-021620/`.
- Verified summary: Restore 0, Build 0, Test 0, PASS. TRX counters: 88 total, 88 passed, 0 failed, 0 skipped. SDK used: 10.0.300; target projects include .NET 8.
- Documentation/bootstrap cleanup on 2026-09-27: required root files/skills present; no nested independent repositories; `git diff --check` PASS; 16 untracked root automation/documentation files, 2 tracked modifications, 0 staged files. `git check-ignore` confirmed the shared policy files remain trackable and representative `.env`/credential, `bin`, `obj`, `node_modules`, `dist`, `target`, verification, publish, and scratch artifacts remain ignored. Build/tests NOT RUN because changes are limited to documentation, automation rules, `.gitignore`, and removal of verified duplicate templates.
- No code was modified after this verification; only handoff/instruction markdown changed afterward. Verification was not rerun after those documentation-only edits.
- Most recent inspected single-file publish: `artifacts/win-x64-singlefile-20260924-dist-final/PDF2HWP.exe`, 187,680,742 bytes (timestamp 2026-09-24). Current-session launch smoke: NOT RUN. README reports a prior latest self-contained startup attempt blocked by Windows Application Control; final launch verification still pending in a permitted environment.
- Hancom Office 2024 availability in this session: NOT VERIFIED. The documented A4 portrait single-page visual sample manual result and older compatibility results are repository-recorded; the 3-page Visual Fidelity manual validation remains pending.
- Rules/security-specific test suite: NOT FOUND / NOT RUN separately. The canonical xUnit suite passed.

## Known issues / blockers

- UI mode/OCR/visual-verification selections are not wired into the current conversion path; this can mislead users and should be the first implementation/guard task.
- HWP binary export and OCR unavailable; full editable conversion and safe hybrid fallback remain incomplete.
- Complete PDF-vs-result visual comparison is not implemented in the active job path; current package validation is not equivalent to visual validation.
- Full 100-page preview batch execution remains pending despite an existing stress fixture. Raster render dedup/cache integration into actual conversion, more UI/history retry automation, and landscape/mixed-orientation Visual Fidelity validation are pending.
- 3-page Visual Fidelity sample #23 has an internal automated E2E pass but still needs Hancom Office 2024 manual open/save/reopen validation.
- Final published executable startup verification remains pending due to a previously reported Windows Application Control block.
- Licensing notice discrepancy described above needs resolution before distribution.
- Git-only synchronization is pending user publication of the prepared root automation/documentation files.

## Do not repeat

- Do not recreate per-component automation trees, documentation configs, README templates, or handoff/deployment folders; those directories follow this root policy and root handoff.
- Do not regenerate/overwrite Golden references or compatibility sample #23. The existing 3-page sample is `artifacts/compatibility-tests/23-visual-fidelity-a4-portrait-3pages-e2e.hwpx` (62,956 bytes); it is the pending manual-validation candidate.
- Do not recreate the 100-page stress PDF unless it is missing: `artifacts/pdf-fixtures/stress-a4-portrait-100pages.pdf` is the existing fixture.
- Do not redo completed minimal HWPX/image/page-background structural and compatibility work without a new failing result.
- Do not treat package structural PASS as a substitute for Hancom manual compatibility or page-image comparison.

## Exact next action

Review the final root-only `git status` and diff, then user-managed `git add`/commit/push may synchronize the prepared automation files. After synchronization, resume the implementation task below: inspect `src/Pdf2Hwp.App/MainWindow.xaml` and `MainWindow.xaml.cs`, `src/Pdf2Hwp.Infrastructure/ConversionJobService.cs`, and tests; make mode/OCR/verification UI truthful by wiring supported contracts or rejecting unsupported selections, add tests, and run `eng/verify.ps1`.

## Additional next actions

1. Implement/document capability-aware dispatch for Safe Hybrid, Visual Fidelity, Editable and OCR only after the contracts and available engine capabilities are clear.
2. Add actual source-PDF vs rendered-output validation and explicit PASS/WARNING/FAIL semantics to conversion status.
3. Integrate render dedup/cache into the actual conversion pipeline and execute the 100-page stress fixture with memory/progress/cancellation measurements.
4. Add UI/history retry and landscape/mixed-orientation regression coverage.
5. On a Windows Application Control-permitted environment, run the single-file EXE smoke test and manually validate the 3-page sample in Hancom Office 2024; record observed results without upgrading claims before evidence.
6. Reconcile `THIRD_PARTY_NOTICES.md` with package and native PDFium licensing/provenance before distribution.

## Environment / configuration

- Required local environment: Windows x64, .NET SDK 8.x (installed SDKs observed: 8.0.425 and 10.0.300), WPF-capable .NET 8 targeting/build setup, NuGet package restore.
- Canonical verification uses `eng/verify.ps1`; publish script is `eng/publish-win-x64.ps1`.
- External cloud services: none identified as required for build/verification.
- Hancom Office 2024 is only needed for manual compatibility checks; installed/available state here: NOT VERIFIED.
- No secrets or secret values recorded.

## Git / workspace state

- Branch: `main` (the one observed commit message is `chore: establish PDF2HWP pre-release baseline`).
- HEAD: `88d763d46d7b5671a7363cca5fd12313b9595f5a` (`chore: establish PDF2HWP pre-release baseline`, 2026-09-24).
- Upstream status reports `main...origin/main`; remote URL was NOT INSPECTED.
- Tracked working tree: exactly `README.md` (verified capability wording correction from the notebook check) and `.gitignore` (this cleanup) are modified; no source code is modified. There are 16 untracked files, all intended root automation/documentation files, and no nested duplicates or build outputs. No Git mutation was performed.
- Current branch remains `main` at `88d763d46d7b5671a7363cca5fd12313b9595f5a`; status shows `main...origin/main`. No remote fetch was performed, so remote server freshness is NOT VERIFIED.
- Root automation/documentation files remain untracked until the user stages and commits them; `.gitignore` and `README.md` are tracked modifications. Nothing was staged or committed.

## Cross-device readiness

READY_WITH_WARNINGS

Reason: the root-only automation/documentation set is ready for Git review/commit; cross-device synchronization will occur only after the user commits and syncs it. No Git write operation was performed.
