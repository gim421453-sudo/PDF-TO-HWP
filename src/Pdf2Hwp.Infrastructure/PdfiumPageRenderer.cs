using Pdf2Hwp.Core;
using PDFtoImage;
using System.Drawing;
using System.Runtime.Versioning;

namespace Pdf2Hwp.Infrastructure;

[SupportedOSPlatform("windows")]
public sealed class PdfiumPageRenderer : IPdfPageRenderer
{
    // PDFtoImage/PDFium is not thread-safe within one process.
    private static readonly SemaphoreSlim RenderGate = new(1, 1);

    public async Task RenderAsync(RenderRequest request, CancellationToken cancellationToken)
    {
        await RenderGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var source = File.OpenRead(request.SourcePath);
            var normalizedRotation = ((request.RotationDegrees % 360) + 360) % 360;
            var intrinsicRotation = ((request.IntrinsicRotationDegrees % 360) + 360) % 360;
            var renderRotation = ((normalizedRotation - intrinsicRotation) % 360 + 360) % 360;
            var rotation = renderRotation switch { 90 => PdfRotation.Rotate90, 180 => PdfRotation.Rotate180, 270 => PdfRotation.Rotate270, _ => PdfRotation.Rotate0 };
            RectangleF? bounds = request.CropBox is { } crop ? new RectangleF((float)crop.Left, (float)crop.Bottom, (float)crop.Width, (float)crop.Height) : null;
            Conversion.SavePng(request.OutputPngPath, source, page: request.PageNumber - 1,
                options: new(Dpi: request.Dpi, Width: request.Width, Height: request.Height, WithAspectRatio: request.Width.HasValue ^ request.Height.HasValue, Rotation: rotation, Bounds: bounds));
        }
        finally { RenderGate.Release(); }
    }
}
