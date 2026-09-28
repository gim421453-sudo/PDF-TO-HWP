using System.Text;
using System.Runtime.Versioning;
using System.IO.Compression;
using System.Xml.Linq;
using Pdf2Hwp.Infrastructure;
using Pdf2Hwp.Core;

namespace Pdf2Hwp.Tests;

[SupportedOSPlatform("windows")]
public sealed class PdfPipelineTests
{
    [Fact] public void A4_200dpi_expected_raster_size_is_stable() => Assert.Equal((1654, 2339), VisualFidelityGeometry.ExpectedRasterSize(new PdfPageInfo(1, 595.2756, 841.8898, 0, null, false, false, []), 200));
    [Fact] public void Full_page_placement_uses_pdf_page_units() { var p = new PdfPageInfo(1, 595.2756, 841.8898, 0, null, false, false, []); Assert.Equal((59528, 84189), (HwpxUnitConverter.FromPdfPoint(p.WidthPoints).Value, HwpxUnitConverter.FromPdfPoint(p.HeightPoints).Value)); }
    [Fact] public void Visual_fidelity_dpi_is_explicit() => Assert.Equal(200, (int)VisualFidelityDpi.Standard);
    [Fact] public void Visual_fidelity_high_dpi_is_explicit() => Assert.Equal(300, (int)VisualFidelityDpi.High);
    [Fact] public void FullPageEffectiveScaleMatchesRequestedSize() => Assert.Equal(59528, HwpxUnitConverter.FromPdfPoint(595.2756).Value);
    [Fact] public void VisualFidelityDoesNotDoubleApplyDpi() => Assert.Equal((200, 400), VisualFidelityGeometry.ExpectedRasterSize(new PdfPageInfo(1, 72, 144, 0, null, false, false, []), (int)VisualFidelityDpi.Standard));
    [Fact] public void FullPageNativeAndRequestedGeometryRemainSeparate() => Assert.NotEqual(1653, HwpxUnitConverter.FromPdfPoint(595.2756).Value);
    [Fact] public void FullPageOriginIsPageRelative() { var p = new HwpxImagePlacement("p", "r", 0, 0, 59528, 84189, true); Assert.True(p.FullPage); Assert.Equal((0L, 0L), (p.X, p.Y)); }
    [Fact] public void A4RenderMapsToA4PhysicalSize() => Assert.Equal((59528, 84189), (HwpxUnitConverter.FromPdfPoint(595.2756).Value, HwpxUnitConverter.FromPdfPoint(841.8898).Value));
    [Fact] public void FullPageVisualDoesNotReserveBodyFlow() { var p = new HwpxImagePlacement("p", "r", 0, 0, 59528, 84189, true); Assert.True(p.FullPage); }
    [Fact] public async Task FullPageVisualUsesNonFlowingPageBackground()
    {
        using var fixture = new PipelineFixture();
        var resource = fixture.CreatePngResource("image0");
        await new HwpxImagePackageWriter().WritePageBackgroundAsync(fixture.Document(), fixture.Output, resource, CancellationToken.None);
        using var zip = ZipFile.OpenRead(fixture.Output);
        var header = ReadXml(zip, "Contents/header.xml");
        var section = ReadXml(zip, "Contents/section0.xml");
        XNamespace hh = "http://www.hancom.co.kr/hwpml/2011/head";
        XNamespace hp = "http://www.hancom.co.kr/hwpml/2011/paragraph";
        XNamespace hc = "http://www.hancom.co.kr/hwpml/2011/core";
        var fill = header.Descendants(hh + "borderFill").Single(x => (string?)x.Attribute("id") == "3");
        Assert.Equal("TOTAL", (string?)fill.Element(hc + "fillBrush")?.Element(hc + "imgBrush")?.Attribute("mode"));
        Assert.Equal("image0", (string?)fill.Descendants(hc + "img").Single().Attribute("binaryItemIDRef"));
        Assert.Empty(section.Descendants(hp + "pic"));
        Assert.Empty(section.Descendants(hp + "t"));
        Assert.Equal("3", (string?)section.Descendants(hp + "pageBorderFill").Single(x => (string?)x.Attribute("type") == "BOTH").Attribute("borderFillIDRef"));
        Assert.Equal(ValidationStatus.Pass, new HwpxPackageInspector().Inspect(fixture.Output).Status);
    }

