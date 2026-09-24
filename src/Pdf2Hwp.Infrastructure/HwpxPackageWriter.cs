using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using Pdf2Hwp.Core;

namespace Pdf2Hwp.Infrastructure;

public sealed class HwpxPackageWriter
{
    public async Task WriteAsync(PdfDocumentInfo document, string outputPath, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(outputPath) ?? throw new InvalidOperationException("Output path has no directory.");
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(outputPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = File.Create(temporaryPath))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, false))
            {
                Write(zip, "mimetype", "application/hwp+zip", CompressionLevel.NoCompression);
                Write(zip, "version.xml", HwpxPackageParts.Version, CompressionLevel.NoCompression);
                Write(zip, "Contents/header.xml", new HwpxHeaderWriter().Write(document.PageCount));
                var sectionWriter = new HwpxSectionWriter();
                foreach (var page in document.Pages) { cancellationToken.ThrowIfCancellationRequested(); Write(zip, $"Contents/section{page.Number - 1}.xml", sectionWriter.Write(page)); }
                Write(zip, "settings.xml", HwpxPackageParts.Settings);
                Write(zip, "META-INF/container.rdf", HwpxPackageParts.Rdf(document.PageCount));
                Write(zip, "Contents/content.hpf", new HwpxManifestWriter().Write(document.PageCount));
                Write(zip, "META-INF/container.xml", HwpxPackageParts.Container);
                Write(zip, "META-INF/manifest.xml", HwpxPackageParts.Manifest);
            }
            cancellationToken.ThrowIfCancellationRequested(); File.Move(temporaryPath, outputPath, true);
        }
        finally { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); }
    }
    private static void Write(ZipArchive zip, string path, string contents, CompressionLevel level = CompressionLevel.Optimal)
    { var entry = zip.CreateEntry(path, level); using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false)); writer.Write(contents); }
}

internal static class HwpxPackageParts
{
    internal const string Container = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><ocf:container xmlns:ocf=\"urn:oasis:names:tc:opendocument:xmlns:container\"><ocf:rootfiles><ocf:rootfile full-path=\"Contents/content.hpf\" media-type=\"application/hwpml-package+xml\"/></ocf:rootfiles></ocf:container>";
    internal const string Manifest = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><odf:manifest xmlns:odf=\"urn:oasis:names:tc:opendocument:xmlns:manifest:1.0\"/>";
    internal const string Version = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><hv:HCFVersion xmlns:hv=\"http://www.hancom.co.kr/hwpml/2011/version\" tagetApplication=\"WORDPROCESSOR\" major=\"5\" minor=\"1\" micro=\"1\" buildNumber=\"0\" os=\"1\" xmlVersion=\"1.5\" application=\"PDF2HWP\" appVersion=\"1.0\"/>";
    internal const string Settings = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><ha:HWPApplicationSetting xmlns:ha=\"http://www.hancom.co.kr/hwpml/2011/app\"><ha:CaretPosition listIDRef=\"0\" paraIDRef=\"0\" pos=\"0\"/></ha:HWPApplicationSetting>";
    internal static string Rdf(int pageCount)
    {
        var b = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?><rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">");
        Add(b, "Contents/header.xml", "HeaderFile"); foreach (var i in Enumerable.Range(0, pageCount)) Add(b, $"Contents/section{i}.xml", "SectionFile");
        return b.Append("<rdf:Description rdf:about=\"\"><rdf:type rdf:resource=\"http://www.hancom.co.kr/hwpml/2016/meta/pkg#Document\"/></rdf:Description></rdf:RDF>").ToString();
    }
    private static void Add(StringBuilder b, string part, string type) => b.Append($"<rdf:Description rdf:about=\"\"><pkg:hasPart xmlns:pkg=\"http://www.hancom.co.kr/hwpml/2016/meta/pkg#\" rdf:resource=\"{part}\"/></rdf:Description><rdf:Description rdf:about=\"{part}\"><rdf:type rdf:resource=\"http://www.hancom.co.kr/hwpml/2016/meta/pkg#{type}\"/></rdf:Description>");
}

