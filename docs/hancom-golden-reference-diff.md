# Hancom golden-reference comparison

The reference is `artifacts/reference-hancom/hancom-minimal-reference.hwpx` (15,460 bytes), manually saved and reopened by Hancom Office 2024. The failed forensic sample is `artifacts/compatibility-tests/01-minimal-text.hwpx` (1,728 bytes). Neither file is modified by this work.

## ZIP comparison

Golden has 11 entries: `mimetype`, `version.xml`, `Contents/header.xml`, `Contents/section0.xml`, `Preview/PrvText.txt`, `settings.xml`, `Preview/PrvImage.png`, `META-INF/container.rdf`, `Contents/content.hpf`, `META-INF/container.xml`, and `META-INF/manifest.xml`. The failed package has five: `mimetype`, `META-INF/container.xml`, `Contents/content.hpf`, `Contents/header.xml`, and `Contents/section0.xml`.

Golden-only entries are `version.xml`, `settings.xml`, `META-INF/container.rdf`, `META-INF/manifest.xml`, and the two Preview entries. There are no failed-only entries. The five common names use the same exact uncompressed first entry value, `application/hwp+zip`, with no BOM or line ending. Both store `mimetype`; XML entries are deflated.

## Root cause and correction

The failed container uses `application/vnd.hancom.hwpml`; the golden uses `application/hwpml-package+xml`. The failed HPF uses the non-canonical OPF URI without a trailing slash and relative `header.xml`/`section0.xml` targets. Golden uses `http://www.idpf.org/2007/opf/`, package-root `Contents/...` targets, and includes header in the spine. The original inspector incorrectly resolved targets relative to `Contents`, which accepted the failed form.

Golden also has version, settings, RDF, and ODF manifest parts. Its header resolves the section's paragraph, character, and style references. It writes section properties in the first paragraph's first run and represents the five Enter-separated lines as five paragraphs with line-segment arrays. The failed package had skeletal header references and one newline-containing text paragraph.

P0 is package relationship metadata and missing package parts; P1 is referenced header/section structure; P2 is layout/default-style completeness; P3 is the intentionally omitted preview. The new writer emits generic reusable structures and generated IDs; it does not copy golden XML or IDs.

The inspector now verifies the exact first `mimetype`, required parts, canonical HPF references and spine, section root/properties, and writer reference conventions. Paragraph IDs are not globally required to be unique because the Hancom-produced golden reuses them.

`02-minimal-text-fixed.hwpx` is `HANCOM_MANUAL_VALIDATION_REQUIRED` until opened by a user in Hancom Office 2024.

## 02 visual border issue

The user observed horizontal lines between paragraphs and an outer text-area outline in both the Hancom editing view and print preview. The lines remained after Hancom resaved the document, so this was not treated as a transient malformed-XML display artifact. The resaved file was present at `artifacts/compatibility-tests/02-minimal-text-fixed-hancom-resaved.hwpx` and was preserved unchanged.

The resolved chain in both 02 and the golden is `hp:p[@paraPrIDRef=0] → hh:paraPr[@id=0] → hh:border[@borderFillIDRef=2] → hh:borderFill[@id=2]`. Golden border fill 2 explicitly contains `leftBorder`, `rightBorder`, `topBorder`, and `bottomBorder` with `type="NONE"` (and a no-fill brush). The earlier writer emitted an empty borderFill element; that was ambiguous to the Hancom renderer and is the root cause of the unwanted visible borders. The section contains zero table/cell objects.

The writer now defines a reusable explicit `NoBorderNoFill` equivalent: every edge is present with `type="NONE"`, zero-alpha/no-fill brush metadata is present, and the existing paragraph reference chain remains valid. The inspector resolves this chain and raises `HWPX_VISIBLE_PARAGRAPH_BORDER` if any resolved edge is not `NONE`. Sample 03 is generated from this corrected structure.

The Hancom-resaved 02 copy contains five paragraphs but resolves its default paragraph to a border fill whose four edges are `SOLID`; this matches the persistent lines observed by the user. It was read from a temporary copy because the original was locked by another process and was not modified.
