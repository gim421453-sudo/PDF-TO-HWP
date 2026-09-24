using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Pdf2Hwp.Core;

namespace Pdf2Hwp.Infrastructure;

public sealed record HwpxBinaryResource(string Id, string PackagePath, string MimeType, byte[] Bytes, string Sha256, int PixelWidth, int PixelHeight);
public sealed record HwpxImagePlacement(string ObjectId, string ResourceId, long X, long Y, long Width, long Height, bool FullPage = false);
public enum HwpxImageFitMode { PreserveAspectRatio, Stretch, Contain, Cover }
public static class HwpxImageGeometry
{
    public static (long Width, long Height) CalculatePlacementPreservingAspectRatio(int sourcePixelWidth, int sourcePixelHeight, long requestedWidth)
    { if (sourcePixelWidth <= 0 || sourcePixelHeight <= 0 || requestedWidth <= 0) throw new ArgumentOutOfRangeException(); return (requestedWidth, (long)Math.Round(requestedWidth * (double)sourcePixelHeight / sourcePixelWidth, MidpointRounding.AwayFromZero)); }
    public static double RatioError(long width, long height, int sourcePixelWidth, int sourcePixelHeight) => Math.Abs((width / (double)height) - (sourcePixelWidth / (double)sourcePixelHeight)) / (sourcePixelWidth / (double)sourcePixelHeight);
}

public sealed class HwpxImageResourceWriter
{
    public HwpxBinaryResource Create(string sourcePath, string id, string mimeType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath); if (mimeType is not ("image/png" or "image/jpeg")) throw new NotSupportedException($"Unsupported image MIME: {mimeType}");
        var bytes = File.ReadAllBytes(sourcePath); if (bytes.Length == 0 || !IsSignatureValid(bytes, mimeType)) throw new InvalidDataException("Invalid image resource.");
        var ext = mimeType == "image/png" ? "png" : "jpg"; var dimensions = ReadDimensions(bytes, mimeType);
        return new HwpxBinaryResource(id, $"BinData/{id}.{ext}", mimeType, bytes, Convert.ToHexString(SHA256.HashData(bytes)), dimensions.Width, dimensions.Height);
    }
    internal static (int Width, int Height) ReadDimensions(byte[] b, string mime)
    { if (mime == "image/png" && b.Length >= 24) return (ReadInt32(b, 16), ReadInt32(b, 20)); if (mime == "image/jpeg") { for (var i = 2; i + 9 < b.Length;) { if (b[i] != 0xff) { i++; continue; } var marker = b[i + 1]; var len = (b[i + 2] << 8) | b[i + 3]; if (marker is >= 0xc0 and <= 0xc3 or >= 0xc5 and <= 0xc7 or >= 0xc9 and <= 0xcb or >= 0xcd and <= 0xcf) return ((b[i + 7] << 8) | b[i + 8], (b[i + 5] << 8) | b[i + 6]); if (len < 2) break; i += 2 + len; } } throw new InvalidDataException("Image dimensions could not be read."); }
    private static int ReadInt32(byte[] b, int o) => (b[o] << 24) | (b[o + 1] << 16) | (b[o + 2] << 8) | b[o + 3];
    private static bool IsSignatureValid(byte[] b, string mime) => mime == "image/png" ? b.Length >= 8 && b.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) : b.Length >= 3 && b[0] == 0xff && b[1] == 0xd8 && b[2] == 0xff;
}

