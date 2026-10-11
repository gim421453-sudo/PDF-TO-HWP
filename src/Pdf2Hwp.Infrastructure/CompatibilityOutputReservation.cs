namespace Pdf2Hwp.Infrastructure;

/// <summary>Exclusively reserves an empty compatibility-sample output directory for one generator process.</summary>
public sealed class CompatibilityOutputReservation : IDisposable
{
    private const string MarkerName = ".pdf2hwp-output-reservation";
    private readonly string _markerPath;
    private FileStream? _marker;

    private CompatibilityOutputReservation(string markerPath, FileStream marker)
    {
        _markerPath = markerPath;
        _marker = marker;
    }

    public static CompatibilityOutputReservation Acquire(string outputDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        var fullPath = Path.GetFullPath(outputDirectory);
        if (File.Exists(fullPath))
            throw new IOException($"출력 경로가 파일입니다. 기존 파일을 보존합니다: {fullPath}");

        var markerPath = Path.Combine(fullPath, MarkerName);
        EnsureEmptyExceptReservation(fullPath, markerPath);
        Directory.CreateDirectory(fullPath);

        var marker = OpenReservationMarker(markerPath);
        try
        {
            EnsureEmptyExceptReservation(fullPath, markerPath);
            return new CompatibilityOutputReservation(markerPath, marker);
        }
        catch
        {
            marker.Dispose();
            TryDeleteMarker(markerPath);
            throw;
        }
    }

    public void Dispose()
    {
        var marker = Interlocked.Exchange(ref _marker, null);
        if (marker is null) return;
        marker.Dispose();
        TryDeleteMarker(_markerPath);
    }

    private static FileStream OpenReservationMarker(string markerPath)
    {
        try
        {
            return new FileStream(markerPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException createError) when (File.Exists(markerPath))
        {
            try
            {
                // Reopen a marker left by a terminated process only after obtaining an exclusive handle.
                return new FileStream(markerPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                throw new IOException($"다른 호환성 샘플 생성 작업이 출력 경로를 사용 중입니다: {Path.GetDirectoryName(markerPath)}", createError);
            }
        }
    }

    private static void EnsureEmptyExceptReservation(string directory, string markerPath)
    {
        if (!Directory.Exists(directory)) return;
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var fullMarkerPath = Path.GetFullPath(markerPath);
        var otherEntry = Directory.EnumerateFileSystemEntries(directory)
            .FirstOrDefault(entry => !string.Equals(Path.GetFullPath(entry), fullMarkerPath, comparison));
        if (otherEntry is not null)
            throw new IOException($"출력 디렉터리가 비어 있지 않습니다. 기존 파일을 보존합니다: {directory}");
    }

    private static void TryDeleteMarker(string markerPath)
    {
        try { if (File.Exists(markerPath)) File.Delete(markerPath); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
