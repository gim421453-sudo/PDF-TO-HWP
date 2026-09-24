using System.Diagnostics;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using Pdf2Hwp.Core;
using UglyToad.PdfPig;

namespace Pdf2Hwp.Infrastructure;

public sealed record PreviewThumbnailOptions(int LongEdge = 300, int Version = 1);
public sealed record PreviewThumbnailResult(int Width, int Height, byte[] PixelData, string SourceHash, int PageIndex, TimeSpan RenderDuration, bool CacheHit);
public interface IPreviewThumbnailRenderer
{
    Task<PreviewThumbnailResult> RenderAsync(string sourceDocument, int sourcePageIndex, PreviewThumbnailOptions options, CancellationToken cancellationToken);
}

public sealed record JobRenderKey(string SourceHash, int SourcePageIndex, int RenderQuality, int RotationDegrees, CropRegion? Crop);
public sealed record JobRenderResult(JobRenderKey Key, string RenderPath, string RenderHash, int Width, int Height, TimeSpan RenderDuration, bool WasReused);

/// <summary>Job-scoped raster single-flight. It deduplicates PDF rendering only; HWPX resources remain writer-owned.</summary>
public sealed class JobRenderCache(IPdfPageRenderer renderer, string jobDirectory)
{
    private readonly ConcurrentDictionary<JobRenderKey, Lazy<Task<JobRenderResult>>> _renders = new();
    private int _actualRenders, _reuseHits;
    public int ActualRenders => Volatile.Read(ref _actualRenders);
    public int RenderReuseHits => Volatile.Read(ref _reuseHits);

    public async Task<JobRenderResult> RenderAsync(DocumentPage page, int renderQuality, CancellationToken cancellationToken)
    {
        var hash = await PdfiumPreviewThumbnailRenderer.SourceHashAsync(page.SourcePath, cancellationToken).ConfigureAwait(false);
        var key = new JobRenderKey(hash, page.SourcePageIndex, renderQuality, page.RotationDegrees, page.Crop);
        var candidate = new Lazy<Task<JobRenderResult>>(() => RenderCoreAsync(page, key, cancellationToken), LazyThreadSafetyMode.ExecutionAndPublication);
        var lazy = _renders.GetOrAdd(key, candidate);
        var reused = !ReferenceEquals(candidate, lazy);
        if (reused) Interlocked.Increment(ref _reuseHits);
        try { var result = await lazy.Value.ConfigureAwait(false); return result with { WasReused = reused }; }
        catch { _renders.TryRemove(new KeyValuePair<JobRenderKey, Lazy<Task<JobRenderResult>>>(key, lazy)); throw; }
    }

    private async Task<JobRenderResult> RenderCoreAsync(DocumentPage page, JobRenderKey key, CancellationToken token)
    {
        Directory.CreateDirectory(jobDirectory);
        var cropIdentity = key.Crop is null ? "full" : Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{key.Crop.Left:R},{key.Crop.Bottom:R},{key.Crop.Width:R},{key.Crop.Height:R}"))))[..12].ToLowerInvariant();
        var output = Path.Combine(jobDirectory, $"{key.SourceHash[..16]}-p{key.SourcePageIndex}-q{key.RenderQuality}-r{key.RotationDegrees}-c{cropIdentity}.png");
        var watch = Stopwatch.StartNew(); Interlocked.Increment(ref _actualRenders);
        try
        {
            var request = new RenderRequest(page.SourcePath, page.SourcePageIndex + 1, output + ".partial-" + Guid.NewGuid().ToString("N"), key.RenderQuality, RotationDegrees: key.RotationDegrees);
            await renderer.RenderAsync(request, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            var dims = HwpxImageResourceWriter.ReadDimensions(await File.ReadAllBytesAsync(request.OutputPngPath, token).ConfigureAwait(false), "image/png");
            File.Move(request.OutputPngPath, output, true); watch.Stop();
            var renderHash = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(output, token).ConfigureAwait(false))).ToLowerInvariant();
            return new JobRenderResult(key, output, renderHash, dims.Width, dims.Height, watch.Elapsed, false);
        }
        finally
        {
            var pattern = Path.GetFileName(output) + ".partial-*";
            foreach (var partial in Directory.EnumerateFiles(jobDirectory, pattern)) { try { File.Delete(partial); } catch { } }
        }
    }
}

