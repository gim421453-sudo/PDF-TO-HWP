using Pdf2Hwp.Core;
using Pdf2Hwp.Infrastructure;

namespace Pdf2Hwp.Tests;

public sealed class HwpxImageWriterTests
{
    private static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
    private static readonly byte[] Jpeg = Convert.FromBase64String("/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAP//////////////////////////////////////////////////////////////////////////////////////2wBDAf//////////////////////////////////////////////////////////////////////////////////////wAARCAABAAEDASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAX/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIQAxAAAAF//8QAFBABAAAAAAAAAAAAAAAAAAAAAP/aAAgBAQABBQJ//8QAFBEBAAAAAAAAAAAAAAAAAAAAAP/aAAgBAwEBPwF//8QAFBEBAAAAAAAAAAAAAAAAAAAAAP/aAAgBAgEBPwF//8QAFBABAAAAAAAAAAAAAAAAAAAAAP/aAAgBAQAGPwJ//8QAFBABAAAAAAAAAAAAAAAAAAAAAP/aAAgBAQABPyF//9k=");

    [Fact] public void Png_resource_has_expected_mime_and_hash() => Assert.Equal("image/png", Create("png", Png).MimeType);
    [Fact] public void Jpeg_resource_has_expected_mime_and_hash() => Assert.Equal("image/jpeg", Create("jpg", Jpeg).MimeType);
    [Fact] public void Resource_hash_is_stable() => Assert.Equal(Create("png", Png).Sha256, Create("png", Png).Sha256);
    [Fact] public void Empty_resource_is_rejected() => Assert.Throws<InvalidDataException>(() => Create("png", []));
    [Fact] public void Mime_signature_mismatch_is_rejected() => Assert.Throws<InvalidDataException>(() => Create("jpg", Png));
    [Fact] public void SquareImagePreservesSquarePlacement() => Assert.Equal((1000L, 1000L), HwpxImageGeometry.CalculatePlacementPreservingAspectRatio(1, 1, 1000));
    [Fact] public void TwoToOneImagePreservesAspectRatio() => Assert.Equal((2000L, 1000L), HwpxImageGeometry.CalculatePlacementPreservingAspectRatio(800, 400, 2000));
    [Fact] public void RequestedWidthComputesExpectedHeight() => Assert.Equal(14173L, HwpxImageGeometry.CalculatePlacementPreservingAspectRatio(800, 400, 28346).Height);
    [Fact] public void PngAndJpegUseSameGeometryPolicy() => Assert.Equal(HwpxImageGeometry.CalculatePlacementPreservingAspectRatio(800, 400, 28346), HwpxImageGeometry.CalculatePlacementPreservingAspectRatio(800, 400, 28346));
    [Fact] public void StretchModeAllowsExplicitDistortion() => Assert.True(HwpxImageFitMode.Stretch != HwpxImageFitMode.PreserveAspectRatio);
    [Fact] public void PageUnitConversionMatchesA4() => Assert.Equal(59528, HwpxUnitConverter.FromPdfPoint(595.275590551).Value);
    [Fact] public void PictureUnitConversionMatchesGolden() => Assert.Equal(28346, HwpxObjectUnitConverter.FromMillimeters(100).Value);
    [Fact] public void Picture100MmProducesExpectedXmlWidth() => Assert.Equal(28346, HwpxObjectUnitConverter.FromMillimeters(100).Value);
    [Fact] public void Picture50MmProducesExpectedXmlHeight() => Assert.Equal(14173, HwpxObjectUnitConverter.FromMillimeters(50).Value);
    [Fact] public void PictureAndPageUnitsAreNotIncorrectlyShared() => Assert.NotEqual(HwpxUnitConverter.FromPdfPoint(100).Value, HwpxObjectUnitConverter.FromMillimeters(100).Value);
    [Fact] public void TwoToOneImagePreservesPhysicalSizeAndRatio() { var p = HwpxImageGeometry.CalculatePlacementPreservingAspectRatio(800, 400, HwpxObjectUnitConverter.FromMillimeters(100).Value); Assert.Equal(28346, p.Width); Assert.Equal(14173, p.Height); Assert.Equal(2d, p.Width / (double)p.Height, 2); }
    [Theory, InlineData(1000, 1000, 30000, 15000), InlineData(0, 0, 1, 1)] public void Placement_preserves_physical_geometry(long x, long y, long width, long height) => Assert.Equal((x, y, width, height), (new HwpxImagePlacement("p", "r", x, y, width, height).X, y, width, height));
    [Fact]
    public async Task EmbeddedImageManifestIsMarkedEmbedded()
    {
        var source = Path.Combine(Path.GetTempPath(), $"image-{Guid.NewGuid():N}.png"); var output = Path.Combine(Path.GetTempPath(), $"image-{Guid.NewGuid():N}.hwpx");
        try { File.WriteAllBytes(source, Png); var page = new PdfPageInfo(1, 595, 842, 0, null, true, false, Array.Empty<PdfGlyph>()); var resource = new HwpxImageResourceWriter().Create(source, "image0", "image/png"); await new HwpxImagePackageWriter().WriteAsync(new PdfDocumentInfo("image", 1, [page]), output, resource, new HwpxImagePlacement("pic0", "image0", 100, 100, 1000, 1000), CancellationToken.None); Assert.Equal(ValidationStatus.Pass, new HwpxPackageInspector().Inspect(output).Status); }
        finally { if (File.Exists(source)) File.Delete(source); if (File.Exists(output)) File.Delete(output); }
    }
    private static HwpxBinaryResource Create(string ext, byte[] bytes)
    { var path = Path.Combine(Path.GetTempPath(), $"hwpx-image-test-{Guid.NewGuid():N}.{ext}"); File.WriteAllBytes(path, bytes); try { return new HwpxImageResourceWriter().Create(path, "image0", ext == "png" ? "image/png" : "image/jpeg"); } finally { File.Delete(path); } }
}
