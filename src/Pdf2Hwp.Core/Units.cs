namespace Pdf2Hwp.Core;

public readonly record struct PdfPoint(double Value);
public readonly record struct HwpxUnit(long Value);
public sealed record PageMargins(HwpxUnit Left, HwpxUnit Right, HwpxUnit Top, HwpxUnit Bottom, HwpxUnit Header, HwpxUnit Footer);
public sealed record HwpxPageProperties(HwpxUnit Width, HwpxUnit Height, bool Landscape, int Rotation, PageMargins Margins);

public static class HwpxUnitConverter
{
    // OWPML physical length: 7,200 HWPUNIT per inch; PDF point: 72 per inch.
    public const double UnitsPerPdfPoint = 100d;
    public static HwpxUnit FromPdfPoint(double points) => new((long)Math.Round(points * UnitsPerPdfPoint, MidpointRounding.AwayFromZero));
    public static HwpxPageProperties FromPdfPage(PdfPageInfo page)
    {
        var rotated = ((page.Rotation % 360) + 360) % 360;
        var width = page.CropBox?.Width ?? page.WidthPoints; var height = page.CropBox?.Height ?? page.HeightPoints;
        var landscape = width > height;
        return new(FromPdfPoint(width), FromPdfPoint(height), landscape, rotated, new(new(5669), new(5669), new(4252), new(4252), new(4252), new(4252)));
    }
}

/// <summary>Confirmed by the 100×50mm Hancom Golden: picture size fields use 1/7200 inch.</summary>
public static class HwpxObjectUnitConverter
{
    public const double UnitsPerInch = 7200d;
    public static HwpxUnit FromMillimeters(double millimeters) => new((long)Math.Round(millimeters / 25.4d * UnitsPerInch, MidpointRounding.AwayFromZero));
}