public sealed class HwpxImagePackageWriter
{
    public async Task WriteAsync(PdfDocumentInfo document, string outputPath, HwpxBinaryResource resource, HwpxImagePlacement placement, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(outputPath) ?? throw new InvalidOperationException("Output path has no directory."); Directory.CreateDirectory(directory); var temporary = Path.Combine(directory, $".{Path.GetFileName(outputPath)}.{Guid.NewGuid():N}.tmp");
        try { await using (var stream = File.Create(temporary)) using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, false)) { Put(zip, "mimetype", "application/hwp+zip", CompressionLevel.NoCompression); Put(zip, "version.xml", HwpxPackageParts.Version, CompressionLevel.NoCompression); Put(zip, "Contents/header.xml", new HwpxHeaderWriter().Write(document.PageCount)); foreach (var page in document.Pages) { cancellationToken.ThrowIfCancellationRequested(); var section = new HwpxSectionWriter().Write(page); if (placement.FullPage) { section = section.Replace("header=\"4252\" footer=\"4252\" gutter=\"0\" left=\"5669\" right=\"5669\" top=\"4252\" bottom=\"4252\"", "header=\"0\" footer=\"0\" gutter=\"0\" left=\"0\" right=\"0\" top=\"0\" bottom=\"0\""); section = StripFullPageBodyText(section); } section = InsertIntoFirstRun(section, PicXml(placement, resource)); Put(zip, $"Contents/section{page.Number - 1}.xml", section); } Put(zip, resource.PackagePath, resource.Bytes, CompressionLevel.Optimal); Put(zip, "settings.xml", HwpxPackageParts.Settings); Put(zip, "META-INF/container.rdf", HwpxPackageParts.Rdf(document.PageCount)); Put(zip, "Contents/content.hpf", ImageManifest(document.PageCount, resource)); Put(zip, "META-INF/container.xml", HwpxPackageParts.Container); Put(zip, "META-INF/manifest.xml", HwpxPackageParts.Manifest); } cancellationToken.ThrowIfCancellationRequested(); File.Move(temporary, outputPath, true); } finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public async Task WritePageBackgroundAsync(PdfDocumentInfo document, string outputPath, HwpxBinaryResource resource, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(outputPath) ?? throw new InvalidOperationException("Output path has no directory.");
        Directory.CreateDirectory(directory);
        var temporary = Path.Combine(directory, $".{Path.GetFileName(outputPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = File.Create(temporary))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, false))
            {
                Put(zip, "mimetype", "application/hwp+zip", CompressionLevel.NoCompression);
                Put(zip, "version.xml", HwpxPackageParts.Version, CompressionLevel.NoCompression);
                Put(zip, "Contents/header.xml", new HwpxHeaderWriter().Write(document.PageCount, resource.Id));
                foreach (var page in document.Pages)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var section = new HwpxSectionWriter().Write(page);
                    section = section.Replace("borderFillIDRef=\"1\"", "borderFillIDRef=\"3\"", StringComparison.Ordinal);
                    Put(zip, $"Contents/section{page.Number - 1}.xml", section);
                }
                Put(zip, resource.PackagePath, resource.Bytes, CompressionLevel.Optimal);
                Put(zip, "settings.xml", HwpxPackageParts.Settings);
                Put(zip, "META-INF/container.rdf", HwpxPackageParts.Rdf(document.PageCount));
                Put(zip, "Contents/content.hpf", ImageManifest(document.PageCount, resource));
                Put(zip, "META-INF/container.xml", HwpxPackageParts.Container);
                Put(zip, "META-INF/manifest.xml", HwpxPackageParts.Manifest);
            }
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporary, outputPath, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public async Task WritePageBackgroundsAsync(PdfDocumentInfo document, string outputPath, IReadOnlyList<HwpxBinaryResource> resources, CancellationToken cancellationToken)
    {
        if (resources.Count != document.PageCount) throw new ArgumentException("One background resource is required per page.", nameof(resources));
        var directory = Path.GetDirectoryName(outputPath) ?? throw new InvalidOperationException("Output path has no directory.");
        Directory.CreateDirectory(directory);
        var temporary = Path.Combine(directory, $".{Path.GetFileName(outputPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = File.Create(temporary))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, false))
            {
                Put(zip, "mimetype", "application/hwp+zip", CompressionLevel.NoCompression);
                Put(zip, "version.xml", HwpxPackageParts.Version, CompressionLevel.NoCompression);
                Put(zip, "Contents/header.xml", new HwpxHeaderWriter().Write(document.PageCount, resources.Select(r => r.Id).ToArray()));
                foreach (var page in document.Pages)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var resource = resources[page.Number - 1];
                    var section = new HwpxSectionWriter().Write(page).Replace("pageBorderFill type=\"BOTH\" borderFillIDRef=\"1\"", $"pageBorderFill type=\"BOTH\" borderFillIDRef=\"{page.Number + 2}\"", StringComparison.Ordinal);
                    Put(zip, $"Contents/section{page.Number - 1}.xml", section);
                }
                foreach (var resource in resources) Put(zip, resource.PackagePath, resource.Bytes, CompressionLevel.Optimal);
                Put(zip, "settings.xml", HwpxPackageParts.Settings);
                Put(zip, "META-INF/container.rdf", HwpxPackageParts.Rdf(document.PageCount));
                Put(zip, "Contents/content.hpf", ImageManifest(document.PageCount, resources));
                Put(zip, "META-INF/container.xml", HwpxPackageParts.Container);
                Put(zip, "META-INF/manifest.xml", HwpxPackageParts.Manifest);
            }
            cancellationToken.ThrowIfCancellationRequested(); File.Move(temporary, outputPath, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    private static string InsertIntoFirstRun(string section, string picture) { var marker = "</hp:run>"; var index = section.IndexOf(marker, StringComparison.Ordinal); return index < 0 ? throw new InvalidDataException("Section has no run.") : section.Insert(index, picture); }
    private static string StripFullPageBodyText(string section) { var start = section.IndexOf("<hp:run charPrIDRef=\"0\"><hp:t>", StringComparison.Ordinal); if (start < 0) return section; var end = section.IndexOf("</hp:run>", start, StringComparison.Ordinal) + "</hp:run>".Length; section = section.Remove(start, end - start); var lines = section.IndexOf("<hp:linesegarray>", StringComparison.Ordinal); if (lines >= 0) { var linesEnd = section.IndexOf("</hp:linesegarray>", lines, StringComparison.Ordinal) + "</hp:linesegarray>".Length; section = section.Remove(lines, linesEnd - lines); } return section; }
    private static string ImageManifest(int pages, HwpxBinaryResource r) { var items = string.Concat(Enumerable.Range(0, pages).Select(i => $"<opf:item id=\"section{i}\" href=\"Contents/section{i}.xml\" media-type=\"application/xml\"/>")); var spine = string.Concat(Enumerable.Range(0, pages).Select(i => $"<opf:itemref idref=\"section{i}\" linear=\"yes\"/>")); var mime = r.MimeType == "image/jpeg" ? "image/jpg" : r.MimeType; return $"<?xml version=\"1.0\" encoding=\"UTF-8\"?><opf:package xmlns:opf=\"{HwpxNamespaces.Opf}\" version=\"\" unique-identifier=\"\" id=\"\"><opf:metadata><opf:title>PDF2HWP image compatibility sample</opf:title><opf:language>ko</opf:language></opf:metadata><opf:manifest><opf:item id=\"header\" href=\"Contents/header.xml\" media-type=\"application/xml\"/>{items}<opf:item id=\"settings\" href=\"settings.xml\" media-type=\"application/xml\"/><opf:item id=\"{r.Id}\" href=\"{r.PackagePath}\" media-type=\"{mime}\" isEmbeded=\"1\"/></opf:manifest><opf:spine><opf:itemref idref=\"header\" linear=\"yes\"/>{spine}</opf:spine></opf:package>"; }
    private static string ImageManifest(int pages, IReadOnlyList<HwpxBinaryResource> resources) { var items = string.Concat(Enumerable.Range(0, pages).Select(i => $"<opf:item id=\"section{i}\" href=\"Contents/section{i}.xml\" media-type=\"application/xml\"/>")); var spine = string.Concat(Enumerable.Range(0, pages).Select(i => $"<opf:itemref idref=\"section{i}\" linear=\"yes\"/>")); var imageItems = string.Concat(resources.Select(r => $"<opf:item id=\"{r.Id}\" href=\"{r.PackagePath}\" media-type=\"{(r.MimeType == "image/jpeg" ? "image/jpg" : r.MimeType)}\" isEmbeded=\"1\"/>")); return $"<?xml version=\"1.0\" encoding=\"UTF-8\"?><opf:package xmlns:opf=\"{HwpxNamespaces.Opf}\" version=\"\" unique-identifier=\"\" id=\"\"><opf:metadata><opf:title>PDF2HWP Visual Fidelity multi-page</opf:title><opf:language>ko</opf:language></opf:metadata><opf:manifest><opf:item id=\"header\" href=\"Contents/header.xml\" media-type=\"application/xml\"/>{items}<opf:item id=\"settings\" href=\"settings.xml\" media-type=\"application/xml\"/>{imageItems}</opf:manifest><opf:spine><opf:itemref idref=\"header\" linear=\"yes\"/>{spine}</opf:spine></opf:package>"; }
    private static string PicXml(HwpxImagePlacement p, HwpxBinaryResource r) { var nativeWidth = (long)Math.Round(r.PixelWidth * 64.87375d); var nativeHeight = (long)Math.Round(r.PixelHeight * 64.875d); var scaleX = p.Width / (double)nativeWidth; var scaleY = p.Height / (double)nativeHeight; var flow = p.FullPage ? 0 : 1; return $"<hp:pic id=\"{p.ObjectId}\" zOrder=\"0\" numberingType=\"PICTURE\" textWrap=\"SQUARE\" textFlow=\"BOTH_SIDES\" lock=\"0\" dropcapstyle=\"None\" href=\"\" groupLevel=\"0\" instid=\"1\" reverse=\"0\"><hp:offset x=\"{p.X}\" y=\"{p.Y}\"/><hp:orgSz width=\"{nativeWidth}\" height=\"{nativeHeight}\"/><hp:curSz width=\"{p.Width}\" height=\"{p.Height}\"/><hp:flip horizontal=\"0\" vertical=\"0\"/><hp:rotationInfo angle=\"0\" centerX=\"{p.Width/2}\" centerY=\"{p.Height/2}\" rotateimage=\"1\"/><hp:renderingInfo><hc:transMatrix xmlns:hc=\"http://www.hancom.co.kr/hwpml/2011/core\" e1=\"1\" e2=\"0\" e3=\"0\" e4=\"0\" e5=\"1\" e6=\"0\"/><hc:scaMatrix xmlns:hc=\"http://www.hancom.co.kr/hwpml/2011/core\" e1=\"{scaleX.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture)}\" e2=\"0\" e3=\"0\" e4=\"0\" e5=\"{scaleY.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture)}\" e6=\"0\"/><hc:rotMatrix xmlns:hc=\"http://www.hancom.co.kr/hwpml/2011/core\" e1=\"1\" e2=\"0\" e3=\"0\" e4=\"0\" e5=\"1\" e6=\"0\"/></hp:renderingInfo><hc:img xmlns:hc=\"http://www.hancom.co.kr/hwpml/2011/core\" binaryItemIDRef=\"{p.ResourceId}\" bright=\"0\" contrast=\"0\" effect=\"REAL_PIC\" alpha=\"0\"/><hp:imgRect><hc:pt0 xmlns:hc=\"http://www.hancom.co.kr/hwpml/2011/core\" x=\"0\" y=\"0\"/><hc:pt1 xmlns:hc=\"http://www.hancom.co.kr/hwpml/2011/core\" x=\"{nativeWidth}\" y=\"0\"/><hc:pt2 xmlns:hc=\"http://www.hancom.co.kr/hwpml/2011/core\" x=\"{nativeWidth}\" y=\"{nativeHeight}\"/><hc:pt3 xmlns:hc=\"http://www.hancom.co.kr/hwpml/2011/core\" x=\"0\" y=\"{nativeHeight}\"/></hp:imgRect><hp:imgClip left=\"0\" right=\"{r.PixelWidth * 75}\" top=\"0\" bottom=\"{r.PixelHeight * 75}\"/><hp:inMargin left=\"0\" right=\"0\" top=\"0\" bottom=\"0\"/><hp:imgDim dimwidth=\"{r.PixelWidth * 75}\" dimheight=\"{r.PixelHeight * 75}\"/><hp:effects/><hp:sz width=\"{p.Width}\" widthRelTo=\"ABSOLUTE\" height=\"{p.Height}\" heightRelTo=\"ABSOLUTE\" protect=\"0\"/><hp:pos treatAsChar=\"0\" affectLSpacing=\"0\" flowWithText=\"{flow}\" allowOverlap=\"1\" vertRelTo=\"PAPER\" horzRelTo=\"PAPER\" vertAlign=\"TOP\" horzAlign=\"LEFT\" vertOffset=\"{p.Y}\" horzOffset=\"{p.X}\"/><hp:outMargin left=\"0\" right=\"0\" top=\"0\" bottom=\"0\"/><hp:shapeComment>PDF2HWP image</hp:shapeComment></hp:pic>"; }
    private static void Put(ZipArchive z, string path, string value, CompressionLevel level = CompressionLevel.Optimal) => Put(z, path, Encoding.UTF8.GetBytes(value), level);
    private static void Put(ZipArchive z, string path, byte[] bytes, CompressionLevel level) { var e = z.CreateEntry(path, level); using var s = e.Open(); s.Write(bytes); }
}
