using Pdf2Hwp.Core;
using UglyToad.PdfPig;

namespace Pdf2Hwp.Infrastructure;

public sealed class PdfPigAnalyzer : IPdfAnalyzer
{
    public Task<PdfDocumentInfo> AnalyzeAsync(string sourcePath, CancellationToken cancellationToken) => Task.Run(() =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var document = PdfDocument.Open(sourcePath);
        var pages = new List<PdfPageInfo>();
        for (var number = 1; number <= document.NumberOfPages; number++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var page = document.GetPage(number);
            var glyphs = page.Letters.Select(letter => new PdfGlyph(
                letter.Value, letter.FontName ?? "Unknown", letter.FontSize,
                new PdfRect(letter.BoundingBox.Left, letter.BoundingBox.Bottom, letter.BoundingBox.Width, letter.BoundingBox.Height))).ToArray();
            pages.Add(new PdfPageInfo(number, page.Width, page.Height, 0, null, glyphs.Length > 0, glyphs.Length == 0, glyphs));
        }
        return new PdfDocumentInfo(sourcePath, document.NumberOfPages, pages);
    }, cancellationToken);
}
