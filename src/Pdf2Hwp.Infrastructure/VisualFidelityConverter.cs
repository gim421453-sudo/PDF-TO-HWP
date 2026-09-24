using System.Diagnostics;
using Pdf2Hwp.Core;

namespace Pdf2Hwp.Infrastructure;

public enum VisualFidelityDpi { Standard = 200, High = 300 }
public sealed record VisualFidelityOptions(VisualFidelityDpi Dpi = VisualFidelityDpi.Standard, string? RenderArtifactPath = null, long SafetyInsetHwpUnit = 0);
public static class VisualFidelityLayoutSafetyPolicy
{
    public const long MaximumAutomaticInsetHwpUnit = 20;
    public static (long Width, long Height) Apply(long pageWidth, long pageHeight, long inset)
    { if (pageWidth <= 0 || pageHeight <= 0 || inset < 0 || inset > MaximumAutomaticInsetHwpUnit) throw new ArgumentOutOfRangeException(nameof(inset)); if (inset == 0) return (pageWidth, pageHeight); var scale = Math.Min((pageWidth - inset) / (double)pageWidth, (pageHeight - inset) / (double)pageHeight); return ((long)Math.Round(pageWidth * scale, MidpointRounding.AwayFromZero), (long)Math.Round(pageHeight * scale, MidpointRounding.AwayFromZero)); }
}
public sealed record VisualFidelityResult(string OutputPath, string RenderPath, int WidthPx, int HeightPx, TimeSpan TotalDuration, ValidationReport Validation, bool RenderHashPreserved);
public static class VisualFidelityGeometry
{
    public static (int Width, int Height) ExpectedRasterSize(PdfPageInfo page, int dpi) => ((int)Math.Round(page.WidthPoints * dpi / 72d), (int)Math.Round(page.HeightPoints * dpi / 72d));
}

public sealed class VisualFidelityConverter(IPdfAnalyzer analyzer, IPdfPageRenderer renderer, HwpxImagePackageWriter writer, HwpxPackageInspector inspector)
{
    public async Task<VisualFidelityResult> ConvertAsync(string pdfPath, string outputPath, VisualFidelityOptions? options, CancellationToken cancellationToken)
    {
        options ??= new(); var clock = Stopwatch.StartNew(); var job = Path.Combine(Path.GetTempPath(), "PDF2HWP", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(job);
        try
        {
            var document = await analyzer.AnalyzeAsync(pdfPath, cancellationToken).ConfigureAwait(false); if (document.PageCount != 1) throw new NotSupportedException("Visual Fidelity first sample requires exactly one PDF page.");
            var renderPath = Path.Combine(job, "page.png"); await renderer.RenderAsync(new(pdfPath, 1, renderPath, (int)options.Dpi), cancellationToken).ConfigureAwait(false);
            var artifact = options.RenderArtifactPath ?? Path.Combine(Path.GetDirectoryName(outputPath)!, Path.GetFileNameWithoutExtension(outputPath) + ".render.png"); Directory.CreateDirectory(Path.GetDirectoryName(artifact)!); File.Copy(renderPath, artifact, true); renderPath = artifact;
            var resource = new HwpxImageResourceWriter().Create(renderPath, "image0", "image/png"); var page = document.Pages[0]; var pageWidth = HwpxUnitConverter.FromPdfPoint(page.WidthPoints).Value; var pageHeight = HwpxUnitConverter.FromPdfPoint(page.HeightPoints).Value; var placement = VisualFidelityLayoutSafetyPolicy.Apply(pageWidth, pageHeight, options.SafetyInsetHwpUnit);
            var tempOutput = Path.Combine(job, "output.hwpx"); await writer.WriteAsync(document, tempOutput, resource, new HwpxImagePlacement("pic0", "image0", 0, 0, placement.Width, placement.Height, FullPage: true), cancellationToken).ConfigureAwait(false);
            var validation = inspector.Inspect(tempOutput); if (validation.Status == ValidationStatus.Fail) throw new InvalidDataException(string.Join("; ", validation.Issues.Select(i => i.Code)));
            var reopened = inspector.Inspect(tempOutput); if (reopened.Status == ValidationStatus.Fail) throw new InvalidDataException("Reopen validation failed.");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!); File.Move(tempOutput, outputPath, true);
            var embeddedHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(resource.Bytes)); var renderHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(renderPath)));
            var dimensions = ReadPngDimensions(resource.Bytes); return new(outputPath, renderPath, dimensions.Width, dimensions.Height, clock.Elapsed, validation, embeddedHash == renderHash);
        }
        finally { if (Directory.Exists(job)) try { Directory.Delete(job, true); } catch { } }
    }
    private static (int Width, int Height) ReadPngDimensions(byte[] bytes) => (ReadInt(bytes, 16), ReadInt(bytes, 20));
    private static int ReadInt(byte[] b, int o) => (b[o] << 24) | (b[o + 1] << 16) | (b[o + 2] << 8) | b[o + 3];
}
