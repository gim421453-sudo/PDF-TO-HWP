using Pdf2Hwp.Core;
using Pdf2Hwp.Infrastructure;

namespace Pdf2Hwp.Tests;

public sealed class ConversionJobServiceTests
{
    [Fact]
    public async Task Job_uses_immutable_snapshot_and_reports_progress()
    {
        using var d = new TempDirectory();
        var source = Path.Combine(d.Path, "source.pdf"); File.WriteAllText(source, "placeholder");
        var page = DocumentPage.FromSource(SourceDocument.Create(source, 1), 0);
        var progress = new List<ConversionProgress>();
        var service = new ConversionJobService(new FakeAnalyzer(), new FakeWriter(), new FakeInspector());
        var result = await service.RunAsync([page], d.Path, RenderQuality.High, new Progress<ConversionProgress>(progress.Add), CancellationToken.None);
        Assert.Single(result.OutputFiles); Assert.Equal(1, result.Report.SelectedPageCount); Assert.NotEmpty(progress);
    }

    [Fact]
    public async Task Job_cancellation_does_not_leave_partial_output()
    {
        using var d = new TempDirectory(); var source = Path.Combine(d.Path, "source.pdf"); File.WriteAllText(source, "x");
        var page = DocumentPage.FromSource(SourceDocument.Create(source, 1), 0); using var cts = new CancellationTokenSource(); cts.Cancel();
        var service = new ConversionJobService(new FakeAnalyzer(), new FakeWriter(), new FakeInspector());
        await Assert.ThrowsAsync<OperationCanceledException>(() => service.RunAsync([page], d.Path, RenderQuality.Standard, null, cts.Token));
        Assert.Empty(Directory.GetFiles(d.Path, "*.partial-*"));
    }

    [Fact]
    public async Task Job_writes_only_selected_pages_in_logical_order_and_marks_reuse()
    {
        using var d = new TempDirectory(); var source = Path.Combine(d.Path, "source.pdf"); File.WriteAllText(source, "placeholder");
        var document = new PdfDocumentInfo(source, 3, Enumerable.Range(1, 3).Select(i => new PdfPageInfo(i, 595, 842, 0, null, true, false, [])).ToArray());
        var workspace = new DocumentWorkspace(); var sourceDoc = SourceDocument.Create(source, 3); workspace.AddSource(sourceDoc); var pages = workspace.Pages.ToArray(); workspace.SetIncluded(pages[1].Id, false); workspace.MovePage(pages[2].Id, 0);
        var writer = new CapturingWriter(); var service = new ConversionJobService(new FixedAnalyzer(document), writer, new FakeInspector());
        var result = await service.RunAsync(workspace.CreateOutputSnapshot(), d.Path, RenderQuality.Standard, null, CancellationToken.None);
        Assert.Equal(new[] { 1, 2 }, writer.Last!.Pages.Select(p => p.Number)); Assert.Equal(2, result.Report.SelectedPageCount); Assert.Equal(new[] { 2, 0 }, result.Report.Pages.Select(p => p.SourcePageIndex));
    }

    [Fact]
    public async Task Duplicate_page_is_emitted_twice_and_conversion_report_preserves_logical_mapping()
    {
        using var d = new TempDirectory(); var source = Path.Combine(d.Path, "source.pdf"); File.WriteAllText(source, "placeholder");
        var document = new PdfDocumentInfo(source, 1, [new PdfPageInfo(1, 595, 842, 0, null, true, false, [])]); var workspace = new DocumentWorkspace(); workspace.AddSource(SourceDocument.Create(source, 1)); workspace.DuplicatePage(workspace.Pages[0].Id);
        var writer = new CapturingWriter(); var service = new ConversionJobService(new FixedAnalyzer(document), writer, new FakeInspector()); var result = await service.RunAsync(workspace.CreateOutputSnapshot(), d.Path, RenderQuality.Standard, null, CancellationToken.None);
        Assert.Equal(2, writer.Last!.PageCount); Assert.Equal(2, result.Report.Pages.Count); Assert.Equal(new[] { 0, 0 }, result.Report.Pages.Select(p => p.SourcePageIndex)); Assert.All(result.Report.Pages, p => Assert.False(p.ReusedRender));
    }

    [Fact]
    public async Task Concurrent_jobs_never_overwrite_the_same_output_or_leave_partial_files()
    {
        using var d = new TempDirectory(); var source = Path.Combine(d.Path, "source.pdf"); File.WriteAllText(source, "placeholder");
        var page = DocumentPage.FromSource(SourceDocument.Create(source, 1), 0);
        var writer = new BarrierWriter(); var service = new ConversionJobService(new FakeAnalyzer(), writer, new FakeInspector());
        var first = service.RunAsync([page], d.Path, RenderQuality.Standard, null, CancellationToken.None, "collision");
        var second = service.RunAsync([page with { Id = Guid.NewGuid() }], d.Path, RenderQuality.Standard, null, CancellationToken.None, "collision");
        var results = await Task.WhenAll(Capture(first), Capture(second));
        Assert.Single(results, result => result.Error is null);
        Assert.Single(results, result => result.Error is IOException);
        Assert.Equal("valid", File.ReadAllText(Path.Combine(d.Path, "collision.hwpx")));
        Assert.Empty(Directory.GetFiles(d.Path, "*.partial-*"));
    }

