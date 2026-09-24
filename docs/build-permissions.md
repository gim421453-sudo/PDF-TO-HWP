# Build-artifact permission recovery

The source tree is owned by `DESKTOP-B0I2P6A\USER` and inherits Modify permission for the current account. A prior build left generated files below `src/Pdf2Hwp.Infrastructure/obj/Release/net8.0` that could not be overwritten (`CS2012`).

Recovery is intentionally limited to regenerated artifacts: each project `bin/` and `obj/`, plus project-root `.build3/` and `.out3/`. No source file, system path, Hancom path, or parent `C:\dev` ACL is changed. Before a clean build, verify a create/write/delete probe at the project root, then delete only those generated folders.

Use `dotnet restore --ignore-failed-sources -p:NuGetAudit=false` only when packages are already present locally and the environment blocks NuGet's advisory feed through a bogus proxy. This is a process-scoped CI workaround, not a change to NuGet sources, proxy settings, or certificate validation.

If MSBuild still reports a no-diagnostic failure, inspect active `dotnet`, `MSBuild`, and `VBCSCompiler` processes before terminating anything. Do not terminate processes not started for this workspace.

## Incident B: non-diagnostic `dotnet build` result

After Incident A was cleared, ordinary `dotnet build` sometimes emitted only a failed summary with zero warnings and errors. It is distinct from the artifact ACL problem: a process-scoped, isolated invocation of `dotnet msbuild` with `-m:1 -nr:false -tl:off -p:UseSharedCompilation=false` completed Restore and Release Build with exit code 0. The corresponding diagnostic text and binary logs are retained under `artifacts/build-diagnostics/` and must be examined before attributing a future failure to source code. No pre-existing dotnet/MSBuild/VBCSCompiler process was terminated.
