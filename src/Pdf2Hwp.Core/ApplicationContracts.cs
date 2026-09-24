using System.Security.Cryptography;

namespace Pdf2Hwp.Core;

public enum ConversionJobStatus { Queued, Analyzing, Rendering, Writing, Validating, Completed, Cancelled, Failed }
public enum RenderQuality { Standard = 200, High = 300 }
public sealed record ConversionProgress(ConversionJobStatus Status, string Stage, int Current, int Total, double Percentage);
public sealed record ConversionJob(Guid Id, DateTimeOffset CreatedAt, IReadOnlyList<DocumentPage> Pages, RenderQuality Quality, string OutputPath, ConversionJobStatus Status = ConversionJobStatus.Queued);
public sealed record PageConversionReport(Guid StablePageId, string SourceDisplayName, int SourcePageIndex, int LogicalOutputIndex, string? RenderHash, TimeSpan RenderDuration, bool ReusedRender = false);
public sealed record ConversionStageDurations(TimeSpan Analyze, TimeSpan Render, TimeSpan Write, TimeSpan Validate, TimeSpan Publish, TimeSpan Total);
public sealed record ConversionReport(string AppVersion, Guid JobId, DateTimeOffset StartedAt, DateTimeOffset CompletedAt, int SourceDocumentCount, int SelectedPageCount, string OutputPath, long OutputSize, RenderQuality RenderQuality, ValidationReport Validation, IReadOnlyList<string> Warnings, IReadOnlyList<PageConversionReport> Pages, ConversionStageDurations? Durations = null);
public static class OutputNamingPolicy
{
    public static string Choose(string directory, string sourceName, bool combined = false, string? requestedStem = null)
    {
        Directory.CreateDirectory(directory);
        if (!string.IsNullOrWhiteSpace(requestedStem) && requestedStem.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) throw new ArgumentException("파일명이 올바르지 않습니다.", nameof(requestedStem));
        var stem = string.IsNullOrWhiteSpace(requestedStem) ? combined ? "PDF2HWP_combined" : Path.GetFileNameWithoutExtension(sourceName) + "_converted" : Path.GetFileNameWithoutExtension(requestedStem.Trim());
        var candidate = Path.Combine(directory, stem + ".hwpx");
        for (var i = 2; File.Exists(candidate); i++) candidate = Path.Combine(directory, $"{stem}_{i}.hwpx");
        return candidate;
    }
}
public sealed record AppSettings(string LastOutputDirectory = "", RenderQuality RenderQuality = RenderQuality.Standard, double WindowWidth = 820, double WindowHeight = 590, string WindowState = "Normal", bool RememberLastFolder = true, int SchemaVersion = 1);
public sealed class JsonSettingsStore(string path)
{
    private readonly System.Text.Json.JsonSerializerOptions _options = new() { WriteIndented = true, Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };
    public AppSettings Load() { try { return File.Exists(path) ? System.Text.Json.JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), _options) ?? new() : new(); } catch { return new(); } }
    public void Save(AppSettings settings) { AtomicJson.Write(path, System.Text.Json.JsonSerializer.Serialize(settings, _options)); }
}
public sealed class RecentFilesStore(string path, int maxEntries = 10)
{
    public IReadOnlyList<(string Path, DateTimeOffset OpenedAt)> Load() { try { if (!File.Exists(path)) return []; return System.Text.Json.JsonSerializer.Deserialize<List<RecentEntry>>(File.ReadAllText(path))?.Where(x => File.Exists(x.Path)).Take(maxEntries).Select(x => (x.Path, x.OpenedAt)).ToArray() ?? []; } catch { return []; } }
    public void Add(string file) => AtomicJson.Update(path, old =>
    {
        List<RecentEntry> list; try { list = System.Text.Json.JsonSerializer.Deserialize<List<RecentEntry>>(old ?? "[]") ?? []; } catch { list = []; }
        list = list.Where(x => File.Exists(x.Path) && !string.Equals(x.Path, file, StringComparison.OrdinalIgnoreCase)).ToList();
        list.Insert(0, new(file, DateTimeOffset.UtcNow)); return System.Text.Json.JsonSerializer.Serialize(list.Take(maxEntries));
    });
    private sealed record RecentEntry(string Path, DateTimeOffset OpenedAt);
}

