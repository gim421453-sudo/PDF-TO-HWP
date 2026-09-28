# PDF2HWP third-party license and notice audit

## Audit result

**BINARY_PROVENANCE_VERIFIED** and **THIRD_PARTY_NOTICE_SET_VERIFIED** for the resolved Windows x64 publish dependency set listed below. The required original license/notice files are preserved under `licenses/third-party/` and are copied into the publish package by `eng/publish-win-x64.ps1`. This records source and packaging evidence; it is not legal advice.

## PDFium binary provenance

Verified chain: `Pdf2Hwp.Infrastructure` direct `PackageReference` `PDFtoImage 5.4.0` -> transitive `bblanchon.PDFium.Win32 152.0.7961` -> `runtimes/win-x64/native/pdfium.dll` -> PDFium release branch `chromium/7961` (`152.0.7961.0`). The app dependency manifest confirms the selected `win-x64` packages; the NuGet `.nuspec` records source commit `c6529b58791d142002f819beb46e370e668797d7` for `bblanchon/pdfium-binaries`.

| Binary | SHA-256 |
|---|---|
| DLL from official NuGet package `bblanchon.PDFium.Win32 152.0.7961` | `D3D9F4B7C9DABE3363F30779C5C3C715C47332749FA590E4B4A2B8B6780CB1C4` |
| DLL from official release archive `pdfium-win-x64.tgz`, `bin/pdfium.dll` | `D3D9F4B7C9DABE3363F30779C5C3C715C47332749FA590E4B4A2B8B6780CB1C4` |
| Project Release output `pdfium.dll` used by publish | `D3D9F4B7C9DABE3363F30779C5C3C715C47332749FA590E4B4A2B8B6780CB1C4` |

All three DLL hashes match. The official release archive SHA-256 is `88276459349B291C41F10422DAD0210F007C04D919C8FA56472B6B7C6406ADF4`, matching the `pdfium-win-x64.tgz` subject digest in `pdfium-attestation.json`. `gh attestation verify` passed with repository `bblanchon/pdfium-binaries`, signer workflow `.github/workflows/build-all.yml`, and source ref `refs/heads/master`. The NuGet package's repository signature also passed `dotnet nuget verify`; its content hash matches the restore assets. The audit downloads, expanded archives and verifier configuration are reproducible in ignored `artifacts/license-audit/`.

The official release archive contains a top-level `LICENSE` plus 15 files in `licenses/`; there were no separate files named `NOTICE`, `THIRD-PARTY-NOTICES`, `LICENSES`, `credits`, or `copyright`. The official NuGet package itself contains no license/notice file; its `.nuspec` declares `Apache-2.0`. Both distribution-source and package-expression obligations are kept separate below.

## Resolved publish dependency inventory

NuGet source recorded in restore metadata: `https://api.nuget.org/v3/index.json`. “Included notice/license” paths below are exact original text files, not rewritten summaries.

| Component and kind | Version / source | Declared license | Redistribution material included in release package |
|---|---|---|---|
| `PdfPig` — direct managed package | `0.1.15`; source commit `f131f642976936e06ee91cb19d3ed728f9dd18b6` | Apache-2.0 | `licenses/third-party/PdfPig/0.1.15/LICENSE` (exact source LICENSE; nupkg has no license/notice file). |
| `PDFtoImage` — direct managed rendering adapter | `5.4.0`; source commit `5948628cb2cf7d070c7fd28b1b4496db39e3afcd` | MIT | `licenses/third-party/PDFtoImage/5.4.0/LICENSE` (exact source LICENSE; nupkg has no license/notice file). |
| `bblanchon.PDFium.Win32` — transitive native package | `152.0.7961`; source commit `c6529b58791d142002f819beb46e370e668797d7` | Apache-2.0 NuGet package expression | `licenses/third-party/bblanchon.PDFium.Win32/152.0.7961/Apache-2.0.txt` (official Apache text). This package expression is not substituted for licenses of its embedded native code. |
| `bblanchon/pdfium-binaries` — native binary distributor/source repository | Release `chromium/7961`; repository source commit above | MIT, from the exact release archive's root `LICENSE` | `licenses/third-party/bblanchon.pdfium-binaries/152.0.7961/LICENSE`. |
| PDFium — native upstream component | `152.0.7961.0`, release branch `chromium/7961` | BSD-3-Clause terms, as provided by this release's original `licenses/pdfium.txt` | `licenses/third-party/PDFium/152.0.7961/LICENSE`. |
| PDFium embedded third-party components — native/transitive | Exact official `chromium/7961` release archive inventory | Each component's own original notice/license; do not collapse to the PDFium license | All 15 untouched archive files under `licenses/third-party/PDFium/152.0.7961/licenses/`: `abseil.txt`, `agg23.txt`, `fast_float.txt`, `freetype.txt`, `icu.txt`, `lcms.txt`, `libjpeg_turbo.ijg`, `libjpeg_turbo.md`, `libopenjpeg.txt`, `libpng.txt`, `libtiff.txt`, `llvm-libc.txt`, `pdfium.txt`, `simdutf.txt`, `zlib.txt`. |
| `SkiaSharp` — transitive managed graphics package | `4.150.1`; package source commit `c3e4f4c20e1f23ab74d31a8838a5bd6dc55365f2` | MIT | Shared original package `LICENSE.txt` is preserved in `licenses/third-party/SkiaSharp/4.150.1/`. |
| `SkiaSharp.NativeAssets.Win32` — transitive native graphics package (`libSkiaSharp.dll`) | `4.150.1`; same source commit | MIT | The actual Win32 nupkg's `LICENSE.txt` and `THIRD-PARTY-NOTICES.txt` are preserved under `licenses/third-party/SkiaSharp/4.150.1/`. The package notice includes its bundled component notices. |
| `Microsoft.NETCore.App.Runtime.win-x64` — bundled self-contained runtime pack | `8.0.27` | MIT | The actual runtime nupkg's `LICENSE.TXT` and `THIRD-PARTY-NOTICES.TXT` are preserved under `licenses/third-party/Microsoft.NETCore.App.Runtime.win-x64/8.0.27/`. |
| `Microsoft.WindowsDesktop.App.Runtime.win-x64` — bundled WPF runtime pack | `8.0.27` | MIT | The actual runtime nupkg's `LICENSE` is preserved under `licenses/third-party/Microsoft.WindowsDesktop.App.Runtime.win-x64/8.0.27/`; its nupkg contains no separate third-party notice file. |

NuGet author/repository signatures were verified for all five library packages above plus both self-contained runtime packs using `dotnet nuget verify --all`. `Microsoft.NET.ILLink.Tasks 8.0.27` is a build-time asset and is not listed in the published app dependency manifest or bundled in the release; it is not treated as a redistributed runtime component.

## Release package

`PDF2HWP.exe` remains a `win-x64`, self-contained, single-file application binary. A release is a package directory, not necessarily one file:

```text
artifacts/win-x64-singlefile/
  PDF2HWP.exe
  THIRD_PARTY_NOTICES.md
  licenses/third-party/...
```

`eng/publish-win-x64.ps1` copies the audit and original license/notice bundle beside the EXE. Validate the result with `eng/verify-license-assets.ps1`. Do not redistribute the EXE without these accompanying materials.