    [Fact]
    public async Task Single_document_visual_converter_uses_validated_page_background_output()
    {
        using var fixture = new PipelineFixture();
        var pdf = Path.Combine(fixture.Directory, "single-convert.pdf");
        await File.WriteAllBytesAsync(pdf, MinimalPdf.Create());
        var output = Path.Combine(fixture.Directory, "single-convert.hwpx");
        var artifact = Path.Combine(fixture.Directory, "single-convert-render.png");
        var converter = new VisualFidelityConverter(new PdfPigAnalyzer(), new PdfiumPageRenderer(), new HwpxImagePackageWriter(), new HwpxPackageInspector());

        var result = await converter.ConvertAsync(pdf, output, new(RenderArtifactPath: artifact), CancellationToken.None);

        Assert.Equal(ValidationStatus.Pass, result.Validation.Status);
        Assert.True(result.RenderHashPreserved);
        Assert.True(File.Exists(output));
        Assert.True(File.Exists(artifact));
        using var zip = ZipFile.OpenRead(output);
        var header = ReadXml(zip, "Contents/header.xml");
        var section = ReadXml(zip, "Contents/section0.xml");
        XNamespace hh = "http://www.hancom.co.kr/hwpml/2011/head";
        XNamespace hp = "http://www.hancom.co.kr/hwpml/2011/paragraph";
        XNamespace hc = "http://www.hancom.co.kr/hwpml/2011/core";
        Assert.Equal("TOTAL", (string?)header.Descendants(hh + "borderFill").Single(x => (string?)x.Attribute("id") == "3").Element(hc + "fillBrush")?.Element(hc + "imgBrush")?.Attribute("mode"));
        Assert.Empty(section.Descendants(hp + "pic"));
        Assert.Equal(ValidationStatus.Pass, new HwpxPackageInspector().Inspect(output).Status);
    }

