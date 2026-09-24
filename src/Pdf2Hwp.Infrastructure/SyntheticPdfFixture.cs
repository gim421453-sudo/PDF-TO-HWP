using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace Pdf2Hwp.Infrastructure;

public static class SyntheticPdfFixture
{
    public static void CreateHundredPageA4(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var stream = File.Create(path);
        using var builder = new PdfDocumentBuilder(stream, disposeStream: false);
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        for (var i = 1; i <= 100; i++)
        {
            var page = builder.AddPage(595.276, 841.89);
            page.SetStrokeColor(30, 80, 140);
            page.DrawRectangle(new PdfPoint(24, 24), 547, 794, 0.6, false);
            page.DrawLine(new PdfPoint(36, 805), new PdfPoint(559, 805), 0.5);
            page.DrawLine(new PdfPoint(36, 36), new PdfPoint(559, 36), 0.5);
            page.AddText($"PAGE {i:000}", 18, new PdfPoint(42, 770), font);
            page.AddText($"MARKER-{i:000}-{(i * 7919):X}", 8, new PdfPoint(42, 748), font);
            page.DrawRectangle(new PdfPoint(530, 760), 20, 20, 0.6, false);
            page.DrawRectangle(new PdfPoint(42, 42), 12 + (i % 8), 12 + (i % 8), 0.6, false);
        }
        var bytes = builder.Build(); stream.Write(bytes);
    }
}
