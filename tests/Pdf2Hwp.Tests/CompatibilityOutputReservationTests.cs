using Pdf2Hwp.Infrastructure;

namespace Pdf2Hwp.Tests;

public sealed class CompatibilityOutputReservationTests
{
    [Fact]
    public async Task Concurrent_generators_reserving_the_same_path_have_one_owner_and_no_loser_partial_files()
    {
        using var directory = new TempDirectory();
        var output = Path.Combine(directory.Path, "samples");
        using var start = new Barrier(2);

        var attempts = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => Task.Run(() =>
        {
            Assert.True(start.SignalAndWait(TimeSpan.FromSeconds(5)));
            try { return new ReservationAttempt(CompatibilityOutputReservation.Acquire(output), null); }
            catch (IOException exception) { return new ReservationAttempt(null, exception); }
        })));

        var winner = Assert.Single(attempts, attempt => attempt.Reservation is not null);
        Assert.IsType<IOException>(Assert.Single(attempts, attempt => attempt.Error is not null).Error);
        using (winner.Reservation!)
        {
            var finalPath = Path.Combine(output, "sample.hwpx");
            File.WriteAllText(finalPath, "winner");
            Assert.Equal("winner", File.ReadAllText(finalPath));
            Assert.Empty(Directory.GetFiles(output, "*.partial-*"));
            Assert.Single(Directory.GetFiles(output, ".pdf2hwp-output-reservation"));
        }

        Assert.Equal("winner", File.ReadAllText(Path.Combine(output, "sample.hwpx")));
        Assert.Empty(Directory.GetFiles(output, "*.partial-*"));
        Assert.Empty(Directory.GetFiles(output, ".pdf2hwp-output-reservation"));
    }

    [Fact]
    public void Existing_output_file_is_preserved_and_nonempty_directory_is_rejected()
    {
        using var directory = new TempDirectory();
        var output = Path.Combine(directory.Path, "samples");
        Directory.CreateDirectory(output);
        var existing = Path.Combine(output, "23-protected-sample.hwpx");
        File.WriteAllText(existing, "protected content");

        Assert.Throws<IOException>(() => CompatibilityOutputReservation.Acquire(output));

        Assert.Equal("protected content", File.ReadAllText(existing));
        Assert.Empty(Directory.GetFiles(output, "*.partial-*"));
        Assert.Empty(Directory.GetFiles(output, ".pdf2hwp-output-reservation"));
    }

    private sealed record ReservationAttempt(CompatibilityOutputReservation? Reservation, Exception? Error);

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        public TempDirectory() => Directory.CreateDirectory(Path);
        public void Dispose() { try { Directory.Delete(Path, true); } catch { } }
    }
}
