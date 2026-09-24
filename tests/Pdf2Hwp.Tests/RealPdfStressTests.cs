using System.Diagnostics;
using System.Runtime.Versioning;
using Pdf2Hwp.Core;
using Pdf2Hwp.Infrastructure;
using UglyToad.PdfPig;

namespace Pdf2Hwp.Tests;

public sealed class RealPdfStressTests
{
    private static readonly string FixturePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "artifacts", "pdf-fixtures", "stress-a4-portrait-100pages.pdf"));

    [Fact]
    public void ActualHundredPagePdfHasHundredPagesAndA4Geometry()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FixturePath)!);
        if (!File.Exists(FixturePath)) SyntheticPdfFixture.CreateHundredPageA4(FixturePath);
        using var pdf = PdfDocument.Open(FixturePath);
        Assert.Equal(100, pdf.NumberOfPages);
        foreach (var number in Enumerable.Range(1, 100))
        {
            var page = pdf.GetPage(number);
            Assert.InRange(page.Width, 595.0, 595.6); Assert.InRange(page.Height, 841.5, 842.2);
            Assert.Equal(0, page.Rotation.Value);
        }
        foreach (var number in new[] { 1, 50, 100 })
        {
            var page = pdf.GetPage(number);
            Assert.True(page.MediaBox.Bounds.Width > 0); Assert.True(page.CropBox.Bounds.Width > 0);
            Assert.Contains($"PAGE {number:000}", page.Text, StringComparison.Ordinal);
        }
    }

    [Fact]
    [SupportedOSPlatform("windows")]
    public async Task ActualHundredPagePdfLoadsAndRendersBoundedRepresentativeThumbnails()
    {
        if (!File.Exists(FixturePath)) SyntheticPdfFixture.CreateHundredPageA4(FixturePath);
        var analysis = await new PdfPigAnalyzer().AnalyzeAsync(FixturePath, CancellationToken.None);
        Assert.Equal(100, analysis.PageCount);
        var workspace = new DocumentWorkspace(); workspace.AddSource(SourceDocument.Create(FixturePath, analysis.PageCount), analysis.Pages);
        Assert.Equal(100, workspace.Pages.Count); Assert.Equal(100, workspace.Pages.Select(p => p.StablePageId).Distinct().Count());
        Assert.Equal(Enumerable.Range(0, 100), workspace.Pages.Select(p => p.LogicalOutputIndex));

        using var temp = new TempDirectory(); var cache = new PreviewThumbnailCache(Path.Combine(temp.Path, "preview"));
        using var renderer = new CachedPreviewThumbnailRenderer(new PdfiumPreviewThumbnailRenderer(new PdfiumPageRenderer()), cache, 2);
        var watch = Stopwatch.StartNew();
        foreach (var pageIndex in new[] { 0, 1, 49, 98, 99 })
        {
            var image = await renderer.RenderAsync(FixturePath, pageIndex, new(300), CancellationToken.None);
            Assert.True(image.Width > 0); Assert.True(image.Height > 0); Assert.InRange(Math.Max(image.Width, image.Height), 299, 301); Assert.True(image.Height > image.Width);
            Assert.True(File.Exists(cache.GetPath(image.SourceHash, pageIndex, 300)));
        }
        await renderer.RenderAsync(FixturePath, 49, new(300), CancellationToken.None);
        Assert.Equal(1, cache.Statistics.Hits); Assert.Equal(0, renderer.ActiveRenderCount);
        var renderCache = new JobRenderCache(new PdfiumPageRenderer(), Path.Combine(temp.Path, "job-render"));
        var sourcePage = workspace.Pages[0]; var first = await renderCache.RenderAsync(sourcePage, 200, CancellationToken.None); var duplicate = await renderCache.RenderAsync(sourcePage with { Id = Guid.NewGuid() }, 200, CancellationToken.None);
        Assert.Equal(1, renderCache.ActualRenders); Assert.False(first.WasReused); Assert.True(duplicate.WasReused); Assert.Equal(first.RenderHash, duplicate.RenderHash);
        watch.Stop(); Assert.True(watch.Elapsed > TimeSpan.Zero);
    }

    [Fact]
    public async Task ActualHundredPageWorkspaceReorderExcludeAndDuplicatePreserveSnapshot()
    {
        if (!File.Exists(FixturePath)) SyntheticPdfFixture.CreateHundredPageA4(FixturePath);
        var document = await new PdfPigAnalyzer().AnalyzeAsync(FixturePath, CancellationToken.None);
        var workspace = new DocumentWorkspace(); workspace.AddSource(SourceDocument.Create(FixturePath, 100), document.Pages);
        var first = workspace.Pages[0].Id; var last = workspace.Pages[^1].Id; workspace.MovePage(last, 0); workspace.MovePage(first, 99);
        foreach (var page in workspace.Pages.Where(p => p.SourcePageIndex is >= 10 and < 20).ToArray()) workspace.SetIncluded(page.Id, false);
        for (var i = 0; i < 5; i++) workspace.DuplicatePage(workspace.Pages[i].Id);
        var snapshot = workspace.CreateOutputSnapshot();
        Assert.Equal(95, snapshot.Count); Assert.Equal(99, snapshot[0].SourcePageIndex); Assert.Equal(99, workspace.Pages[0].SourcePageIndex);
        Assert.Equal(Enumerable.Range(0, snapshot.Count), snapshot.Select(p => p.LogicalOutputIndex));
        Assert.Equal(95, snapshot.Select(p => p.StablePageId).Distinct().Count());
    }

    [Fact]
    [SupportedOSPlatform("windows")]
    public async Task ActualHundredPageCancellationStopsThumbnailLoopAndCleansPartials()
    {
        if (!File.Exists(FixturePath)) SyntheticPdfFixture.CreateHundredPageA4(FixturePath);
        using var temp = new TempDirectory(); var cache = new PreviewThumbnailCache(Path.Combine(temp.Path, "preview"));
        using var renderer = new CachedPreviewThumbnailRenderer(new PdfiumPreviewThumbnailRenderer(new PdfiumPageRenderer()), cache, 2); using var cancellation = new CancellationTokenSource();
        var completed = 0;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            for (var pageIndex = 0; pageIndex < 100; pageIndex++)
            {
                cancellation.Token.ThrowIfCancellationRequested();
                await renderer.RenderAsync(FixturePath, pageIndex, new(300), cancellation.Token);
                completed++;
                if (completed == 4) cancellation.Cancel();
            }
        });
        Assert.Equal(4, completed); Assert.Equal(4, cache.Measure().Entries); Assert.Equal(0, renderer.ActiveRenderCount);
        Assert.Empty(Directory.GetFiles(cache.Root, "*.partial-*", SearchOption.AllDirectories));
    }

    private sealed class TempDirectory : IDisposable { public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N")); public TempDirectory() => Directory.CreateDirectory(Path); public void Dispose() { try { Directory.Delete(Path, true); } catch { } } }
}
