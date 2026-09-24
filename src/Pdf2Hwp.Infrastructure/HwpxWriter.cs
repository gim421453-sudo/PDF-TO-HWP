using Pdf2Hwp.Core;

namespace Pdf2Hwp.Infrastructure;

public sealed class HwpxWriter : IHwpxWriter
{
    public Task WriteAsync(PdfDocumentInfo document, string outputPath, CancellationToken cancellationToken) => new HwpxPackageWriter().WriteAsync(document, outputPath, cancellationToken);
}
