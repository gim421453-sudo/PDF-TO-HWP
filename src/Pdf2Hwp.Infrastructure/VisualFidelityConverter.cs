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
public sealed record VisualFidelityBatchResult(IReadOnlyList<JobRenderResult> Pages, int UniqueImageResources, TimeSpan RenderDuration, TimeSpan WriteDuration);
public static class VisualFidelityGeometry
{
    public static (int Width, int Height) ExpectedRasterSize(PdfPageInfo page, int dpi) => ((int)Math.Round(page.WidthPoints * dpi / 72d), (int)Math.Round(page.HeightPoints * dpi / 72d));
}

public sealed class VisualFidelityConverter(IPdfAnalyzer analyzer, IPdfPageRenderer renderer, HwpxImagePackageWriter writer, IHwpxPackageInspector inspector)
{
    public static PdfPageInfo PrepareRasterOutputPage(DocumentPage logicalPage, PdfPageInfo analyzedPage)
    {
        var crop = logicalPage.Crop is { } region ? new PdfRect(region.Left, region.Bottom, region.Width, region.Height) : analyzedPage.CropBox;
        var width = crop?.Width ?? analyzedPage.WidthPoints; var height = crop?.Height ?? analyzedPage.HeightPoints;
        if (Math.Abs(logicalPage.RotationDegrees % 180) == 90) (width, height) = (height, width);
        return analyzedPage with { Number = logicalPage.LogicalOutputIndex + 1, WidthPoints = width, HeightPoints = height, Rotation = logicalPage.RotationDegrees, CropBox = crop, Glyphs = [] };
    }

    public async Task<VisualFidelityBatchResult> ConvertPagesAsync(
        PdfDocumentInfo document,
        IReadOnlyList<DocumentPage> logicalPages,
        string outputPath,
        string renderDirectory,
        RenderQuality quality,
        IProgress<ConversionProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (logicalPages.Count != document.PageCount) throw new ArgumentException("Logical pages must match the output section count.", nameof(logicalPages));
        Directory.CreateDirectory(renderDirectory);
        var cache = new JobRenderCache(renderer, renderDirectory);
        var renderedPages = new List<JobRenderResult>(logicalPages.Count);
        var resourcesByHash = new Dictionary<string, HwpxBinaryResource>(StringComparer.Ordinal);
        var pageResourceIds = new List<string>(logicalPages.Count);
        var renderWatch = Stopwatch.StartNew();
        for (var index = 0; index < logicalPages.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new(ConversionJobStatus.Rendering, "PDF 페이지 렌더링", index, logicalPages.Count, 35 + 30d * index / logicalPages.Count));
            var rendered = await cache.RenderAsync(logicalPages[index], (int)quality, cancellationToken).ConfigureAwait(false);
            renderedPages.Add(rendered);
            if (!resourcesByHash.TryGetValue(rendered.RenderHash, out var resource))
            {
                resource = new HwpxImageResourceWriter().Create(rendered.RenderPath, $"image{resourcesByHash.Count}", "image/png");
                resourcesByHash.Add(rendered.RenderHash, resource);
            }
            pageResourceIds.Add(resource.Id);
        }
        renderWatch.Stop();
        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(new(ConversionJobStatus.Writing, "HWPX page-background 작성", 0, 1, 70));
        var writeWatch = Stopwatch.StartNew();
        await writer.WritePageBackgroundsAsync(document, outputPath, resourcesByHash.Values.ToArray(), pageResourceIds, cancellationToken).ConfigureAwait(false);
        writeWatch.Stop();
        return new(renderedPages, resourcesByHash.Count, renderWatch.Elapsed, writeWatch.Elapsed);
    }

    public async Task<VisualFidelityResult> ConvertAsync(string pdfPath, string outputPath, VisualFidelityOptions? options, CancellationToken cancellationToken)
    {
        options ??= new(); var clock = Stopwatch.StartNew(); var job = Path.Combine(Path.GetTempPath(), "PDF2HWP", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(job);
        try
        {
            var source = await analyzer.AnalyzeAsync(pdfPath, cancellationToken).ConfigureAwait(false);
            var sourceDocument = SourceDocument.Create(pdfPath, source.PageCount);
            var logicalPages = source.Pages.Select((page, index) => new DocumentPage(Guid.NewGuid(), sourceDocument.Id, sourceDocument.SourcePath, index, index, page.Rotation, page.CropBox is null ? null : new CropRegion(page.CropBox.Left, page.CropBox.Bottom, page.CropBox.Width, page.CropBox.Height), ["Source"], page.MediaBox, page.Rotation)).ToArray();
            var outputPages = logicalPages.Select((page, index) => PrepareRasterOutputPage(page, source.Pages[index])).ToArray();
            var outputDocument = new PdfDocumentInfo(sourceDocument.SourcePath, outputPages.Length, outputPages);
            var tempOutput = Path.Combine(job, "output.hwpx");
            var rendered = await ConvertPagesAsync(outputDocument, logicalPages, tempOutput, Path.Combine(job, "render"), options.Dpi == VisualFidelityDpi.High ? RenderQuality.High : RenderQuality.Standard, null, cancellationToken).ConfigureAwait(false);
            var validation = inspector.Inspect(tempOutput); cancellationToken.ThrowIfCancellationRequested();
            if (validation.Status == ValidationStatus.Fail) throw new InvalidDataException(string.Join("; ", validation.Issues.Select(i => i.Code)));
            var reopened = inspector.Inspect(tempOutput); cancellationToken.ThrowIfCancellationRequested();
            if (reopened.Status == ValidationStatus.Fail) throw new InvalidDataException("Reopen validation failed.");
            var renderPath = rendered.Pages[0].RenderPath;
            var artifact = options.RenderArtifactPath ?? Path.Combine(Path.GetDirectoryName(Path.GetFullPath(outputPath))!, Path.GetFileNameWithoutExtension(outputPath) + ".render.png");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(artifact))!); File.Copy(renderPath, artifact, false);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!); cancellationToken.ThrowIfCancellationRequested(); File.Move(tempOutput, outputPath, false);
            var renderHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(artifact))).ToLowerInvariant();
            return new(outputPath, artifact, rendered.Pages[0].Width, rendered.Pages[0].Height, clock.Elapsed, validation, renderHash == rendered.Pages[0].RenderHash);
        }
        finally { if (Directory.Exists(job)) try { Directory.Delete(job, true); } catch { } }
    }
    private static (int Width, int Height) ReadPngDimensions(byte[] bytes) => (ReadInt(bytes, 16), ReadInt(bytes, 20));
    private static int ReadInt(byte[] b, int o) => (b[o] << 24) | (b[o + 1] << 16) | (b[o + 2] << 8) | b[o + 3];
}