public sealed class HwpxManifestWriter
{
    public string Write(int pageCount)
    {
        var items = string.Concat(Enumerable.Range(0, pageCount).Select(i => $"<opf:item id=\"section{i}\" href=\"Contents/section{i}.xml\" media-type=\"application/xml\"/>"));
        var spine = string.Concat(Enumerable.Range(0, pageCount).Select(i => $"<opf:itemref idref=\"section{i}\" linear=\"yes\"/>"));
        return $"<?xml version=\"1.0\" encoding=\"UTF-8\"?><opf:package xmlns:opf=\"{HwpxNamespaces.Opf}\" version=\"\" unique-identifier=\"\" id=\"\"><opf:metadata><opf:title>PDF2HWP converted document</opf:title><opf:language>ko</opf:language></opf:metadata><opf:manifest><opf:item id=\"header\" href=\"Contents/header.xml\" media-type=\"application/xml\"/>{items}<opf:item id=\"settings\" href=\"settings.xml\" media-type=\"application/xml\"/></opf:manifest><opf:spine><opf:itemref idref=\"header\" linear=\"yes\"/>{spine}</opf:spine></opf:package>";
    }
}

public sealed class HwpxHeaderWriter
{
    public string Write(int sectionCount)
    {
        const string f = "<hh:font id=\"0\" face=\"Malgun Gothic\" type=\"TTF\" isEmbedded=\"0\"><hh:typeInfo familyType=\"FCAT_GOTHIC\" weight=\"6\" proportion=\"4\" contrast=\"0\" strokeVariation=\"1\" armStyle=\"1\" letterform=\"1\" midline=\"1\" xHeight=\"1\"/></hh:font>";
        var fonts = string.Concat(new[] { "HANGUL", "LATIN", "HANJA", "JAPANESE", "OTHER", "SYMBOL", "USER" }.Select(lang => $"<hh:fontface lang=\"{lang}\" fontCnt=\"1\">{f}</hh:fontface>"));
        const string borderEdges = "<hh:slash type=\"NONE\" Crooked=\"0\" isCounter=\"0\"/><hh:backSlash type=\"NONE\" Crooked=\"0\" isCounter=\"0\"/><hh:leftBorder type=\"NONE\" width=\"0.1 mm\" color=\"#000000\"/><hh:rightBorder type=\"NONE\" width=\"0.1 mm\" color=\"#000000\"/><hh:topBorder type=\"NONE\" width=\"0.1 mm\" color=\"#000000\"/><hh:bottomBorder type=\"NONE\" width=\"0.1 mm\" color=\"#000000\"/><hh:diagonal type=\"SOLID\" width=\"0.1 mm\" color=\"#000000\"/>";
        var borders = $"<hh:borderFills itemCnt=\"2\"><hh:borderFill id=\"1\" threeD=\"0\" shadow=\"0\" centerLine=\"NONE\" breakCellSeparateLine=\"0\">{borderEdges}</hh:borderFill><hh:borderFill id=\"2\" threeD=\"0\" shadow=\"0\" centerLine=\"NONE\" breakCellSeparateLine=\"0\">{borderEdges}<hc:fillBrush><hc:winBrush faceColor=\"none\" hatchColor=\"#999999\" alpha=\"0\"/></hc:fillBrush></hh:borderFill></hh:borderFills>";
        const string charPr = "<hh:charProperties itemCnt=\"1\"><hh:charPr id=\"0\" height=\"1000\" textColor=\"#000000\" shadeColor=\"none\" useFontSpace=\"0\" useKerning=\"0\" symMark=\"NONE\" borderFillIDRef=\"2\"><hh:fontRef hangul=\"0\" latin=\"0\" hanja=\"0\" japanese=\"0\" other=\"0\" symbol=\"0\" user=\"0\"/><hh:ratio hangul=\"100\" latin=\"100\" hanja=\"100\" japanese=\"100\" other=\"100\" symbol=\"100\" user=\"100\"/><hh:spacing hangul=\"0\" latin=\"0\" hanja=\"0\" japanese=\"0\" other=\"0\" symbol=\"0\" user=\"0\"/><hh:relSz hangul=\"100\" latin=\"100\" hanja=\"100\" japanese=\"100\" other=\"100\" symbol=\"100\" user=\"100\"/><hh:offset hangul=\"0\" latin=\"0\" hanja=\"0\" japanese=\"0\" other=\"0\" symbol=\"0\" user=\"0\"/><hh:underline type=\"NONE\" shape=\"SOLID\" color=\"#000000\"/><hh:strikeout shape=\"NONE\" color=\"#000000\"/><hh:outline type=\"NONE\"/><hh:shadow type=\"NONE\" color=\"#C0C0C0\" offsetX=\"10\" offsetY=\"10\"/></hh:charPr></hh:charProperties>";
        const string paraPr = "<hh:paraProperties itemCnt=\"1\"><hh:paraPr id=\"0\" tabPrIDRef=\"0\" condense=\"0\" fontLineHeight=\"0\" snapToGrid=\"1\" suppressLineNumbers=\"0\" checked=\"0\" textDir=\"LTR\"><hh:align horizontal=\"JUSTIFY\" vertical=\"BASELINE\"/><hh:heading type=\"NONE\" idRef=\"0\" level=\"0\"/><hh:breakSetting breakLatinWord=\"KEEP_WORD\" breakNonLatinWord=\"KEEP_WORD\" widowOrphan=\"0\" keepWithNext=\"0\" keepLines=\"0\" pageBreakBefore=\"0\" lineWrap=\"BREAK\"/><hh:autoSpacing eAsianEng=\"0\" eAsianNum=\"0\"/><hh:margin><hc:intent value=\"0\" unit=\"HWPUNIT\"/><hc:left value=\"0\" unit=\"HWPUNIT\"/><hc:right value=\"0\" unit=\"HWPUNIT\"/><hc:prev value=\"0\" unit=\"HWPUNIT\"/><hc:next value=\"0\" unit=\"HWPUNIT\"/></hh:margin><hh:lineSpacing type=\"PERCENT\" value=\"160\" unit=\"HWPUNIT\"/><hh:border borderFillIDRef=\"2\" offsetLeft=\"0\" offsetRight=\"0\" offsetTop=\"0\" offsetBottom=\"0\" connect=\"0\" ignoreMargin=\"0\"/></hh:paraPr></hh:paraProperties>";
        return $"<?xml version=\"1.0\" encoding=\"UTF-8\"?><hh:head xmlns:hh=\"{HwpxNamespaces.Head}\" xmlns:hc=\"{HwpxNamespaces.Core}\" version=\"1.5\" secCnt=\"{sectionCount}\"><hh:beginNum page=\"1\" footnote=\"1\" endnote=\"1\" pic=\"1\" tbl=\"1\" equation=\"1\"/><hh:refList><hh:fontfaces itemCnt=\"7\">{fonts}</hh:fontfaces>{borders}{charPr}{paraPr}<hh:styles itemCnt=\"1\"><hh:style id=\"0\" type=\"PARA\" name=\"Default\" engName=\"Default\" paraPrIDRef=\"0\" charPrIDRef=\"0\" nextStyleIDRef=\"0\" langID=\"1042\" lockForm=\"0\"/></hh:styles></hh:refList><hh:compatibleDocument targetProgram=\"HWP201X\"><hh:layoutCompatibility/></hh:compatibleDocument></hh:head>";
    }

