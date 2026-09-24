using Pdf2Hwp.Core;
using Pdf2Hwp.Infrastructure;
using System.Text;

var root = Path.Combine(Environment.CurrentDirectory, "artifacts");
var directory = Path.Combine(root, "compatibility-tests");
var fixtureDirectory = Path.Combine(root, "image-fixtures");
Directory.CreateDirectory(directory); Directory.CreateDirectory(fixtureDirectory);
var page = new PdfPageInfo(1, 595.275590551d, 841.88976378d, 0, null, true, false, Array.Empty<PdfGlyph>());
var document = new PdfDocumentInfo("image-geometry-compatibility-sample", 1, [page]);
var png = Path.Combine(fixtureDirectory, "diagnostic-800x400.png"); var jpeg = Path.Combine(fixtureDirectory, "diagnostic-800x400.jpg");
if (!File.Exists(png) || !File.Exists(jpeg)) throw new FileNotFoundException("Run the diagnostic fixture generator first.");
var resources = new HwpxImageResourceWriter(); var writer = new HwpxImagePackageWriter();
var width = HwpxObjectUnitConverter.FromMillimeters(100d).Value; var placement = HwpxImageGeometry.CalculatePlacementPreservingAspectRatio(800, 400, width);
await writer.WriteAsync(document, Path.Combine(directory, "15-image-png-100x50mm-golden-fixed.hwpx"), resources.Create(png, "image0", "image/png"), new HwpxImagePlacement("pic0", "image0", 1000, 1000, placement.Width, placement.Height), CancellationToken.None);
await writer.WriteAsync(document, Path.Combine(directory, "16-image-jpeg-100x50mm-golden-fixed.hwpx"), resources.Create(jpeg, "image0", "image/jpeg"), new HwpxImagePlacement("pic0", "image0", 1000, 1000, placement.Width, placement.Height), CancellationToken.None);
var inspector = new HwpxPackageInspector();
foreach (var name in new[] { "15-image-png-100x50mm-golden-fixed.hwpx", "16-image-jpeg-100x50mm-golden-fixed.hwpx" }) { var path = Path.Combine(directory, name); var result = inspector.Inspect(path); var reopen = inspector.Inspect(path); if (result.Status == ValidationStatus.Fail || reopen.Status == ValidationStatus.Fail) throw new InvalidDataException(string.Join("; ", result.Issues.Select(i => i.Code))); Console.WriteLine($"{path} PASS {new FileInfo(path).Length} bytes"); }

var pdfFixture = Path.Combine(root, "pdf-fixtures", "visual-fidelity-a4-portrait-1page.pdf"); Directory.CreateDirectory(Path.GetDirectoryName(pdfFixture)!); await File.WriteAllBytesAsync(pdfFixture, CreateVisualPdf());
var vf = new VisualFidelityConverter(new PdfPigAnalyzer(), new PdfiumPageRenderer(), writer, inspector);
var candidates = new[] { (1L, "20a"), (2L, "20b"), (5L, "20c"), (10L, "20d"), (20L, "20e") };
foreach (var (inset, label) in candidates) { var renderArtifact = Path.Combine(root, "visual-fidelity", $"{label}-a4-portrait-render-200dpi.png"); var vfResult = await vf.ConvertAsync(pdfFixture, Path.Combine(directory, $"{label}-visual-a4-inset-{inset}hu.hwpx"), new VisualFidelityOptions(RenderArtifactPath: renderArtifact, SafetyInsetHwpUnit: inset), CancellationToken.None); if (vfResult.Validation.Status == ValidationStatus.Fail || !vfResult.RenderHashPreserved) throw new InvalidDataException($"Visual Fidelity validation failed for {label}."); Console.WriteLine($"{vfResult.OutputPath} PASS {new FileInfo(vfResult.OutputPath).Length} bytes {vfResult.WidthPx}x{vfResult.HeightPx} inset={inset} hash={vfResult.RenderHashPreserved}"); }

var backgroundDocument = await new PdfPigAnalyzer().AnalyzeAsync(pdfFixture, CancellationToken.None);
var backgroundResource = new HwpxImageResourceWriter().Create(Path.Combine(root, "visual-fidelity", "19-a4-portrait-render-200dpi.png"), "image1", "image/png");
var backgroundPath = Path.Combine(directory, "21-visual-fidelity-a4-page-background.hwpx");
await writer.WritePageBackgroundAsync(backgroundDocument, backgroundPath, backgroundResource, CancellationToken.None);
var backgroundValidation = inspector.Inspect(backgroundPath);
var backgroundReopen = inspector.Inspect(backgroundPath);
if (backgroundValidation.Status == ValidationStatus.Fail || backgroundReopen.Status == ValidationStatus.Fail) throw new InvalidDataException("Page-background validation failed.");
Console.WriteLine($"{backgroundPath} {backgroundValidation.Status} reopen={backgroundReopen.Status} {new FileInfo(backgroundPath).Length} bytes");

