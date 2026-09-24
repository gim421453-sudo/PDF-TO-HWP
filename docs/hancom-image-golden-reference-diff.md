# Image HWPX crash forensic report

Hancom Office 2024 terminated while opening both original samples `05-image-png-basic.hwpx` and `06-image-jpeg-basic.hwpx`, although the prior inspector reported PASS. They remain preserved as forensic artifacts. Windows read-only event log inspection found Application Error / WER events for `hwp.exe` 13.0.0.3066, faulting module `HwpApp.dll`, exception `0xc0000005`, offset `0x00260e9c`.

The embedded source binaries were extracted without re-encoding:

- `artifacts/reference-hancom/image-source/05-source.png`: 68 bytes, PNG signature, 1x1, SHA-256 `431CED6916A2A21A156E38701AFE55BBD7F88969FBBFC56D7FE099D47F265460`.
- `artifacts/reference-hancom/image-source/06-source.jpg`: 404 bytes, JPEG signature, 1x1, SHA-256 `A8A9E0FD3047A7608AD323BAF18288610A6A52C8E6AB755ABD7840AA53B2292E`.

No Hancom image golden reference exists yet. Instructions are in `artifacts/reference-hancom/IMAGE-GOLDEN-INSTRUCTIONS.txt`.

The references were subsequently found under `artifacts/compatibility-tests/` and copied, without modification, to the requested `artifacts/reference-hancom/` paths. PNG golden is 35,632 bytes and JPEG golden is 33,932 bytes. Both contain the same core package parts as 07/08 plus `Preview/PrvImage.png` and `Preview/PrvText.txt`; JPEG golden stores `BinData/image1.JPG` while the writer uses `BinData/image0.jpg`.

## Root cause found from the public OWPML model

`CPictureType::InitMap` defines the sequence `sz, pos, outMargin, caption/shapeComment/parameterset/metaTag, offset, orgSz, curSz, flip/rotationInfo/renderingInfo, lineShape, imgRect, imgClip, effects, inMargin, imgDim, img`. The original 05/06 emitted `offset, orgSz, curSz, sz, pos, outMargin, ...`, omitted model attributes and placed `hp:pic` content in an unsafe minimal form. The old inspector only checked that a binary and a positive size existed, so it was a serious false positive.

The writer now emits the model order, explicit object/component attributes, `shapeComment`, `inMargin`, positive HWPUNIT geometry, and places `hp:pic` inside the first `hp:run`. The validator rejects the old child sequence, invalid anchor, missing required children, unresolved resource, and invalid geometry. It does not claim Hancom compatibility until manual validation.

## Golden comparison result

The PNG and JPEG golden files have the same semantic `hp:pic` child sequence as the fixed samples: `offset → orgSz → curSz → flip → rotationInfo → renderingInfo → img → imgRect → imgClip → inMargin → imgDim → effects → sz → pos → outMargin → shapeComment`. Both use `hp:p → hp:run → hp:pic`, positive geometry, `WIDELY` A4 portrait page semantics, and a resolvable `binaryItemIDRef`/BinData relation. Fixed samples use generated IDs (`pic0`, `image0`) and HWPUNIT placement; golden IDs and the JPEG `.JPG` casing are not copied.

The remaining differences are P2/P3: golden preview files, richer header metadata, exact Hancom-generated object IDs, and PNG/JPEG-specific original-image dimensions. No additional P0/P1 structural difference was found in the package, manifest, anchor, required picture children, geometry, or resource-reference chain. Original 05/06 continue to fail the strengthened validator with `HWPX_IMAGE_CHILD_SEQUENCE_INVALID`.

## Image Embedded vs Linked Resolution

The user observed a Hancom “그림 경로” dialog for 07, showing `BinData/image0.png` with an empty external path. 07/08 manifest image items lacked an embedded marker. The Hancom public reader reads the misspelled attribute `isEmbeded` (not `isEmbedded`) and treats only `1` or `true` as embedded; the default is false. PNG golden uses `isEmbeded="1"`; JPEG golden uses the same spelling and `media-type="image/jpg"`. Neither golden uses `sub-path`, and `META-INF/manifest.xml` is an empty ODF manifest in both.

The corrected writer emits `isEmbeded="1"`, maps JPEG’s HPF media type to `image/jpg` for Hancom compatibility, and preserves the actual BinData bytes and package href. The validator now rejects missing embedded flags as `HWPX_IMAGE_NOT_EMBEDDED`. New 09/10 samples pass this chain; 07/08 remain preserved forensic artifacts and fail the new check.

New samples are `07-image-png-fixed.hwpx` and `08-image-jpeg-fixed.hwpx`. Both have one `hp:pic`, one BinData resource, and pass internal/reopen validation. Visual Fidelity remains blocked.
## Picture Physical Unit Semantics

The 11 PNG sample preserved the 800×400 (2:1) aspect ratio, but Hancom displayed `28346 × 14173` as `10.00 × 5.00 mm`. This is an exact 1/10 physical-size discrepancy, not an aspect-ratio error.

`pagePr` uses 1/7200 inch units (`59528 ≈ 210 mm` for A4). The tested `hp:pic` geometry fields (`sz`, `orgSz`, `curSz`, `imgRect`, `imgClip`, `imgDim`) use 1/720 inch units (`2835 ≈ 100 mm`, `1418 ≈ 50 mm`). A single global converter was therefore incorrect.

`HwpxObjectUnitConverter` now owns picture/object millimetre conversion. Samples 13/14 use 100×50 mm and remain distinct from the preserved 11/12 forensic artifacts. Hancom manual confirmation is still required.

The 100×50mm Golden is now available at `artifacts/reference-hancom/hancom-image-100x50mm-reference.hwpx`. Its geometry is: `sz=28346×14173`, `orgSz=51899×25950`, `curSz=28346×14172`, `imgRect=51899×25950`, `imgClip=60000×30000`, and `imgDim=60000×30000`. The scale matrix is approximately `0.546176/0.546166`. This shows that `sz` is the requested physical size, while native image geometry and scale metadata must remain distinct; setting every field to the requested size caused the observed 10×5mm result.
