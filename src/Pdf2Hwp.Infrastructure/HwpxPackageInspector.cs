using System.IO.Compression;
using System.Xml.Linq;
using System.Xml;
using Pdf2Hwp.Core;

namespace Pdf2Hwp.Infrastructure;

public sealed class HwpxPackageInspector : IHwpxPackageInspector
{
    private static readonly string[] Required = ["mimetype", "version.xml", "settings.xml", "META-INF/container.xml", "META-INF/container.rdf", "META-INF/manifest.xml", "Contents/content.hpf", "Contents/header.xml"];
    public ValidationReport Inspect(string hwpxPath)
    {
        var issues = new List<ValidationIssue>();
        XNamespace hh = "http://www.hancom.co.kr/hwpml/2011/head";
        try
        {
            using var zip = ZipFile.OpenRead(hwpxPath);
            var entries = zip.Entries.ToArray();
            if (entries.Length == 0 || entries[0].FullName != "mimetype") issues.Add(new ValidationIssue(ValidationStatus.Fail, null, "HWPX_MIMETYPE_NOT_FIRST", "mimetype must be the first ZIP entry."));
            var mimetype = zip.GetEntry("mimetype");
            if (mimetype is not null)
            {
                using var reader = new StreamReader(mimetype.Open(), System.Text.Encoding.UTF8, false);
                if (reader.ReadToEnd() != "application/hwp+zip") issues.Add(new ValidationIssue(ValidationStatus.Fail, null, "HWPX_MIMETYPE_INVALID", "mimetype must be exactly application/hwp+zip."));
            }
            foreach (var item in Required.Where(item => zip.GetEntry(item) is null))
                issues.Add(new ValidationIssue(ValidationStatus.Fail, null, "HWPX_REQUIRED_PART_MISSING", item));
            var content = zip.GetEntry("Contents/content.hpf");
            XDocument? headerDocument = null;
            var headerEntry = zip.GetEntry("Contents/header.xml");
            if (headerEntry is not null)
            {
                try { using var headerStream = headerEntry.Open(); headerDocument = XDocument.Load(headerStream); }
                catch (XmlException) { issues.Add(new ValidationIssue(ValidationStatus.Fail, null, "HWPX_HEADER_XML_INVALID", headerEntry.FullName)); }
            }
            if (headerDocument is not null)
            {
                XNamespace hc = "http://www.hancom.co.kr/hwpml/2011/core";
                foreach (var img in headerDocument.Descendants(hc + "imgBrush").Elements(hc + "img"))
                {
                    var id = (string?)img.Attribute("binaryItemIDRef");
                    if (string.IsNullOrWhiteSpace(id) || (zip.GetEntry("BinData/" + id + ".png") is null && zip.GetEntry("BinData/" + id + ".jpg") is null))
                        issues.Add(new ValidationIssue(ValidationStatus.Fail, null, "HWPX_PAGE_BACKGROUND_REFERENCE_INVALID", id ?? "missing id"));
                }
            }
            if (content is not null)
            {
                using var stream = content.Open();
                var manifest = XDocument.Load(stream);
                XNamespace opf = "http://www.idpf.org/2007/opf/";
                var itemIds = manifest.Descendants(opf + "item").Select(item => (string?)item.Attribute("id")).Where(id => id is not null).Cast<string>().ToHashSet(StringComparer.Ordinal);
                var resourceItems = manifest.Descendants(opf + "item").Where(item => ((string?)item.Attribute("media-type"))?.StartsWith("image/", StringComparison.Ordinal) is true).ToDictionary(item => (string)item.Attribute("id")!, item => (string?)item.Attribute("href"), StringComparer.Ordinal);
                foreach (var resource in resourceItems)
                {
                    var manifestItem = manifest.Descendants(opf + "item").FirstOrDefault(item => (string?)item.Attribute("id") == resource.Key);
                    if ((string?)manifestItem?.Attribute("isEmbeded") is not ("1" or "true")) issues.Add(new ValidationIssue(ValidationStatus.Fail, null, "HWPX_IMAGE_NOT_EMBEDDED", resource.Key));
                    if (resource.Value is null || zip.GetEntry(resource.Value) is null) issues.Add(new ValidationIssue(ValidationStatus.Fail, null, "HWPX_IMAGE_RESOURCE_MISSING", resource.Key));
                    if (resource.Value is not null && !resource.Value.StartsWith("BinData/", StringComparison.Ordinal)) issues.Add(new ValidationIssue(ValidationStatus.Fail, null, "HWPX_IMAGE_PATH_INVALID", resource.Value));
                }
                foreach (var item in manifest.Descendants(opf + "item"))
                {
                    var href = (string?)item.Attribute("href");
                    if (string.IsNullOrWhiteSpace(href) || zip.GetEntry(href) is null) issues.Add(new ValidationIssue(ValidationStatus.Fail, null, "HWPX_MANIFEST_TARGET_MISSING", href ?? "missing href"));
                }
                foreach (var reference in manifest.Descendants(opf + "itemref"))
                {
                    var idref = (string?)reference.Attribute("idref");
                    if (string.IsNullOrWhiteSpace(idref) || !itemIds.Contains(idref)) issues.Add(new ValidationIssue(ValidationStatus.Fail, null, "HWPX_SPINE_REFERENCE_INVALID", idref ?? "missing idref"));
                }
                if (!manifest.Descendants(opf + "item").Any(item => ((string?)item.Attribute("href"))?.StartsWith("Contents/section", StringComparison.Ordinal) is true)) issues.Add(new ValidationIssue(ValidationStatus.Fail, null, "HWPX_NO_SECTIONS", "No section part exists."));
            }
            foreach (var entry in zip.Entries.Where(entry => entry.FullName.StartsWith("Contents/section", StringComparison.Ordinal) && entry.FullName.EndsWith(".xml", StringComparison.Ordinal)))
            {
                try
                {
                    using var stream = entry.Open(); var section = XDocument.Load(stream);
                    XNamespace hp = "http://www.hancom.co.kr/hwpml/2011/paragraph";
                    if (section.Root?.Name != XName.Get("sec", "http://www.hancom.co.kr/hwpml/2011/section")) issues.Add(new ValidationIssue(ValidationStatus.Fail, null, "HWPX_SECTION_ROOT_INVALID", entry.FullName));
                    var firstParagraph = section.Descendants(hp + "p").FirstOrDefault();
                    if (firstParagraph?.Element(hp + "run")?.Element(hp + "secPr") is null) issues.Add(new ValidationIssue(ValidationStatus.Fail, null, "HWPX_SECTION_PROPERTIES_MISSING", entry.FullName));
                    foreach (var paragraph in section.Descendants(hp + "p"))
                    {
                        var paraRef = (string?)paragraph.Attribute("paraPrIDRef");
                        var styleRef = (string?)paragraph.Attribute("styleIDRef");
                        if (paraRef != "0" || styleRef != "0") issues.Add(new ValidationIssue(ValidationStatus.Fail, null, "HWPX_PARAGRAPH_REFERENCE_INVALID", entry.FullName));
                        if (paragraph.Descendants(hp + "run").Any(run => (string?)run.Attribute("charPrIDRef") != "0")) issues.Add(new ValidationIssue(ValidationStatus.Fail, null, "HWPX_RUN_REFERENCE_INVALID", entry.FullName));
                        if (headerDocument is not null)
                        {
                            var paraDefinition = headerDocument.Descendants(hh + "paraPr").FirstOrDefault(item => (string?)item.Attribute("id") == paraRef);
                            var borderId = (string?)paraDefinition?.Element(hh + "border")?.Attribute("borderFillIDRef");
                            var borderDefinition = borderId is null ? null : headerDocument.Descendants(hh + "borderFill").FirstOrDefault(item => (string?)item.Attribute("id") == borderId);
                            if (borderDefinition is null) issues.Add(new ValidationIssue(ValidationStatus.Fail, null, "HWPX_PARAGRAPH_BORDER_REFERENCE_INVALID", entry.FullName));
                            else if (borderDefinition.Elements().Where(element => element.Name.LocalName.EndsWith("Border", StringComparison.Ordinal)).Any(element => (string?)element.Attribute("type") != "NONE")) issues.Add(new ValidationIssue(ValidationStatus.Fail, null, "HWPX_VISIBLE_PARAGRAPH_BORDER", entry.FullName));
                        }
                    }
                    var pagePr = section.Descendants(hp + "pagePr").FirstOrDefault();
                    if (pagePr is null || !long.TryParse((string?)pagePr.Attribute("width"), out var width) || !long.TryParse((string?)pagePr.Attribute("height"), out var height) || width <= 0 || height <= 0) issues.Add(new ValidationIssue(ValidationStatus.Fail, null, "HWPX_PAGE_PROPERTIES_INVALID", entry.FullName));
                    else
                    {
                        var orientation = (string?)pagePr.Attribute("landscape");
                        var expected = width < height ? "WIDELY" : width > height ? "NARROWLY" : orientation;
                        if (orientation != expected) issues.Add(new ValidationIssue(ValidationStatus.Fail, null, "HWPX_PAGE_ORIENTATION_INVALID", entry.FullName));
                    }
                    XNamespace hc = "http://www.hancom.co.kr/hwpml/2011/core";
                    foreach (var image in section.Descendants(hc + "img"))
                    {
                        var resourceId = (string?)image.Attribute("binaryItemIDRef");
                        if (string.IsNullOrWhiteSpace(resourceId) || !(zip.GetEntry("BinData/" + resourceId + ".png") is not null || zip.GetEntry("BinData/" + resourceId + ".jpg") is not null)) issues.Add(new ValidationIssue(ValidationStatus.Fail, null, "HWPX_IMAGE_REFERENCE_INVALID", entry.FullName));
                        var picture = image.Parent;
                        if (picture is null || picture.Name != hp + "pic" || picture.Parent?.Name != hp + "run") issues.Add(new ValidationIssue(ValidationStatus.Fail, null, "HWPX_IMAGE_ANCHOR_INVALID", entry.FullName));
                        var sequence = picture?.Elements().Select(element => element.Name.LocalName).ToArray() ?? Array.Empty<string>();
                        var requiredSequence = new[] { "offset", "orgSz", "curSz", "flip", "rotationInfo", "renderingInfo", "img", "imgRect", "imgClip", "inMargin", "imgDim", "effects", "sz", "pos", "outMargin", "shapeComment" };
                        var sequenceIndex = -1;
                        foreach (var child in sequence) { var childIndex = Array.IndexOf(requiredSequence, child); if (childIndex < 0 || childIndex < sequenceIndex) { issues.Add(new ValidationIssue(ValidationStatus.Fail, null, "HWPX_IMAGE_CHILD_SEQUENCE_INVALID", entry.FullName)); break; } sequenceIndex = childIndex; }
                        foreach (var requiredChild in new[] { "sz", "pos", "outMargin", "offset", "orgSz", "curSz", "imgRect", "imgClip", "imgDim", "img" }) if (!sequence.Contains(requiredChild, StringComparer.Ordinal)) issues.Add(new ValidationIssue(ValidationStatus.Fail, null, "HWPX_IMAGE_CHILD_MISSING", requiredChild));
                        var size = picture?.Elements(hp + "sz").FirstOrDefault();
                        if (size is null || !long.TryParse((string?)size.Attribute("width"), out var imageWidth) || !long.TryParse((string?)size.Attribute("height"), out var imageHeight) || imageWidth <= 0 || imageHeight <= 0) issues.Add(new ValidationIssue(ValidationStatus.Fail, null, "HWPX_IMAGE_PLACEMENT_INVALID", entry.FullName)); else { var resourceEntry = zip.GetEntry("BinData/" + resourceId + ".png") ?? zip.GetEntry("BinData/" + resourceId + ".jpg"); if (resourceEntry is not null) { using var rs = resourceEntry.Open(); using var ms = new MemoryStream(); rs.CopyTo(ms); try { var mime = resourceEntry.FullName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg"; var d = HwpxImageResourceWriter.ReadDimensions(ms.ToArray(), mime); if (HwpxImageGeometry.RatioError(imageWidth, imageHeight, d.Width, d.Height) > 0.001) issues.Add(new ValidationIssue(ValidationStatus.Fail, null, "HWPX_IMAGE_ASPECT_RATIO_MISMATCH", $"{entry.FullName}: placement {imageWidth}x{imageHeight}, source {d.Width}x{d.Height}")); var dim = picture?.Elements(hp + "imgDim").FirstOrDefault(); if (dim is not null && long.TryParse((string?)dim.Attribute("dimwidth"), out var dw) && dw == imageWidth) issues.Add(new ValidationIssue(ValidationStatus.Fail, null, "HWPX_IMAGE_PHYSICAL_GEOMETRY_UNVERIFIED", entry.FullName)); var pagePrNode = section.Descendants(hp + "pagePr").FirstOrDefault(); if (pagePrNode is not null && long.TryParse((string?)pagePrNode.Attribute("width"), out var pw) && long.TryParse((string?)pagePrNode.Attribute("height"), out var ph) && imageWidth == pw && imageHeight == ph && (picture?.Parent?.Descendants(hp + "t").Any() == true || picture?.Parent?.Parent?.Element(hp + "linesegarray") is not null)) issues.Add(new ValidationIssue(ValidationStatus.Fail, null, "HWPX_VISUAL_EXTRA_PAGE_RISK", entry.FullName)); if (pagePrNode is not null && long.TryParse((string?)pagePrNode.Attribute("width"), out var pageWidth) && long.TryParse((string?)pagePrNode.Attribute("height"), out var pageHeight) && imageWidth == pageWidth && imageHeight == pageHeight) issues.Add(new ValidationIssue(ValidationStatus.Fail, null, "HWPX_VISUAL_EXACT_PAGE_SIZE_RISK", entry.FullName)); else if (pagePrNode is not null && long.TryParse((string?)pagePrNode.Attribute("width"), out var pageW) && long.TryParse((string?)pagePrNode.Attribute("height"), out var pageH) && (imageWidth > pageW || imageHeight > pageH)) issues.Add(new ValidationIssue(ValidationStatus.Fail, null, "HWPX_VISUAL_PICTURE_EXCEEDS_PAGE", entry.FullName)); } catch (InvalidDataException) { issues.Add(new ValidationIssue(ValidationStatus.Fail, null, "HWPX_IMAGE_DIMENSIONS_INVALID", entry.FullName)); } } }
                    }
                    var backgroundRefs = section.Descendants(hp + "pageBorderFill").Where(x => (string?)x.Attribute("type") == "BOTH" && (string?)x.Attribute("borderFillIDRef") is not null).ToArray();
                    if (headerDocument?.Descendants(hh + "borderFill").Any(x => (string?)x.Attribute("id") == backgroundRefs.FirstOrDefault()?.Attribute("borderFillIDRef")?.Value && x.Descendants(hc + "imgBrush").Any()) == true && section.Descendants(hp + "pic").Any())
                        issues.Add(new ValidationIssue(ValidationStatus.Fail, null, "HWPX_PAGE_BACKGROUND_BODY_PICTURE_PRESENT", entry.FullName));
                }
                catch (Exception ex) when (ex is System.Xml.XmlException or InvalidOperationException) { issues.Add(new ValidationIssue(ValidationStatus.Fail, null, "HWPX_SECTION_XML_INVALID", entry.FullName)); }
            }
        }
        catch (InvalidDataException ex) { issues.Add(new ValidationIssue(ValidationStatus.Fail, null, "HWPX_INVALID_ZIP", ex.Message)); }
        return new ValidationReport(issues.Any(i => i.Status == ValidationStatus.Fail) ? ValidationStatus.Fail : ValidationStatus.Pass, issues);
    }
}
