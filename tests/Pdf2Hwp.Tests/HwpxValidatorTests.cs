using System.IO.Compression;
using Pdf2Hwp.Core;
using Pdf2Hwp.Infrastructure;

namespace Pdf2Hwp.Tests;

public sealed class HwpxValidatorTests
{
    [Fact]
    public void Missing_manifest_target_is_rejected()
    {
        var path = Path.Combine(Path.GetTempPath(), $"invalid-{Guid.NewGuid():N}.hwpx");
        try
        {
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                Write(archive, "mimetype", "application/hwp+zip"); Write(archive, "META-INF/container.xml", "<container />"); Write(archive, "Contents/header.xml", "<head />"); Write(archive, "Contents/content.hpf", "<opf:package xmlns:opf=\"http://www.idpf.org/2007/opf/\"><opf:manifest><opf:item id=\"section0\" href=\"Contents/section0.xml\" /></opf:manifest><opf:spine><opf:itemref idref=\"missing\" /></opf:spine></opf:package>");
            }
            var report = new HwpxPackageInspector().Inspect(path); Assert.Equal(ValidationStatus.Fail, report.Status); Assert.Contains(report.Issues, issue => issue.Code == "HWPX_MANIFEST_TARGET_MISSING"); Assert.Contains(report.Issues, issue => issue.Code == "HWPX_SPINE_REFERENCE_INVALID");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void Hancom_rejected_minimal_package_shape_is_rejected()
    {
        var path = Path.Combine(Path.GetTempPath(), $"legacy-{Guid.NewGuid():N}.hwpx");
        try
        {
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                Write(archive, "mimetype", "application/hwp+zip");
                Write(archive, "META-INF/container.xml", "<container />");
                Write(archive, "Contents/content.hpf", "<package />");
                Write(archive, "Contents/header.xml", "<head />");
                Write(archive, "Contents/section0.xml", "<sec />");
            }
            var report = new HwpxPackageInspector().Inspect(path);
            Assert.Equal(ValidationStatus.Fail, report.Status);
            Assert.Contains(report.Issues, issue => issue.Code == "HWPX_REQUIRED_PART_MISSING");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
    private static void Write(ZipArchive archive, string path, string value) { using var writer = new StreamWriter(archive.CreateEntry(path).Open()); writer.Write(value); }
}
