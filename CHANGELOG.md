# PDF2HWP Changelog

Record verified, release-relevant product or operator changes here. No release entries have been verified for this pre-release project yet.

## Unreleased

### Fixed

- Atomically reserve compatibility-sample output directories, reject non-empty explicit output directories, and allow only one concurrent generator to write to a path, protecting tracked sample fixtures from overwrite.
- Clear each conversion-attempt snapshot before validation and after completion so a failed new attempt cannot record pages from an older job.
- Map synchronous result-folder, report-export, and diagnostics errors to visible UI status messages instead of letting them escape the event handlers.
- Continue adding the remaining PDFs after an individual batch-import failure, while preserving cancellation behavior and reporting failed items.
- Remove the selected conversion history entry by matching its persisted value, so JSON reloads do not make deletion silently fail; preserve the generated output file.
- Preserve page pixels for offset, asymmetric PDF CropBoxes by avoiding a second PDFium bounds crop; cover rotations 0/90/180/270 with pixel-level regression tests.
- Preserve validation warnings as `CompletedWithWarnings` in conversion results, history, progress, and the WPF status display instead of presenting them as successful completion.
- Connect the WPF HWPX path to PDFium rendering, job-scoped raster reuse, page-background packaging, and package/reopen validation; preserve PDF rotation and CropBox geometry.
- Prevent publication after validation cancellation, keep retry from mutating the workspace during conversion, preserve corrupt local-state files, and wire settings/recent-file/report interactions.
- Disable unsupported conversion modes and verification controls until their processing paths exist; replace placeholder PDF pipeline assertions with generated-package, rotation/crop, cancellation, and sample-23 checks.

### Changed

- Verify the PDFium release provenance and bundled license notices; include the original dependency/runtime notice bundle beside the single-file EXE and add a reproducible audit gate.
