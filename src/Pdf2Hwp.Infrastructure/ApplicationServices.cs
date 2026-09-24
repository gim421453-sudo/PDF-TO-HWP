using System.Security.Cryptography;
using System.Text.Json;
using System.Collections.Concurrent;
using Pdf2Hwp.Core;

namespace Pdf2Hwp.Infrastructure;

public sealed class TempWorkspaceManager : IConversionWorkspaceFactory
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "PDF2HWP");
    public IConversionWorkspace Create() { Directory.CreateDirectory(_root); var path = Path.Combine(_root, Guid.NewGuid().ToString("N")); Directory.CreateDirectory(path); return new ConversionWorkspace(path); }
    public int CleanupStale(TimeSpan olderThan) { if (!Directory.Exists(_root)) return 0; var count = 0; foreach (var dir in Directory.EnumerateDirectories(_root)) { try { var info = new DirectoryInfo(dir); if (DateTime.UtcNow - info.LastWriteTimeUtc > olderThan) { info.Delete(true); count++; } } catch { } } return count; }
    private sealed class ConversionWorkspace(string path) : IConversionWorkspace
    { public string DirectoryPath => path; public string CreateFilePath(string extension) => Path.Combine(path, Guid.NewGuid().ToString("N") + (extension.StartsWith('.') ? extension : "." + extension)); public void Dispose() { try { Directory.Delete(path, true); } catch { } } }
}
public sealed class PreviewThumbnailCache(string root)
{
    public const long DefaultMaximumBytes = 500L * 1024 * 1024;
    private long _hits, _misses;
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> ProcessLocks = new(StringComparer.OrdinalIgnoreCase);
    public string Root => root;
    public string GetPath(string sourceHash, int pageIndex, int longEdge, int version = 1) => Path.Combine(root, sourceHash, $"v{version}_p{pageIndex}_{longEdge}.png");
    public (long Hits, long Misses) Statistics => (Interlocked.Read(ref _hits), Interlocked.Read(ref _misses));
    public void RecordHit() => Interlocked.Increment(ref _hits);
    public void RecordMiss() => Interlocked.Increment(ref _misses);
    public async ValueTask<IDisposable> AcquireEntryAsync(string path, CancellationToken cancellationToken)
    {
        var gate = ProcessLocks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var lockPath = path + ".lease";
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try { var stream = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); return new CacheLease(gate, lockPath, stream); }
                catch (IOException) { await Task.Delay(25, cancellationToken).ConfigureAwait(false); }
            }
        }
        catch { gate.Release(); throw; }
    }
    public (int Entries, long Bytes) Measure() { if (!Directory.Exists(root)) return (0, 0); var files = Directory.EnumerateFiles(root, "*.png", SearchOption.AllDirectories).Select(path => new FileInfo(path)).ToArray(); return (files.Length, files.Sum(f => f.Length)); }
    public int Cleanup(long maximumBytes = DefaultMaximumBytes)
    {
        if (!Directory.Exists(root)) return 0;
        var removed = 0;
        foreach (var partial in Directory.EnumerateFiles(root, "*.partial-*", SearchOption.AllDirectories)) { try { if (DateTime.UtcNow - File.GetLastWriteTimeUtc(partial) > TimeSpan.FromDays(1)) { File.Delete(partial); removed++; } } catch (IOException) { } catch (UnauthorizedAccessException) { } }
        var files = Directory.EnumerateFiles(root, "*.png", SearchOption.AllDirectories).Select(path => new FileInfo(path)).OrderBy(f => f.LastAccessTimeUtc).ToList();
        var total = files.Sum(f => f.Length);
        foreach (var file in files)
        {
            if (total <= maximumBytes) break;
            try
            {
                using var lease = TryAcquireCleanupLease(file.FullName);
                if (lease is null) continue;
                var length = file.Length; file.Delete(); total -= length; removed++;
            }
            catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
        return removed;
    }
    public void Clear()
    {
        if (!Directory.Exists(root)) return;
        foreach (var file in Directory.EnumerateFiles(root, "*.png", SearchOption.AllDirectories))
        {
            try { using var lease = TryAcquireCleanupLease(file); if (lease is not null) File.Delete(file); }
            catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
        foreach (var partial in Directory.EnumerateFiles(root, "*.partial-*", SearchOption.AllDirectories))
        {
            try { if (DateTime.UtcNow - File.GetLastWriteTimeUtc(partial) > TimeSpan.FromDays(1)) File.Delete(partial); }
            catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }
    private static IDisposable? TryAcquireCleanupLease(string path)
    {
        var gate = ProcessLocks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
        if (!gate.Wait(0)) return null;
        try { var stream = new FileStream(path + ".lease", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); return new CacheLease(gate, path + ".lease", stream); }
        catch (IOException) { gate.Release(); return null; }
        catch { gate.Release(); throw; }
    }
    private sealed class CacheLease(SemaphoreSlim gate, string lockPath, FileStream stream) : IDisposable
    { public void Dispose() { stream.Dispose(); try { File.Delete(lockPath); } catch { } gate.Release(); } }
}
public static class DiagnosticExport
{
    public static void Write(string path, object value) { Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true })); }
}
