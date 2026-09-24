using Pdf2Hwp.Core;

namespace Pdf2Hwp.Infrastructure;

public sealed class ConversionWorkspaceFactory : IConversionWorkspaceFactory
{
    private const string RootName = "PDF2HWP";
    private readonly string _root = Path.Combine(Path.GetTempPath(), RootName);
    public IConversionWorkspace Create()
    {
        Directory.CreateDirectory(_root); var path = Path.Combine(_root, Guid.NewGuid().ToString("N")); Directory.CreateDirectory(path); File.WriteAllText(Path.Combine(path, ".pdf2hwp-workspace"), $"pid={Environment.ProcessId}\ncreated={DateTimeOffset.UtcNow:O}"); return new Workspace(path);
    }
    public int CleanupStale(TimeSpan olderThan)
    {
        if (!Directory.Exists(_root)) return 0; var threshold = DateTime.UtcNow - olderThan; var removed = 0;
        foreach (var directory in Directory.EnumerateDirectories(_root))
        {
            if (!File.Exists(Path.Combine(directory, ".pdf2hwp-workspace")) || Directory.GetLastWriteTimeUtc(directory) >= threshold) continue;
            var activePath = Path.Combine(directory, ".active");
            if (File.Exists(activePath))
            {
                try
                {
                    var pidLine = File.ReadLines(activePath).FirstOrDefault(line => line.StartsWith("pid=", StringComparison.Ordinal));
                    if (pidLine is null || !int.TryParse(pidLine.AsSpan(4), out var pid)) continue;
                    try { using var process = System.Diagnostics.Process.GetProcessById(pid); if (!process.HasExited) continue; }
                    catch (ArgumentException) { }
                    File.Delete(activePath);
                }
                catch (IOException) { continue; } catch (UnauthorizedAccessException) { continue; }
            }
            try { Directory.Delete(directory, true); removed++; } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
        return removed;
    }
    private sealed class Workspace : IConversionWorkspace
    {
        public string DirectoryPath { get; }
        public Workspace(string path) { DirectoryPath = path; File.WriteAllText(Path.Combine(path, ".active"), $"pid={Environment.ProcessId}\nstarted={DateTimeOffset.UtcNow:O}"); }
        public string CreateFilePath(string extension) => Path.Combine(DirectoryPath, Guid.NewGuid().ToString("N") + extension);
        public void Dispose() { try { File.Delete(Path.Combine(DirectoryPath, ".active")); Directory.Delete(DirectoryPath, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }
}