    public string Write(int sectionCount, string backgroundResourceId)
    {
        var xml = Write(sectionCount);
        const string marker = "</hh:borderFill></hh:borderFills>";
        var fill = $"<hh:borderFill id=\"3\" threeD=\"0\" shadow=\"0\" centerLine=\"NONE\" breakCellSeparateLine=\"0\"><hh:slash type=\"NONE\" Crooked=\"0\" isCounter=\"0\"/><hh:backSlash type=\"NONE\" Crooked=\"0\" isCounter=\"0\"/><hh:leftBorder type=\"NONE\" width=\"0.1 mm\" color=\"#000000\"/><hh:rightBorder type=\"NONE\" width=\"0.1 mm\" color=\"#000000\"/><hh:topBorder type=\"NONE\" width=\"0.1 mm\" color=\"#000000\"/><hh:bottomBorder type=\"NONE\" width=\"0.1 mm\" color=\"#000000\"/><hh:diagonal type=\"SOLID\" width=\"0.1 mm\" color=\"#000000\"/><hc:fillBrush><hc:imgBrush mode=\"TOTAL\"><hc:img binaryItemIDRef=\"{backgroundResourceId}\" bright=\"0\" contrast=\"0\" effect=\"REAL_PIC\" alpha=\"0\"/></hc:imgBrush></hc:fillBrush></hh:borderFill>";
        xml = xml.Replace("itemCnt=\"2\"><hh:borderFill", "itemCnt=\"3\"><hh:borderFill", StringComparison.Ordinal);
        return xml.Replace(marker, "</hh:borderFill>" + fill + "</hh:borderFills>", StringComparison.Ordinal);
    }
    public string Write(int sectionCount, IReadOnlyList<string> backgroundResourceIds)
    {
        var xml = Write(sectionCount);
        xml = xml.Replace("itemCnt=\"2\"><hh:borderFill", $"itemCnt=\"{2 + backgroundResourceIds.Count}\"><hh:borderFill", StringComparison.Ordinal);
        var fills = string.Concat(backgroundResourceIds.Select((id, i) => $"<hh:borderFill id=\"{i + 3}\" threeD=\"0\" shadow=\"0\" centerLine=\"NONE\" breakCellSeparateLine=\"0\"><hh:slash type=\"NONE\" Crooked=\"0\" isCounter=\"0\"/><hh:backSlash type=\"NONE\" Crooked=\"0\" isCounter=\"0\"/><hh:leftBorder type=\"NONE\" width=\"0.1 mm\" color=\"#000000\"/><hh:rightBorder type=\"NONE\" width=\"0.1 mm\" color=\"#000000\"/><hh:topBorder type=\"NONE\" width=\"0.1 mm\" color=\"#000000\"/><hh:bottomBorder type=\"NONE\" width=\"0.1 mm\" color=\"#000000\"/><hh:diagonal type=\"SOLID\" width=\"0.1 mm\" color=\"#000000\"/><hc:fillBrush><hc:imgBrush mode=\"TOTAL\"><hc:img binaryItemIDRef=\"{id}\" bright=\"0\" contrast=\"0\" effect=\"REAL_PIC\" alpha=\"0\"/></hc:imgBrush></hc:fillBrush></hh:borderFill>"));
        return xml.Replace("</hh:borderFill></hh:borderFills>", "</hh:borderFill>" + fills + "</hh:borderFills>", StringComparison.Ordinal);
    }
}