    [Fact]
    public async Task Existing_output_is_never_overwritten_and_retry_gets_a_new_job_id()
    {
        using var d = new TempDirectory(); var source = Path.Combine(d.Path, "source.pdf"); File.WriteAllText(source, "placeholder"); var existing = Path.Combine(d.Path, "wanted.hwpx"); File.WriteAllText(existing, "user data");
        var page = DocumentPage.FromSource(SourceDocument.Create(source, 1), 0); var service = new ConversionJobService(new FakeAnalyzer(), new FakeWriter(), new FakeInspector());
        var first = await service.RunAsync([page], d.Path, RenderQuality.Standard, null, CancellationToken.None, "wanted"); var second = await service.RunAsync([page], d.Path, RenderQuality.Standard, null, CancellationToken.None, "wanted");
        Assert.Equal("user data", File.ReadAllText(existing)); Assert.EndsWith("wanted_2.hwpx", first.Report.OutputPath); Assert.EndsWith("wanted_3.hwpx", second.Report.OutputPath); Assert.NotEqual(first.Report.JobId, second.Report.JobId);
    }

    [Fact]
    public async Task Failed_validation_removes_partial_and_never_publishes_final_file()
    {
        using var d = new TempDirectory(); var source = Path.Combine(d.Path, "source.pdf"); File.WriteAllText(source, "placeholder"); var page = DocumentPage.FromSource(SourceDocument.Create(source, 1), 0);
        var service = new ConversionJobService(new FakeAnalyzer(), new FakeWriter(), new FailingInspector());
        await Assert.ThrowsAsync<InvalidDataException>(() => service.RunAsync([page], d.Path, RenderQuality.Standard, null, CancellationToken.None, "invalid"));
        Assert.False(File.Exists(Path.Combine(d.Path, "invalid.hwpx"))); Assert.Empty(Directory.GetFiles(d.Path, "*.partial-*"));
    }

    [Fact]
    public async Task Progress_uses_logical_page_count_including_duplicates()
    {
        using var d = new TempDirectory(); var source = Path.Combine(d.Path, "source.pdf"); File.WriteAllText(source, "placeholder"); var document = new PdfDocumentInfo(source, 3, Enumerable.Range(1, 3).Select(i => new PdfPageInfo(i, 595, 842, 0, null, true, false, [])).ToArray());
        var workspace = new DocumentWorkspace(); workspace.AddSource(SourceDocument.Create(source, 3)); workspace.DuplicatePage(workspace.Pages[1].Id); workspace.DuplicatePage(workspace.Pages[1].Id);
        var progress = new ProgressRecorder(); var service = new ConversionJobService(new FixedAnalyzer(document), new CapturingWriter(), new FakeInspector());
        await service.RunAsync(workspace.CreateOutputSnapshot(), d.Path, RenderQuality.Standard, progress, CancellationToken.None);
        var logical = progress.Events.Where(e => e.Stage == "선택된 페이지 구성").ToArray(); Assert.Equal(5, logical.Length); Assert.Equal(Enumerable.Range(1, 5), logical.Select(e => e.Current)); Assert.All(logical, e => Assert.Equal(5, e.Total));
    }

    private static async Task<(ConversionJobResult? Result, Exception? Error)> Capture(Task<ConversionJobResult> task)
    { try { return (await task, null); } catch (Exception ex) { return (null, ex); } }

    [Fact] public void Invalid_output_name_is_rejected() => Assert.Throws<ArgumentException>(() => OutputPathValidator.ValidateFileName("bad<name>.hwpx"));

    private sealed class FakeAnalyzer : IPdfAnalyzer { public Task<PdfDocumentInfo> AnalyzeAsync(string path, CancellationToken token) => Task.FromResult(new PdfDocumentInfo(path, 1, [new PdfPageInfo(1, 595, 842, 0, null, true, false, [])])); }
    private sealed class FixedAnalyzer(PdfDocumentInfo document) : IPdfAnalyzer { public Task<PdfDocumentInfo> AnalyzeAsync(string path, CancellationToken token) => Task.FromResult(document); }
    private sealed class CapturingWriter : IHwpxWriter { public PdfDocumentInfo? Last; public Task WriteAsync(PdfDocumentInfo document, string outputPath, CancellationToken token) { Last = document; File.WriteAllText(outputPath, "fake"); return Task.CompletedTask; } }
    private sealed class FakeWriter : IHwpxWriter { public Task WriteAsync(PdfDocumentInfo document, string outputPath, CancellationToken token) { File.WriteAllText(outputPath, "fake"); return Task.CompletedTask; } }
    private sealed class BarrierWriter : IHwpxWriter
    {
        private int _arrived;
        private readonly TaskCompletionSource _bothReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task WriteAsync(PdfDocumentInfo document, string outputPath, CancellationToken token)
        {
            if (Interlocked.Increment(ref _arrived) == 2) _bothReady.TrySetResult();
            await _bothReady.Task.WaitAsync(token);
            await File.WriteAllTextAsync(outputPath, "valid", token);
        }
    }
    private sealed class FakeInspector : IHwpxPackageInspector { public ValidationReport Inspect(string path) => new(ValidationStatus.Pass, []); }
    private sealed class FailingInspector : IHwpxPackageInspector { public ValidationReport Inspect(string path) => new(ValidationStatus.Fail, [new ValidationIssue(ValidationStatus.Fail, null, "test", "invalid package")]); }
    private sealed class ProgressRecorder : IProgress<ConversionProgress> { public List<ConversionProgress> Events { get; } = []; public void Report(ConversionProgress value) => Events.Add(value); }
    private sealed class TempDirectory : IDisposable { public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N")); public TempDirectory() => Directory.CreateDirectory(Path); public void Dispose() { try { Directory.Delete(Path, true); } catch { } } }
}