var threePdf = Path.Combine(root, "pdf-fixtures", "visual-fidelity-a4-portrait-3pages.pdf");
await File.WriteAllBytesAsync(threePdf, CreateThreePagePdf());
var threeDoc = await new PdfPigAnalyzer().AnalyzeAsync(threePdf, CancellationToken.None);
var threeResources = new List<HwpxBinaryResource>();
var threeRenderer = new PdfiumPageRenderer();
for (var i = 1; i <= 3; i++) { var rp = Path.Combine(root, "visual-fidelity", $"23-page{i}-render-200dpi.png"); await threeRenderer.RenderAsync(new(threePdf, i, rp, 200), CancellationToken.None); threeResources.Add(new HwpxImageResourceWriter().Create(rp, $"image{i-1}", "image/png")); }
var sample23 = Path.Combine(directory, "23-visual-fidelity-a4-portrait-3pages-e2e.hwpx");
await writer.WritePageBackgroundsAsync(threeDoc, sample23, threeResources, CancellationToken.None);
var v23 = inspector.Inspect(sample23); var r23 = inspector.Inspect(sample23); if (v23.Status == ValidationStatus.Fail || r23.Status == ValidationStatus.Fail) throw new InvalidDataException("3-page background validation failed."); Console.WriteLine($"{sample23} {v23.Status} reopen={r23.Status} {new FileInfo(sample23).Length} bytes");

static byte[] CreateVisualPdf()
{
    const string content = "q 0 0 0 RG 3 w 14 14 567 813 re S 14 420 m 581 420 l S 297.5 14 m 297.5 827 l S 14 14 m 581 827 l S 581 14 m 14 827 l S 0.8 0.8 0.8 rg 40 500 515 90 re f BT /F1 24 Tf 55 650 Td (PDF2HWP VISUAL FIDELITY TEST) Tj /F1 16 Tf 0 -35 Td (TOP LEFT        TOP RIGHT) Tj 0 -35 Td (CENTER CROSS) Tj 0 -35 Td (BOTTOM LEFT     BOTTOM RIGHT) Tj 0 -35 Td (English Test  1234567890  symbols) Tj 0 -35 Td (Korean test) Tj ET 0 0 0 RG 3 w 250 340 95 95 re S 297.5 340 m 297.5 435 l S 250 387.5 m 345 387.5 l S Q";
    var objects = new[] { "<< /Type /Catalog /Pages 2 0 R >>", "<< /Type /Pages /Kids [3 0 R] /Count 1 >>", "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595.2756 841.8898] /CropBox [0 0 595.2756 841.8898] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>", $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream", "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>" };
    var sb = new StringBuilder("%PDF-1.4\n"); var offsets = new List<int> { 0 }; foreach (var (obj, index) in objects.Select((o, i) => (o, i))) { offsets.Add(Encoding.ASCII.GetByteCount(sb.ToString())); sb.Append(index + 1).Append(" 0 obj\n").Append(obj).Append("\nendobj\n"); } var xref = Encoding.ASCII.GetByteCount(sb.ToString()); sb.Append("xref\n0 6\n0000000000 65535 f \n"); foreach (var offset in offsets.Skip(1)) sb.Append(offset.ToString("D10")).Append(" 00000 n \n"); sb.Append("trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n").Append(xref).Append("\n%%EOF\n"); return Encoding.ASCII.GetBytes(sb.ToString());
}

static byte[] CreateThreePagePdf()
{
    const int pageCount = 3;
    var objects = new List<string> { $"<< /Type /Catalog /Pages 2 0 R >>" };
    var kids = string.Join(' ', Enumerable.Range(0, pageCount).Select(i => $"{3 + i * 3} 0 R")); objects.Add($"<< /Type /Pages /Kids [{kids}] /Count {pageCount} >>");
    for (var i = 0; i < pageCount; i++) { var content = $"q 0 0 0 RG 3 w 14 14 567 813 re S 0.8 0.8 0.8 rg {40 + i * 25} {500 - i * 70} 515 90 re f BT /F1 48 Tf 230 700 Td (PAGE {i + 1}) Tj /F1 18 Tf 0 -45 Td (P{i + 1} TL    P{i + 1} TR) Tj 0 -45 Td (P{i + 1} CENTER) Tj 0 -45 Td (P{i + 1} BL    P{i + 1} BR) Tj ET Q"; var cid = 4 + i * 3; objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595.2756 841.8898] /CropBox [0 0 595.2756 841.8898] /Rotate 0 /Resources << /Font << /F1 {5 + i * 3} 0 R >> >> /Contents {cid} 0 R >>"); objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream"); objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"); }
    var sb = new StringBuilder("%PDF-1.4\n"); var offsets = new List<int> { 0 }; for (var i = 0; i < objects.Count; i++) { offsets.Add(Encoding.ASCII.GetByteCount(sb.ToString())); sb.Append(i + 1).Append(" 0 obj\n").Append(objects[i]).Append("\nendobj\n"); } var xref = Encoding.ASCII.GetByteCount(sb.ToString()); sb.Append("xref\n0 ").Append(objects.Count + 1).Append("\n0000000000 65535 f \n"); foreach (var o in offsets.Skip(1)) sb.Append(o.ToString("D10")).Append(" 00000 n \n"); sb.Append("trailer\n<< /Size ").Append(objects.Count + 1).Append(" /Root 1 0 R >>\nstartxref\n").Append(xref).Append("\n%%EOF\n"); return Encoding.ASCII.GetBytes(sb.ToString());
}