public sealed class HwpxSectionWriter
{
    public string Write(PdfPageInfo page)
    {
        using var text = new StringWriter(CultureInfo.InvariantCulture); using var writer = XmlWriter.Create(text, new XmlWriterSettings { OmitXmlDeclaration = true });
        var ids = new HwpxIdAllocator(); var styles = new HwpxStyleRegistry(); var layout = HwpxUnitConverter.FromPdfPage(page);
        var values = string.Concat(page.Glyphs.Select(g => g.Text)).Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
        writer.WriteStartElement("hs", "sec", HwpxNamespaces.Section); writer.WriteAttributeString("xmlns", "hp", null, HwpxNamespaces.Paragraph); writer.WriteAttributeString("xmlns", "hc", null, HwpxNamespaces.Core);
        for (var i = 0; i < values.Length; i++) WriteParagraph(writer, ids.Next(), styles, layout, values[i], i, i == 0);
        writer.WriteEndElement(); writer.Flush(); return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + text;
    }
    private static void WriteParagraph(XmlWriter w, int id, HwpxStyleRegistry styles, HwpxPageProperties layout, string value, int line, bool section)
    {
        w.WriteStartElement("hp", "p", HwpxNamespaces.Paragraph); foreach (var pair in new[] { ("id", id.ToString(CultureInfo.InvariantCulture)), ("paraPrIDRef", "0"), ("styleIDRef", "0"), ("pageBreak", "0"), ("columnBreak", "0"), ("merged", "0") }) w.WriteAttributeString(pair.Item1, pair.Item2);
        if (section) { w.WriteStartElement("hp", "run", HwpxNamespaces.Paragraph); w.WriteAttributeString("charPrIDRef", "0"); WriteSectionProperties(w, layout); w.WriteStartElement("hp", "ctrl", HwpxNamespaces.Paragraph); Empty(w, "colPr", ("id", ""), ("type", "NEWSPAPER"), ("layout", "LEFT"), ("colCount", "1"), ("sameSz", "1"), ("sameGap", "0")); w.WriteEndElement(); w.WriteEndElement(); }
        new HwpxTextRunWriter(styles).Write(w, value); w.WriteStartElement("hp", "linesegarray", HwpxNamespaces.Paragraph); Empty(w, "lineseg", ("textpos", "0"), ("vertpos", (line * 1600).ToString(CultureInfo.InvariantCulture)), ("vertsize", "1000"), ("textheight", "1000"), ("baseline", "850"), ("spacing", "600"), ("horzpos", "0"), ("horzsize", Math.Max(1, layout.Width.Value - layout.Margins.Left.Value - layout.Margins.Right.Value).ToString(CultureInfo.InvariantCulture)), ("flags", "393216")); w.WriteEndElement(); w.WriteEndElement();
    }
    private static void WriteSectionProperties(XmlWriter w, HwpxPageProperties x)
    {
        w.WriteStartElement("hp", "secPr", HwpxNamespaces.Paragraph); foreach (var pair in new[] { ("id", ""), ("textDirection", "HORIZONTAL"), ("spaceColumns", "1134"), ("tabStop", "8000"), ("tabStopVal", "4000"), ("tabStopUnit", "HWPUNIT"), ("outlineShapeIDRef", "1"), ("memoShapeIDRef", "1"), ("textVerticalWidthHead", "0"), ("masterPageCnt", "0") }) w.WriteAttributeString(pair.Item1, pair.Item2);
        Empty(w, "grid", ("lineGrid", "0"), ("charGrid", "0"), ("wonggojiFormat", "0")); Empty(w, "startNum", ("pageStartsOn", "BOTH"), ("page", "0"), ("pic", "0"), ("tbl", "0"), ("equation", "0")); Empty(w, "visibility", ("hideFirstHeader", "0"), ("hideFirstFooter", "0"), ("hideFirstMasterPage", "0"), ("border", "SHOW_ALL"), ("fill", "SHOW_ALL"), ("hideFirstPageNum", "0"), ("hideFirstEmptyLine", "0"), ("showLineNumber", "0")); Empty(w, "lineNumberShape", ("restartType", "0"), ("countBy", "0"), ("distance", "0"), ("startNumber", "0"));
        w.WriteStartElement("hp", "pagePr", HwpxNamespaces.Paragraph); w.WriteAttributeString("landscape", x.Landscape ? "NARROWLY" : "WIDELY"); w.WriteAttributeString("width", x.Width.Value.ToString(CultureInfo.InvariantCulture)); w.WriteAttributeString("height", x.Height.Value.ToString(CultureInfo.InvariantCulture)); w.WriteAttributeString("gutterType", "LEFT_ONLY"); Empty(w, "margin", ("header", x.Margins.Header.Value.ToString(CultureInfo.InvariantCulture)), ("footer", x.Margins.Footer.Value.ToString(CultureInfo.InvariantCulture)), ("gutter", "0"), ("left", x.Margins.Left.Value.ToString(CultureInfo.InvariantCulture)), ("right", x.Margins.Right.Value.ToString(CultureInfo.InvariantCulture)), ("top", x.Margins.Top.Value.ToString(CultureInfo.InvariantCulture)), ("bottom", x.Margins.Bottom.Value.ToString(CultureInfo.InvariantCulture))); w.WriteEndElement();
        foreach (var type in new[] { "BOTH", "EVEN", "ODD" }) { w.WriteStartElement("hp", "pageBorderFill", HwpxNamespaces.Paragraph); foreach (var p in new[] { ("type", type), ("borderFillIDRef", "1"), ("textBorder", "PAPER"), ("headerInside", "0"), ("footerInside", "0"), ("fillArea", "PAPER") }) w.WriteAttributeString(p.Item1, p.Item2); Empty(w, "offset", ("left", "1417"), ("right", "1417"), ("top", "1417"), ("bottom", "1417")); w.WriteEndElement(); } w.WriteEndElement();
    }
    private static void Empty(XmlWriter w, string name, params (string, string)[] attributes) { w.WriteStartElement("hp", name, HwpxNamespaces.Paragraph); foreach (var (attributeName, value) in attributes) w.WriteAttributeString(attributeName, value); w.WriteEndElement(); }
}

public sealed class HwpxImageWriter(HwpxResourceRegistry resources) { public string Register(string sourcePath) => resources.AddImage(sourcePath); }
public sealed class HwpxTableWriter { }