    [Fact] public async Task FullPageVisualHasNoTrailingPageBreak()
    {
        using var fixture = new PipelineFixture();
        var resource = fixture.CreatePngResource("image0");
        await new HwpxImagePackageWriter().WritePageBackgroundAsync(fixture.Document(), fixture.Output, resource, CancellationToken.None);
        using var zip = ZipFile.OpenRead(fixture.Output);
        var manifest = ReadXml(zip, "Contents/content.hpf");
        XNamespace opf = "http://www.idpf.org/2007/opf/";
        Assert.Single(manifest.Descendants(opf + "item"), x => ((string?)x.Attribute("href"))?.StartsWith("Contents/section", StringComparison.Ordinal) == true);
        Assert.Single(manifest.Descendants(opf + "itemref"), x => ((string?)x.Attribute("idref"))?.StartsWith("section", StringComparison.Ordinal) == true);
        Assert.Equal(ValidationStatus.Pass, new HwpxPackageInspector().Inspect(fixture.Output).Status);
    }
    [Fact] public async Task FullPageVisualAnchorDoesNotRequireSecondPage()
    {
        using var fixture = new PipelineFixture();
        var resource = fixture.CreatePngResource("image0");
        await new HwpxImagePackageWriter().WritePageBackgroundAsync(fixture.Document(), fixture.Output, resource, CancellationToken.None);
        using var zip = ZipFile.OpenRead(fixture.Output);
        Assert.Single(zip.Entries, entry => entry.FullName == "Contents/section0.xml");
        Assert.Equal(ValidationStatus.Pass, new HwpxPackageInspector().Inspect(fixture.Output).Status);
    }
    [Fact] public async Task GeneratedPageBackgroundUsesTotalFillModeAndValidPackage()
    {
        using var fixture = new PipelineFixture();
        var resource = fixture.CreatePngResource("image0");
        await new HwpxImagePackageWriter().WritePageBackgroundAsync(fixture.Document(), fixture.Output, resource, CancellationToken.None);
        using var zip = ZipFile.OpenRead(fixture.Output);
        var header = ReadXml(zip, "Contents/header.xml");
        XNamespace hh = "http://www.hancom.co.kr/hwpml/2011/head";
        XNamespace hc = "http://www.hancom.co.kr/hwpml/2011/core";
        var imageFill = header.Descendants(hh + "borderFill").Single(x => (string?)x.Attribute("id") == "3");
        Assert.NotNull(imageFill.Element(hc + "fillBrush")?.Element(hc + "imgBrush"));
        Assert.DoesNotContain(header.Descendants(hh + "borderFill").Where(x => (string?)x.Attribute("id") == "3"), x => x.Descendants(hc + "imgBrush").Any(image => (string?)image.Attribute("mode") != "TOTAL"));
        Assert.Equal(ValidationStatus.Pass, new HwpxPackageInspector().Inspect(fixture.Output).Status);
    }
    [Fact] public void ExactPageSizedPictureIsKnownPaginationRisk() => Assert.Equal((59528L, 84189L), VisualFidelityLayoutSafetyPolicy.Apply(59528, 84189, 0));
    [Fact] public void SafetyInsetPreservesAspectRatio() { var p = VisualFidelityLayoutSafetyPolicy.Apply(59528, 84189, 1); Assert.Equal(59527L / (double)84188L, p.Width / (double)p.Height, 6); }
    [Fact] public void SafetyInsetNeverChangesPagePr() => Assert.Equal((59528L, 84189L), (HwpxUnitConverter.FromPdfPoint(595.2756).Value, HwpxUnitConverter.FromPdfPoint(841.8898).Value));
    [Fact] public void SafetyInsetPictureNeverExceedsPage() { var p = VisualFidelityLayoutSafetyPolicy.Apply(59528, 84189, 20); Assert.True(p.Width <= 59528 && p.Height <= 84189); }
    [Fact] public void OneHwpUnitInsetCalculationIsStable() => Assert.Equal((59527L, 84188L), VisualFidelityLayoutSafetyPolicy.Apply(59528, 84189, 1));
    [Fact] public void SafetyInsetUpperBoundIsEnforced() => Assert.Throws<ArgumentOutOfRangeException>(() => VisualFidelityLayoutSafetyPolicy.Apply(59528, 84189, 21));
    [Fact]
    public async Task Analyzer_and_pdfium_renderer_process_a_minimal_pdf()
    {
        var directory = Path.Combine(Path.GetTempPath(), "pdf2hwp-test-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        var pdf = Path.Combine(directory, "sample.pdf"); var png = Path.Combine(directory, "page.png");
        try
        {
            await File.WriteAllBytesAsync(pdf, MinimalPdf.Create());
            var result = await new PdfPigAnalyzer().AnalyzeAsync(pdf, CancellationToken.None);
            Assert.Equal(1, result.PageCount); Assert.True(result.Pages[0].HasText);
            await new PdfiumPageRenderer().RenderAsync(new(pdf, 1, png, 72), CancellationToken.None);
            Assert.True(new FileInfo(png).Length > 0);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task One_hundred_page_thumbnail_batch_uses_real_pdfium_renderer()
    {
        using var fixture = new PipelineFixture();
        var pdf = Path.Combine(fixture.Directory, "hundred-pages.pdf");
        await File.WriteAllBytesAsync(pdf, MinimalPdf.Create(pageCount: 100));
        var analyzer = new PdfPigAnalyzer(); var metadata = await analyzer.AnalyzeAsync(pdf, CancellationToken.None);
        Assert.Equal(100, metadata.PageCount);
        var cache = new PreviewThumbnailCache(Path.Combine(fixture.Directory, "preview-cache"));
        using var renderer = new CachedPreviewThumbnailRenderer(new PdfiumPreviewThumbnailRenderer(new PdfiumPageRenderer()), cache, 2);
        var results = await Task.WhenAll(Enumerable.Range(0, metadata.PageCount).Select(index => renderer.RenderAsync(pdf, index, new(300), CancellationToken.None)));
        Assert.Equal(100, results.Length); Assert.All(results, result => Assert.True(result.Width > 0 && result.Height > 0));
        Assert.Equal(100, cache.Measure().Entries); Assert.Equal(0, renderer.ActiveRenderCount);
    }

    [Fact]
    public void Sample23_is_inspected_without_modification_and_keeps_manual_gate()
    {
        var root = FindRepositoryRoot();
        var sample = Path.Combine(root, "artifacts", "compatibility-tests", "23-visual-fidelity-a4-portrait-3pages-e2e.hwpx");
        Assert.True(File.Exists(sample), "Tracked sample 23 fixture is required for structural regression validation.");
        var before = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(sample)));
        var first = new HwpxPackageInspector().Inspect(sample); var reopened = new HwpxPackageInspector().Inspect(sample);
        Assert.Equal(ValidationStatus.Pass, first.Status); Assert.Equal(ValidationStatus.Pass, reopened.Status);
        Assert.Equal(before, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(sample))));
        using var zip = ZipFile.OpenRead(sample);
        Assert.Equal(3, zip.Entries.Count(entry => entry.FullName.StartsWith("Contents/section", StringComparison.Ordinal) && entry.FullName.EndsWith(".xml", StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData(0, 160, 140)]
    [InlineData(90, 140, 160)]
    [InlineData(180, 160, 140)]
    [InlineData(270, 140, 160)]
    public async Task Analyzer_preserves_rotation_crop_and_renderer_applies_them(int rotation, int expectedWidth, int expectedHeight)
    {
        using var fixture = new PipelineFixture();
        var pdfPath = Path.Combine(fixture.Directory, "rotation-crop.pdf");
        await File.WriteAllBytesAsync(pdfPath, MinimalPdf.Create(rotation, [20, 30, 180, 170]));
        var analyzed = await new PdfPigAnalyzer().AnalyzeAsync(pdfPath, CancellationToken.None);
        Assert.Equal(rotation, analyzed.Pages[0].Rotation);
        Assert.Equal(new PdfRect(20, 30, 160, 140), analyzed.Pages[0].CropBox);
        Assert.Equal(new PdfRect(0, 0, 200, 200), analyzed.Pages[0].MediaBox);
        var png = Path.Combine(fixture.Directory, "crop.png");
        await new PdfiumPageRenderer().RenderAsync(new(pdfPath, 1, png, 72, RotationDegrees: rotation, CropBox: analyzed.Pages[0].CropBox, IntrinsicRotationDegrees: analyzed.Pages[0].Rotation), CancellationToken.None);
        var pngBytes = await File.ReadAllBytesAsync(png);
        var dimensions = (Width: ReadPngInt(pngBytes, 16), Height: ReadPngInt(pngBytes, 20));
        Assert.Equal((expectedWidth, expectedHeight), (dimensions.Width, dimensions.Height));
    }

    [Theory]
    [InlineData(0, 200, 300)]
    [InlineData(90, 300, 200)]
    [InlineData(180, 200, 300)]
    [InlineData(270, 300, 200)]
    public async Task Pdf_rotation_changes_render_pixel_orientation(int rotation, int expectedWidth, int expectedHeight)
    {
        using var fixture = new PipelineFixture(); var pdf = Path.Combine(fixture.Directory, $"rotated-{rotation}.pdf");
        await File.WriteAllBytesAsync(pdf, MinimalPdf.Create(rotation: rotation, mediaWidth: 200, mediaHeight: 300));
        var analyzed = await new PdfPigAnalyzer().AnalyzeAsync(pdf, CancellationToken.None);
        var png = Path.Combine(fixture.Directory, $"rotated-{rotation}.png");
        await new PdfiumPageRenderer().RenderAsync(new(pdf, 1, png, 72, RotationDegrees: analyzed.Pages[0].Rotation, IntrinsicRotationDegrees: analyzed.Pages[0].Rotation), CancellationToken.None);
        var bytes = await File.ReadAllBytesAsync(png); var size = (Width: ReadPngInt(bytes, 16), Height: ReadPngInt(bytes, 20));
        Assert.Equal((expectedWidth, expectedHeight), size);
    }

    private static XDocument ReadXml(ZipArchive zip, string name) { using var stream = zip.GetEntry(name)!.Open(); return XDocument.Load(stream); }
    private static int ReadPngInt(byte[] bytes, int offset) => (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "PDF2HWP.sln"))) return directory.FullName;
        throw new DirectoryNotFoundException("PDF2HWP solution root was not found above the test assembly.");
    }
}

