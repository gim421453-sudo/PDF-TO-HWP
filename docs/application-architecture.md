# PDF2HWP application architecture

`DocumentWorkspace` owns source documents and the logical `PageSequence`.
Each `DocumentPage` has a stable identity, source document/page reference,
logical output index, inclusion flag, rotation and operation history. Reorder,
remove, duplicate and selection operations mutate only the logical sequence;
source PDFs are never modified.

Conversion jobs consume an immutable page snapshot and expose status/progress,
cancellation and atomic output. `TempWorkspaceManager` isolates intermediate
files below `%TEMP%\\PDF2HWP\\<job-id>`, while `PreviewThumbnailCache` is kept
separate from full-resolution Visual Fidelity renders. Settings and recent
files are JSON-only and contain paths/timestamps, never document contents.

The validated one-page page-background representation remains unchanged. The
three-page sample remains pending Hancom manual validation.

## Desktop application integration

`MainWindow` is a thin WPF shell around `DocumentWorkspace` and
`ConversionJobService`. PDF files are analyzed asynchronously and become
logical page cards. The list uses WPF virtualization, extended selection,
keyboard shortcuts, a context menu, include/exclude state, duplicate/remove
actions and drag reorder. The source PDF is immutable; only the workspace
sequence changes.

`ConversionJobService` receives an immutable page snapshot, reports
analyze/write/validate progress, observes cancellation, writes to a partial
path and promotes only a validated package. Output directory checks happen
before work begins. HWP remains an explicit adapter boundary and is not
silently treated as HWPX.

Settings and recent files are stored under the user's LocalApplicationData
folder. Conversion reports and diagnostics are explicit JSON exports. Startup
cleanup removes only marked, stale `%TEMP%\\PDF2HWP` workspaces. The self-
contained Windows publish contract is `artifacts\\win-x64-singlefile\\PDF2HWP.exe`.

Thumbnail rendering is lazy and viewport-driven: the page editor requests
visible page cards only, keeps independent cancellation per page, and uses a
bounded PDFium worker gate without loading a document's raster pages at once.

The current preview path now uses PDFium at a 300px long-edge policy, a
separate `%LOCALAPPDATA%\\PDF2HWP\\Cache\\Preview` cache, SHA-256 source
identity, page/size/version keys, atomic cache writes and a bounded two-worker
gate. Selection changes request previews asynchronously and failed pages keep
their placeholder state.

Conversion receives the exact immutable logical snapshot. It analyzes each
distinct source once, maps selected pages into logical `PdfPageInfo` order,
omits excluded pages and preserves duplicates as separate output sections.
The current HWPX writer path does not rasterize pages, so conversion reports
do not claim raster reuse. `JobRenderCache` is a separate job-scoped PDFium
single-flight boundary for future raster stages; its key includes source SHA,
source page, quality, rotation and crop, and failures/cancellation are evicted.
This cache is not HWPX embedded-resource deduplication.

`WorkspaceHistory` provides bounded snapshot-based Undo/Redo while preserving
stable page IDs. Conversion history is capped at 20 metadata-only entries and
supports output/report/folder actions plus retry when the source page metadata
is available. Settings, recent-files and history JSON use per-path named
mutexes and temporary-file replacement, including locked history read-modify-
write updates. Active temporary workspaces carry a marker so startup cleanup
skips workspaces owned by a live process and removes only old, app-marked
directories whose owner process has exited.

Preview cache entries are SHA-256 keyed by source/page/thumbnail policy,
limited to 500 MB with least-recently-used cleanup, and protected by per-entry
process and file leases. Cleanup and Clear skip active entries; partial files
are removed only after the stale threshold. Output publication uses a unique
partial file followed by a non-overwriting atomic move, so simultaneous jobs
cannot replace an existing result. The user-provided output stem is honored
and collision suffixes are selected before publication, with the final move
remaining authoritative against races.

The synthetic 100-page A4 fixture at
`artifacts\pdf-fixtures\stress-a4-portrait-100pages.pdf` is exercised through
PdfPig metadata checks, representative real PDFium renders, page workspace
operations and job-render-cache deduplication. Conversion progress currently
reports analysis by source and the single HWPX write/validation stage; the
writer interface does not expose per-page write progress yet.