public sealed record HistoryPageEntry(string SourcePath, int SourcePageIndex);
public sealed record ConversionHistoryEntry(DateTimeOffset Timestamp, IReadOnlyList<string> SourceDisplayNames, string OutputPath, int PageCount, RenderQuality RenderQuality, TimeSpan Duration, ConversionJobStatus Status, string AppVersion, IReadOnlyList<HistoryPageEntry>? Pages = null, int SchemaVersion = 1)
{
    public string OutputFileName => Path.GetFileName(OutputPath);
    public string Summary => $"{Timestamp.ToLocalTime():yyyy-MM-dd HH:mm} · {SourceDisplayNames.Count} PDF · {PageCount}쪽 · {Duration.TotalSeconds:0.0}s";
    public string StatusLabel => Status switch { ConversionJobStatus.Completed => "완료", ConversionJobStatus.Cancelled => "취소", ConversionJobStatus.Failed => "실패", _ => Status.ToString() };
    public bool CanRetry => Status is ConversionJobStatus.Failed or ConversionJobStatus.Cancelled && Pages is { Count: > 0 } && Pages.All(p => File.Exists(p.SourcePath));
}
public sealed class ConversionHistoryStore(string path, int maxEntries = 20)
{
    public IReadOnlyList<ConversionHistoryEntry> Load() { try { if (!File.Exists(path)) return []; return System.Text.Json.JsonSerializer.Deserialize<List<ConversionHistoryEntry>>(File.ReadAllText(path))?.Take(maxEntries).ToArray() ?? []; } catch { return []; } }
    public void Add(ConversionHistoryEntry entry) => AtomicJson.Update(path, old =>
    {
        List<ConversionHistoryEntry> items; try { items = System.Text.Json.JsonSerializer.Deserialize<List<ConversionHistoryEntry>>(old ?? "[]") ?? []; } catch { items = []; }
        return System.Text.Json.JsonSerializer.Serialize(new[] { entry }.Concat(items).Take(maxEntries));
    });
    public void Clear() => AtomicJson.Write(path, "[]");
    public void RemoveAt(int index) => AtomicJson.Update(path, old =>
    {
        List<ConversionHistoryEntry> items; try { items = System.Text.Json.JsonSerializer.Deserialize<List<ConversionHistoryEntry>>(old ?? "[]") ?? []; } catch { items = []; }
        if (index >= 0 && index < items.Count) items.RemoveAt(index);
        return System.Text.Json.JsonSerializer.Serialize(items);
    });
}
internal static class AtomicJson
{
    public static void Write(string path, string json)
    {
        WithMutex(path, () => WriteUnlocked(path, json));
    }
    public static void Update(string path, Func<string?, string> update)
    {
        WithMutex(path, () =>
        {
            string? old = null; try { if (File.Exists(path)) old = File.ReadAllText(path); } catch { }
            WriteUnlocked(path, update(old));
        });
    }
    private static void WriteUnlocked(string path, string json)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp = path + ".partial-" + Guid.NewGuid().ToString("N");
        try { File.WriteAllText(temp, json); File.Move(temp, path, true); }
        finally { try { if (File.Exists(temp)) File.Delete(temp); } catch { } }
    }
    private static void WithMutex(string path, Action action)
    {
        var identity = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Path.GetFullPath(path)));
        using var mutex = new Mutex(false, "Local\\PDF2HWP-State-" + Convert.ToHexString(identity.AsSpan(0, 12)));
        var acquired = false;
        try { try { acquired = mutex.WaitOne(TimeSpan.FromSeconds(30)); } catch (AbandonedMutexException) { acquired = true; } if (!acquired) throw new TimeoutException("Timed out waiting for application state lock."); action(); }
        finally { if (acquired) mutex.ReleaseMutex(); }
    }
}

public static class ConversionErrorMapper
{
    public static string ToUserMessage(Exception error) => error switch
    {
        OperationCanceledException => "변환이 취소되었습니다.",
        UnauthorizedAccessException => "파일 또는 출력 폴더에 접근할 수 없습니다.",
        DirectoryNotFoundException => "출력 폴더를 찾을 수 없습니다.",
        InvalidDataException => "PDF 또는 HWPX 구조가 올바르지 않습니다.",
        NotSupportedException => error.Message,
        _ => error.Message
    };
}
