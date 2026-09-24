# Visual Fidelity 1-page sample

Input PDF: `C:\dev\Hwp\artifacts\pdf-fixtures\visual-fidelity-a4-portrait-1page.pdf`

Rendered PNG (PDFium, 200 DPI): `C:\dev\Hwp\artifacts\visual-fidelity\17-a4-portrait-render-200dpi.png`

Output: `17-visual-fidelity-a4-portrait-1page.hwpx` (preserved forensic artifact; Hancom manual FAIL: content rendered too small)

Fixed candidate: `18-visual-fidelity-a4-portrait-1page-fixed.hwpx`
Fixed render: `C:\dev\Hwp\artifacts\visual-fidelity\18-a4-portrait-render-200dpi.png`

17 status: `INTERNAL_PASS`, `HANCOM_MANUAL_FAIL`, `CONTENT_TOO_SMALL`, `VISUAL_FIDELITY_FAIL`

18 internal status: `VISUAL_FIDELITY_INTERNAL_PASS`; manual Hancom validation required.

19 candidate: `19-visual-fidelity-a4-portrait-1page-no-blank.hwpx`. The full-page policy removes body text runs and the trailing line-segment array, leaving one anchor paragraph/run and no explicit page break. Internal package/reopen validation PASS; Hancom page-count validation required.

Manual Hancom 2024 validation required:

- no crash, repair warning, or image path dialog
- exactly one A4 portrait page
- full-page content with all corner markers visible
- no clipping, unexpected margins, or ratio distortion
- circle remains circular
- save and reopen succeeds

Do not mark `VISUAL_FIDELITY_PASS` or `PRODUCTION_READY` until manual validation is complete.