public sealed class PdfiumPreviewThumbnailRenderer(IPdfPageRenderer renderer) : IPreviewThumbnailRenderer
{
    public async Task<PreviewThumbnailResult> RenderAsync(string sourceDocument, int sourcePageIndex, PreviewThumbnailOptions options, CancellationToken cancellationToken)
    {
        var watch = Stopwatch.StartNew();
        var sourceHash = await SourceHashAsync(sourceDocument, cancellationToken).ConfigureAwait(false);
        var temp = Path.Combine(Path.GetTempPath(), "PDF2HWP-preview-" + Guid.NewGuid().ToString("N") + ".png");
        try
        {
            var geometry = await PreviewGeometryAsync(sourceDocument, sourcePageIndex, options.LongEdge, cancellationToken).ConfigureAwait(false);
            await renderer.RenderAsync(new RenderRequest(sourceDocument, sourcePageIndex + 1, temp, 72, geometry.Width, geometry.Height, geometry.Rotation), cancellationToken).ConfigureAwait(false);
            var bytes = await File.ReadAllBytesAsync(temp, cancellationToken).ConfigureAwait(false);
            var dimensions = HwpxImageResourceWriter.ReadDimensions(bytes, "image/png");
            return new(dimensions.Width, dimensions.Height, bytes, sourceHash, sourcePageIndex, watch.Elapsed, false);
        }
        finally { try { if (File.Exists(temp)) File.Delete(temp); } catch { } }
    }
    private static Task<(int Width, int Height, int Rotation)> PreviewGeometryAsync(string path, int pageIndex, int longEdge, CancellationToken token) => Task.Run(() =>
    {
        token.ThrowIfCancellationRequested(); using var pdf = PdfDocument.Open(path); var page = pdf.GetPage(pageIndex + 1); var width = page.Width; var height = page.Height; var rotation = page.Rotation.Value;
        if (Math.Abs(rotation) % 180 == 90) (width, height) = (height, width);
        var scale = longEdge / Math.Max(width, height); return ((int)Math.Round(width * scale), (int)Math.Round(height * scale), rotation);
    }, token);
    internal static async Task<string> SourceHashAsync(string path, CancellationToken token)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, token).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

public sealed class CachedPreviewThumbnailRenderer(
    IPreviewThumbnailRenderer inner,
    PreviewThumbnailCache cache,
    int maxConcurrency = 2) : IPreviewThumbnailRenderer, IDisposable
{
    private readonly SemaphoreSlim _gate = new(Math.Max(1, maxConcurrency));
    private readonly ConcurrentDictionary<string, Lazy<Task<PreviewThumbnailResult>>> _inflight = new();
    public int ActiveRenderCount => Volatile.Read(ref _active);
    private int _active;
    public async Task<PreviewThumbnailResult> RenderAsync(string sourceDocument, int sourcePageIndex, PreviewThumbnailOptions options, CancellationToken cancellationToken)
    {
        var sourceHash = await PdfiumPreviewThumbnailRenderer.SourceHashAsync(sourceDocument, cancellationToken).ConfigureAwait(false);
        var path = cache.GetPath(sourceHash, sourcePageIndex, options.LongEdge, options.Version);
        using var entryLease = await cache.AcquireEntryAsync(path, cancellationToken).ConfigureAwait(false);
        if (File.Exists(path))
        {
            try
            {
                var bytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
                var d = HwpxImageResourceWriter.ReadDimensions(bytes, "image/png");
                cache.RecordHit(); return new(d.Width, d.Height, bytes, sourceHash, sourcePageIndex, TimeSpan.Zero, true);
            }
            catch { try { File.Delete(path); } catch { } }
        }
        cache.RecordMiss();
        var key = path;
        var lazy = _inflight.GetOrAdd(key, _ => new Lazy<Task<PreviewThumbnailResult>>(() => RenderAndStoreAsync(sourceDocument, sourcePageIndex, options, sourceHash, path, cancellationToken), LazyThreadSafetyMode.ExecutionAndPublication));
        try { return await lazy.Value.ConfigureAwait(false); }
        catch { _inflight.TryRemove(new KeyValuePair<string, Lazy<Task<PreviewThumbnailResult>>>(key, lazy)); throw; }
        finally { if (lazy.IsValueCreated && lazy.Value.IsCompletedSuccessfully) _inflight.TryRemove(new KeyValuePair<string, Lazy<Task<PreviewThumbnailResult>>>(key, lazy)); }
    }
    public async Task InvalidateAsync(string sourceDocument, int sourcePageIndex, PreviewThumbnailOptions options, CancellationToken cancellationToken)
    {
        var hash = await PdfiumPreviewThumbnailRenderer.SourceHashAsync(sourceDocument, cancellationToken).ConfigureAwait(false);
        var path = cache.GetPath(hash, sourcePageIndex, options.LongEdge, options.Version);
        using var lease = await cache.AcquireEntryAsync(path, cancellationToken).ConfigureAwait(false);
        try { if (File.Exists(path)) File.Delete(path); } catch (IOException) { }
    }
    private async Task<PreviewThumbnailResult> RenderAndStoreAsync(string source, int page, PreviewThumbnailOptions options, string hash, string path, CancellationToken token)
    {
        await _gate.WaitAsync(token).ConfigureAwait(false);
        Interlocked.Increment(ref _active);
        try
        {
            var result = await inner.RenderAsync(source, page, options, token).ConfigureAwait(false);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temporary = path + ".partial-" + Guid.NewGuid().ToString("N");
            try { await File.WriteAllBytesAsync(temporary, result.PixelData, token).ConfigureAwait(false); File.Move(temporary, path, true); }
            finally { try { if (File.Exists(temporary)) File.Delete(temporary); } catch { } }
            return result with { SourceHash = hash };
        }
        finally { Interlocked.Decrement(ref _active); _gate.Release(); }
    }
    public void Dispose() => _gate.Dispose();
}