internal sealed class PipelineFixture : IDisposable
{
    public string Directory { get; } = Path.Combine(Path.GetTempPath(), "pdf2hwp-pipeline-" + Guid.NewGuid().ToString("N"));
    public string Output => Path.Combine(Directory, "sample.hwpx");
    public PipelineFixture() => System.IO.Directory.CreateDirectory(Directory);
    public PdfDocumentInfo Document() => new("fixture.pdf", 1, [new PdfPageInfo(1, 595.2756, 841.8898, 0, null, false, false, [])]);
    public HwpxBinaryResource CreatePngResource(string id)
    {
        var path = Path.Combine(Directory, id + ".png");
        File.WriteAllBytes(path, Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jqz8AAAAASUVORK5CYII="));
        return new HwpxImageResourceWriter().Create(path, id, "image/png");
    }
    public void Dispose() { if (System.IO.Directory.Exists(Directory)) System.IO.Directory.Delete(Directory, true); }
}

internal static class MinimalPdf
{
    public static byte[] Create(int rotation = 0, int[]? cropBox = null, int pageCount = 1, int mediaWidth = 200, int mediaHeight = 200)
    {
        if (pageCount <= 0) throw new ArgumentOutOfRangeException(nameof(pageCount));
        var crop = cropBox is null ? "" : $" /CropBox [{string.Join(" ", cropBox)}]";
        var rotate = rotation == 0 ? "" : $" /Rotate {rotation}";
        var firstContentId = 3 + pageCount; var fontId = 3 + pageCount * 2;
        var kids = string.Join(" ", Enumerable.Range(3, pageCount).Select(id => $"{id} 0 R"));
        var objects = new List<string> { "<< /Type /Catalog /Pages 2 0 R >>", $"<< /Type /Pages /Kids [{kids}] /Count {pageCount} >>" };
        for (var index = 0; index < pageCount; index++) objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {mediaWidth} {mediaHeight}]{crop}{rotate} /Resources << /Font << /F1 {fontId} 0 R >> >> /Contents {firstContentId + index} 0 R >>");
        for (var index = 0; index < pageCount; index++) { var content = $"BT /F1 18 Tf 20 {100 - index % 80} Td (Page {index + 1}) Tj ET"; objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream"); }
        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
        var builder = new StringBuilder("%PDF-1.4\n"); var offsets = new List<int> { 0 };
        for (var index = 0; index < objects.Count; index++) { offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString())); builder.Append(index + 1).Append(" 0 obj\n").Append(objects[index]).Append("\nendobj\n"); }
        var xref = Encoding.ASCII.GetByteCount(builder.ToString()); builder.Append("xref\n0 ").Append(objects.Count + 1).Append("\n0000000000 65535 f \n"); foreach (var offset in offsets.Skip(1)) builder.Append(offset.ToString("D10", System.Globalization.CultureInfo.InvariantCulture)).Append(" 00000 n \n"); builder.Append("trailer\n<< /Size ").Append(objects.Count + 1).Append(" /Root 1 0 R >>\nstartxref\n").Append(xref).Append("\n%%EOF\n"); return Encoding.ASCII.GetBytes(builder.ToString());
    }
}
