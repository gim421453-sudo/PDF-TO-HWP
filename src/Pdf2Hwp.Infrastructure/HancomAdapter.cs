using Pdf2Hwp.Core;

namespace Pdf2Hwp.Infrastructure;

public sealed class HancomAdapter : IHwpAdapter
{
    public Task ExportAsync(string hwpxPath, string hwpPath, CancellationToken cancellationToken) =>
        throw new NotSupportedException("HWP export requires a separately licensed and explicitly configured Hancom adapter. No Hancom installation is modified or automated by this build.");
}
