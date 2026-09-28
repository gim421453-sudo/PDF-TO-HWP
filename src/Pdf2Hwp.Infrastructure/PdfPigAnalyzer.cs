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
            var media = page.MediaBox.Bounds;
            var crop = page.CropBox.Bounds;
            var glyphs = page.Letters.Select(letter => new PdfGlyph(
                letter.Value, letter.FontName ?? "Unknown", letter.FontSize,
                new PdfRect(letter.BoundingBox.Left, letter.BoundingBox.Bottom, letter.BoundingBox.Width, letter.BoundingBox.Height))).ToArray();
            var mediaBox = new PdfRect(media.Left, media.Bottom, media.Width, media.Height);
            var cropBox = new PdfRect(crop.Left, crop.Bottom, crop.Width, crop.Height);
            var cropBoxDiffers = Math.Abs(mediaBox.Left - cropBox.Left) > 0.01 || Math.Abs(mediaBox.Bottom - cropBox.Bottom) > 0.01 || Math.Abs(mediaBox.Width - cropBox.Width) > 0.01 || Math.Abs(mediaBox.Height - cropBox.Height) > 0.01;
            pages.Add(new PdfPageInfo(number, page.Width, page.Height, page.Rotation.Value, cropBoxDiffers ? cropBox : null, glyphs.Length > 0, glyphs.Length == 0, glyphs, mediaBox));
        }
        return new PdfDocumentInfo(sourcePath, document.NumberOfPages, pages);
    }, cancellationToken);
}
