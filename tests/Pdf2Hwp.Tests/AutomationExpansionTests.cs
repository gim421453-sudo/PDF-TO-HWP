using Pdf2Hwp.Core;
using Pdf2Hwp.Infrastructure;

namespace Pdf2Hwp.Tests;

public sealed class AutomationExpansionTests
{
    private static readonly byte[] Png1x1 = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

    [Fact]
    public async Task ThumbnailCacheHitAvoidsSecondRender()
    {
        using var d = new TempDirectory(); var source = Path.Combine(d.Path, "a.pdf"); File.WriteAllText(source, "a");
        var cache = new PreviewThumbnailCache(Path.Combine(d.Path, "cache")); var inner = new FakeThumbnailRenderer(); using var renderer = new CachedPreviewThumbnailRenderer(inner, cache, 2);
        await renderer.RenderAsync(source, 0, new(300), CancellationToken.None); await renderer.RenderAsync(source, 0, new(300), CancellationToken.None);
        Assert.Equal(1, inner.Count); Assert.Equal(1, cache.Statistics.Hits);
    }

    [Fact]
    public async Task ThumbnailSourceChangeCausesCacheMiss()
    {
        using var d = new TempDirectory(); var source = Path.Combine(d.Path, "a.pdf"); File.WriteAllText(source, "a");
        var cache = new PreviewThumbnailCache(Path.Combine(d.Path, "cache")); var inner = new FakeThumbnailRenderer(); using var renderer = new CachedPreviewThumbnailRenderer(inner, cache);
        await renderer.RenderAsync(source, 0, new(300), CancellationToken.None); File.WriteAllText(source, "changed"); await renderer.RenderAsync(source, 0, new(300), CancellationToken.None);
        Assert.Equal(2, inner.Count);
    }

