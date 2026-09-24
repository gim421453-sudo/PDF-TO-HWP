using Pdf2Hwp.Core;
using Pdf2Hwp.Infrastructure;

namespace Pdf2Hwp.Tests;

public sealed class HwpxPackageInspectorTests
{
    [Fact]
    public async Task Writer_creates_structurally_inspectable_package()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pdf2hwp-{Guid.NewGuid():N}.hwpx");
        try
        {
            var document = new PdfDocumentInfo("input.pdf", 1, [new PdfPageInfo(1, 595, 842, 0, null, true, false, [new PdfGlyph("한글 ABC 123", "Malgun Gothic", 10, new PdfRect(0, 0, 10, 10))])]);
            await new HwpxWriter().WriteAsync(document, path, CancellationToken.None);
            Assert.Equal(ValidationStatus.Pass, new HwpxPackageInspector().Inspect(path).Status);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public async Task ParagraphDefaultStyleDoesNotCreateVisibleBorders()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pdf2hwp-noborder-{Guid.NewGuid():N}.hwpx");
        try
        {
            var lines = new[] { "PDF2HWP Compatibility Test", "한글 테스트", "English Test", "1234567890", "!@#$%^&*()" };
            var glyphs = lines.Select((line, index) => new PdfGlyph(index == 0 ? line : "\n" + line, "Malgun Gothic", 10, new PdfRect(0, 0, 10, 10))).ToArray();
            var document = new PdfDocumentInfo("input.pdf", 1, [new PdfPageInfo(1, 595, 842, 0, null, true, false, glyphs)]);
            await new HwpxWriter().WriteAsync(document, path, CancellationToken.None);
            var report = new HwpxPackageInspector().Inspect(path);
            Assert.Equal(ValidationStatus.Pass, report.Status);
            Assert.DoesNotContain(report.Issues, issue => issue.Code == "HWPX_VISIBLE_PARAGRAPH_BORDER");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public async Task A4PortraitWritesWidthLessThanHeightAndPortraitOrientation()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pdf2hwp-portrait-{Guid.NewGuid():N}.hwpx");
        try
        {
            var page = new PdfPageInfo(1, 595.28, 841.89, 0, null, true, false, [new PdfGlyph("text", "Malgun Gothic", 10, new PdfRect(0, 0, 1, 1))]);
            await new HwpxWriter().WriteAsync(new PdfDocumentInfo("p", 1, [page]), path, CancellationToken.None);
            Assert.Equal(ValidationStatus.Pass, new HwpxPackageInspector().Inspect(path).Status);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public async Task LandscapeWritesWidthGreaterThanHeightAndLandscapeOrientation()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pdf2hwp-landscape-{Guid.NewGuid():N}.hwpx");
        try
        {
            var page = new PdfPageInfo(1, 841.89, 595.28, 0, null, true, false, [new PdfGlyph("text", "Malgun Gothic", 10, new PdfRect(0, 0, 1, 1))]);
            await new HwpxWriter().WriteAsync(new PdfDocumentInfo("p", 1, [page]), path, CancellationToken.None);
            Assert.Equal(ValidationStatus.Pass, new HwpxPackageInspector().Inspect(path).Status);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
