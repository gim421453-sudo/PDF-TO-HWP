# PDF2HWP Changelog

Record verified, release-relevant product or operator changes here. No release entries have been verified for this pre-release project yet.

## Unreleased

### Fixed

- Preserve validation warnings as `CompletedWithWarnings` in conversion results, history, progress, and the WPF status display instead of presenting them as successful completion.
- Connect the WPF HWPX path to PDFium rendering, job-scoped raster reuse, page-background packaging, and package/reopen validation; preserve PDF rotation and CropBox geometry.
- Prevent publication after validation cancellation, keep retry from mutating the workspace during conversion, preserve corrupt local-state files, and wire settings/recent-file/report interactions.
- Disable unsupported conversion modes and verification controls until their processing paths exist; replace placeholder PDF pipeline assertions with generated-package, rotation/crop, cancellation, and sample-23 checks.

### Changed

- Verify the PDFium release provenance and bundled license notices; include the original dependency/runtime notice bundle beside the single-file EXE and add a reproducible audit gate.
