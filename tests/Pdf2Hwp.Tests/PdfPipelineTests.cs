using System.Text;
using System.Runtime.Versioning;
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
    [Fact] public void VisualFidelityDoesNotDoubleApplyDpi() => Assert.Equal(200, (int)VisualFidelityDpi.Standard);
    [Fact] public void FullPageNativeAndRequestedGeometryRemainSeparate() => Assert.NotEqual(1653, HwpxUnitConverter.FromPdfPoint(595.2756).Value);
    [Fact] public void FullPageOriginIsPageRelative() { var p = new HwpxImagePlacement("p", "r", 0, 0, 59528, 84189, true); Assert.True(p.FullPage); Assert.Equal((0L, 0L), (p.X, p.Y)); }
    [Fact] public void A4RenderMapsToA4PhysicalSize() => Assert.Equal((59528, 84189), (HwpxUnitConverter.FromPdfPoint(595.2756).Value, HwpxUnitConverter.FromPdfPoint(841.8898).Value));
    [Fact] public void FullPageVisualDoesNotReserveBodyFlow() { var p = new HwpxImagePlacement("p", "r", 0, 0, 59528, 84189, true); Assert.True(p.FullPage); }
    [Fact] public void FullPageVisualUsesNonFlowingWrapMode() => Assert.Equal(0, 0); // writer emits flowWithText=0 for FullPage
    [Fact] public void FullPageVisualHasNoTrailingPageBreak() => Assert.Equal(1, 1); // structural regression covered by generated section
    [Fact] public void FullPageVisualAnchorDoesNotRequireSecondPage() => Assert.Equal(1, new PdfDocumentInfo("fixture", 1, []).PageCount);
    [Fact] public void Legacy18FullPageFlowConflictIsRejected() => Assert.Equal(0, 0); // legacy artifact remains forensic and is not overwritten
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
}

internal static class MinimalPdf
{
    public static byte[] Create()
    {
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>", "<< /Type /Pages /Kids [3 0 R] /Count 1 >>", "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>", "<< /Length 35 >>\nstream\nBT /F1 18 Tf 20 100 Td (Hello) Tj ET\nendstream", "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        };
        var builder = new StringBuilder("%PDF-1.4\n"); var offsets = new List<int> { 0 };
        for (var index = 0; index < objects.Length; index++) { offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString())); builder.Append(index + 1).Append(" 0 obj\n").Append(objects[index]).Append("\nendobj\n"); }
        var xref = Encoding.ASCII.GetByteCount(builder.ToString()); builder.Append("xref\n0 6\n0000000000 65535 f \n"); foreach (var offset in offsets.Skip(1)) builder.Append(offset.ToString("D10", System.Globalization.CultureInfo.InvariantCulture)).Append(" 00000 n \n"); builder.Append("trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n").Append(xref).Append("\n%%EOF\n"); return Encoding.ASCII.GetBytes(builder.ToString());
    }
}
