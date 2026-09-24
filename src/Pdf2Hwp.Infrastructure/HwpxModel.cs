using System.Xml;

namespace Pdf2Hwp.Infrastructure;

public sealed class HwpxIdAllocator
{
    private int _next;
    public int Next() => Interlocked.Increment(ref _next) - 1;
}

public sealed class HwpxStyleRegistry
{
    public int DefaultFontId { get; } = 0;
    public int DefaultCharPropertyId { get; } = 0;
    public int DefaultParagraphPropertyId { get; } = 0;
}

public sealed class HwpxResourceRegistry
{
    private readonly Dictionary<string, string> _resources = new(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, string> Resources => _resources;
    public string AddImage(string sourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        return _resources.TryGetValue(sourcePath, out var existing) ? existing : _resources[sourcePath] = $"BinData/image{_resources.Count}.png";
    }
}

internal static class HwpxNamespaces
{
    internal const string Head = "http://www.hancom.co.kr/hwpml/2011/head";
    internal const string Section = "http://www.hancom.co.kr/hwpml/2011/section";
    internal const string Paragraph = "http://www.hancom.co.kr/hwpml/2011/paragraph";
    internal const string Core = "http://www.hancom.co.kr/hwpml/2011/core";
    internal const string Opf = "http://www.idpf.org/2007/opf/";
}

public sealed class HwpxTextRunWriter(HwpxStyleRegistry styles)
{
    public void Write(XmlWriter writer, string text)
    {
        writer.WriteStartElement("hp", "run", HwpxNamespaces.Paragraph); writer.WriteAttributeString("charPrIDRef", styles.DefaultCharPropertyId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        writer.WriteElementString("hp", "t", HwpxNamespaces.Paragraph, text); writer.WriteEndElement();
    }
}

public sealed class HwpxParagraphWriter(HwpxIdAllocator ids, HwpxStyleRegistry styles)
{
    public void Write(XmlWriter writer, string text)
    {
        writer.WriteStartElement("hp", "p", HwpxNamespaces.Paragraph); writer.WriteAttributeString("id", ids.Next().ToString(System.Globalization.CultureInfo.InvariantCulture)); writer.WriteAttributeString("paraPrIDRef", styles.DefaultParagraphPropertyId.ToString(System.Globalization.CultureInfo.InvariantCulture)); writer.WriteAttributeString("styleIDRef", "0");
        new HwpxTextRunWriter(styles).Write(writer, text); writer.WriteEndElement();
    }
}
