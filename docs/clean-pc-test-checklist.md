# PDF2HWP clean-PC test checklist

Run every case without administrator elevation. Record application version, Windows version, Hancom version, source PDF hash, output hash, and PASS/WARNING/FAIL.

- [ ] Windows 10 x64 and Windows 11 x64 with no .NET runtime installed: launch the published EXE.
- [ ] No Hancom installed: HWPX conversion, package validation, and clear HWP-export availability message.
- [ ] Hancom installed: open HWPX manually, save a copy, and compare rendered pages.
- [ ] Offline machine: launch and convert after installation; no dependency download may occur.
- [ ] Korean, spaces, long names, and non-ASCII output paths.
- [ ] Standard (non-admin) user and a folder without write permission.
- [ ] TEMP available and TEMP unavailable: report a clear actionable error; do not leave partial output.
- [ ] Antivirus real-time protection enabled: cold start, PDFium render, cancellation, exit.
- [ ] Confirm input PDF hash is unchanged after success, warning, failure, and cancellation.
- [ ] Confirm stale dedicated temp workspaces are cleaned on next launch without touching other TEMP data.

Do not call a deployment successful until every applicable row has evidence.
