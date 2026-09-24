# HWPX embedded-image structure

Reference: the Apache-2.0 Hancom `hwpx-owpml-model`, inspected from its `CPictureType` and `CImageType` classes. This is an implementation reference, not a claim that our writer is Hancom-compatible.

An embedded image has three distinct identities:

1. A package binary in `BinData/`.
2. An `opf:item` in `Contents/content.hpf`; its `id` is the binary-resource ID and `href` targets the package binary. Its media type is `image/png` or `image/jpeg` and it is embedded.
3. A per-placement `hp:pic` in `Contents/sectionN.xml`; its nested core `hc:img` uses `binaryItemIDRef` to point to the manifest item ID.

The official model's picture object owns layout children including `sz`, `pos`, `outMargin`, `offset`, `orgSz`, `curSz`, `flip`, `rotationInfo`, `renderingInfo`, `imgRect`, `imgClip`, `effects`, `inMargin`, `imgDim`, and core `img`. `binaryItemIDRef` belongs to the core image type. Placement size is HWPUNIT; PNG/JPEG pixels are only the rendered resource resolution.

Before emitting image XML, retain these invariants: a package path cannot be absolute or contain `..`; IDs are unique by scope; resource bytes are non-empty and MIME/extension agree; every `binaryItemIDRef` resolves to one manifest item and package entry; and resource deduplication never merges placement objects.

The current PDF2HWP writer does **not** yet emit these image objects. It must not label an image-based HWPX result successful until the complete official child sequence has been implemented and manually opened in Hancom.