    [Fact]
    public async Task ThumbnailCancellationLeavesNoPartialCache()
    {
        using var d = new TempDirectory(); var source = Path.Combine(d.Path, "a.pdf"); File.WriteAllText(source, "a"); var cache = new PreviewThumbnailCache(Path.Combine(d.Path, "cache"));
        using var renderer = new CachedPreviewThumbnailRenderer(new SlowThumbnailRenderer(), cache); using var cts = new CancellationTokenSource(); cts.CancelAfter(10);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => renderer.RenderAsync(source, 0, new(300), cts.Token));
        Assert.True(!Directory.Exists(cache.Root) || Directory.GetFiles(cache.Root, "*.partial-*", SearchOption.AllDirectories).Length == 0);
    }

    [Fact]
    public async Task ConcurrentDuplicateRequestsUseOneJobRasterRender()
    {
        using var d = new TempDirectory(); var source = Path.Combine(d.Path, "a.pdf"); File.WriteAllText(source, "a");
        var page = DocumentPage.FromSource(SourceDocument.Create(source, 1), 0); var fake = new CountingPdfRenderer(); var cache = new JobRenderCache(fake, Path.Combine(d.Path, "job"));
        var calls = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => cache.RenderAsync(page, 200, CancellationToken.None)));
        Assert.Equal(1, fake.Count); Assert.Equal(1, cache.ActualRenders); Assert.Equal(7, cache.RenderReuseHits); Assert.Single(calls.Select(c => c.RenderHash).Distinct()); Assert.Equal(1, calls.Count(c => !c.WasReused));
    }

    [Fact]
    public async Task FailedJobRasterRenderCanBeRetriedAndQualityHasDistinctKey()
    {
        using var d = new TempDirectory(); var source = Path.Combine(d.Path, "a.pdf"); File.WriteAllText(source, "a"); var page = DocumentPage.FromSource(SourceDocument.Create(source, 1), 0);
        var fake = new CountingPdfRenderer(failFirst: true); var cache = new JobRenderCache(fake, Path.Combine(d.Path, "job"));
        await Assert.ThrowsAsync<IOException>(() => cache.RenderAsync(page, 200, CancellationToken.None));
        var retry = await cache.RenderAsync(page, 200, CancellationToken.None); var high = await cache.RenderAsync(page, 300, CancellationToken.None); var cropped = await cache.RenderAsync(page with { Crop = new CropRegion(1, 2, 3, 4) }, 200, CancellationToken.None);
        Assert.False(retry.WasReused); Assert.False(high.WasReused); Assert.False(cropped.WasReused); Assert.Equal(4, fake.Count); Assert.NotEqual(retry.Key, high.Key); Assert.NotEqual(retry.RenderPath, cropped.RenderPath);
        Assert.Empty(Directory.GetFiles(Path.Combine(d.Path, "job"), "*.partial-*"));
    }

    [Fact]
    public async Task CancelledJobRasterRenderLeavesNoPartialFileAndCanBeRetried()
    {
        using var d = new TempDirectory(); var source = Path.Combine(d.Path, "a.pdf"); File.WriteAllText(source, "a"); var page = DocumentPage.FromSource(SourceDocument.Create(source, 1), 0);
        var fake = new CountingPdfRenderer(); var cache = new JobRenderCache(fake, Path.Combine(d.Path, "job")); using var cts = new CancellationTokenSource(5);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cache.RenderAsync(page, 200, cts.Token));
        Assert.Empty(Directory.GetFiles(Path.Combine(d.Path, "job"), "*.partial-*"));
        var retry = await cache.RenderAsync(page, 200, CancellationToken.None); Assert.False(retry.WasReused); Assert.True(File.Exists(retry.RenderPath));
    }

    [Fact]
    public async Task DifferentSourceHashDoesNotReuseJobRaster()
    {
        using var d = new TempDirectory(); var source = Path.Combine(d.Path, "a.pdf"); File.WriteAllText(source, "first"); var page = DocumentPage.FromSource(SourceDocument.Create(source, 1), 0);
        var fake = new CountingPdfRenderer(); var cache = new JobRenderCache(fake, Path.Combine(d.Path, "job")); var first = await cache.RenderAsync(page, 200, CancellationToken.None);
        File.WriteAllText(source, "second"); var changed = await cache.RenderAsync(page, 200, CancellationToken.None);
        Assert.Equal(2, fake.Count); Assert.NotEqual(first.Key.SourceHash, changed.Key.SourceHash);
    }

    [Fact]
    public async Task DuplicateLogicalPagesRenderOneRasterButRemainSeparateLogicalEntries()
    {
        using var d = new TempDirectory(); var source = Path.Combine(d.Path, "a.pdf"); File.WriteAllText(source, "a"); var page = DocumentPage.FromSource(SourceDocument.Create(source, 3), 1);
        var logicalPages = new[] { page with { SourcePageIndex = 0 }, page with { SourcePageIndex = 1 }, page with { SourcePageIndex = 1, Id = Guid.NewGuid() }, page with { SourcePageIndex = 1, Id = Guid.NewGuid() }, page with { SourcePageIndex = 2 } };
        var fake = new CountingPdfRenderer(); var cache = new JobRenderCache(fake, Path.Combine(d.Path, "job")); var renders = await Task.WhenAll(logicalPages.Select(p => cache.RenderAsync(p, 200, CancellationToken.None)));
        Assert.Equal(5, renders.Length); Assert.Equal(3, fake.Count); Assert.Equal(2, renders.Count(r => r.WasReused));
    }

    [Fact]
    public async Task PreviewWorkerGateNeverExceedsConfiguredConcurrency()
    {
        using var d = new TempDirectory(); var source = Path.Combine(d.Path, "a.pdf"); File.WriteAllText(source, "a"); var inner = new ConcurrencyThumbnailRenderer(); var cache = new PreviewThumbnailCache(Path.Combine(d.Path, "cache")); using var renderer = new CachedPreviewThumbnailRenderer(inner, cache, 2);
        await Task.WhenAll(Enumerable.Range(0, 12).Select(index => renderer.RenderAsync(source, index, new(300), CancellationToken.None)));
        Assert.Equal(12, inner.Count); Assert.InRange(inner.MaximumActive, 1, 2); Assert.Equal(0, renderer.ActiveRenderCount);
    }

    [Fact]
    public void WorkspaceHistoryRestoresStableIdentityAndClearsRedo()
    {
        using var d = new TempDirectory(); var source = SourceDocument.Create(Path.Combine(d.Path, "a.pdf"), 3); var workspace = new DocumentWorkspace(); workspace.AddSource(source); var id = workspace.Pages[0].Id; var history = new WorkspaceHistory(workspace);
        history.Execute("move", w => w.MovePage(id, 2)); Assert.True(history.CanUndo); history.Undo(); Assert.Equal(id, workspace.Pages[0].Id); Assert.True(history.CanRedo); history.Redo(); Assert.Equal(id, workspace.Pages[2].Id); history.Undo(); history.Execute("remove", w => w.RemovePage(id)); Assert.False(history.CanRedo);
    }

    [Fact]
    public void HundredPageSnapshotPreservesOrderAndBulkExclude()
    {
        using var d = new TempDirectory(); var workspace = new DocumentWorkspace(); workspace.AddSource(SourceDocument.Create(Path.Combine(d.Path, "a.pdf"), 100));
        foreach (var page in workspace.Pages.Where(p => p.SourcePageIndex % 2 == 0).ToArray()) workspace.SetIncluded(page.Id, false);
        var snapshot = workspace.CreateOutputSnapshot(); Assert.Equal(50, snapshot.Count); Assert.Equal(1, snapshot[0].SourcePageIndex); Assert.Equal(99, snapshot[^1].SourcePageIndex);
    }

    private sealed class FakeThumbnailRenderer : IPreviewThumbnailRenderer { public int Count; public Task<PreviewThumbnailResult> RenderAsync(string source, int page, PreviewThumbnailOptions options, CancellationToken token) { Count++; return Task.FromResult(new PreviewThumbnailResult(1, 1, Png1x1, "", page, TimeSpan.Zero, false)); } }
    private sealed class SlowThumbnailRenderer : IPreviewThumbnailRenderer { public async Task<PreviewThumbnailResult> RenderAsync(string source, int page, PreviewThumbnailOptions options, CancellationToken token) { await Task.Delay(1000, token); return new(1, 1, Png1x1, "", page, TimeSpan.Zero, false); } }
    private sealed class ConcurrencyThumbnailRenderer : IPreviewThumbnailRenderer
    {
        private int _active, _maximumActive, _count; public int Count => Volatile.Read(ref _count); public int MaximumActive => Volatile.Read(ref _maximumActive);
        public async Task<PreviewThumbnailResult> RenderAsync(string source, int page, PreviewThumbnailOptions options, CancellationToken token)
        {
            Interlocked.Increment(ref _count); var active = Interlocked.Increment(ref _active); int observed;
            do { observed = _maximumActive; if (active <= observed) break; } while (Interlocked.CompareExchange(ref _maximumActive, active, observed) != observed);
            try { await Task.Delay(30, token); return new(1, 1, Png1x1, "", page, TimeSpan.Zero, false); }
            finally { Interlocked.Decrement(ref _active); }
        }
    }
    private sealed class CountingPdfRenderer(bool failFirst = false) : IPdfPageRenderer
    {
        private int _count; public int Count => Volatile.Read(ref _count);
        public async Task RenderAsync(RenderRequest request, CancellationToken token)
        {
            var count = Interlocked.Increment(ref _count); await Task.Delay(35, token);
            if (failFirst && count == 1) throw new IOException("synthetic render failure");
            await File.WriteAllBytesAsync(request.OutputPngPath, Png1x1, token);
        }
    }
    private sealed class TempDirectory : IDisposable { public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N")); public TempDirectory() => Directory.CreateDirectory(Path); public void Dispose() { try { Directory.Delete(Path, true); } catch { } } }
}
