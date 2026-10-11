using Pdf2Hwp.Core;
using Pdf2Hwp.Infrastructure;
using System.Xml.Linq;
using System.Runtime.Versioning;

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
    [SupportedOSPlatform("windows")]
    public async Task Production_visual_fidelity_job_renders_duplicate_page_once_and_writes_page_backgrounds()
    {
        using var d = new TempDirectory(); var source = Path.Combine(d.Path, "rotated-cropped.pdf");
        await File.WriteAllBytesAsync(source, MinimalPdf.Create(rotation: 90, cropBox: [20, 30, 180, 170], pageCount: 2));
        var analyzer = new PdfPigAnalyzer(); var document = await analyzer.AnalyzeAsync(source, CancellationToken.None);
        var workspace = new DocumentWorkspace(); workspace.AddSource(SourceDocument.Create(source, document.PageCount), document.Pages); workspace.DuplicatePage(workspace.Pages[0].Id);
        var service = new ConversionJobService(analyzer, new HwpxWriter(), new HwpxPackageInspector(), new PdfiumPageRenderer(), new HwpxImagePackageWriter());
        var result = await service.RunAsync(workspace.CreateOutputSnapshot(), d.Path, RenderQuality.Standard, null, CancellationToken.None, "production-visual");
        Assert.Equal(new[] { 0, 0, 1 }, result.Report.Pages.Select(p => p.SourcePageIndex));
        Assert.Equal(1, result.Report.Pages.Count(p => p.ReusedRender));
        Assert.Equal(2, result.Report.Pages.Select(p => p.RenderHash).Distinct().Count());
        Assert.Equal(ValidationStatus.Pass, result.Report.Validation.Status);
        using var zip = System.IO.Compression.ZipFile.OpenRead(result.Report.OutputPath);
        Assert.Equal(3, zip.Entries.Count(entry => entry.FullName.StartsWith("Contents/section", StringComparison.Ordinal) && entry.FullName.EndsWith(".xml", StringComparison.Ordinal)));
        var manifest = System.Xml.Linq.XDocument.Load(zip.GetEntry("Contents/content.hpf")!.Open());
        XNamespace opf = "http://www.idpf.org/2007/opf/";
        Assert.Equal(2, manifest.Descendants(opf + "item").Count(item => ((string?)item.Attribute("media-type"))?.StartsWith("image/", StringComparison.Ordinal) == true));
        for (var index = 0; index < 3; index++)
        {
            var section = System.Xml.Linq.XDocument.Load(zip.GetEntry($"Contents/section{index}.xml")!.Open());
            XNamespace hp = "http://www.hancom.co.kr/hwpml/2011/paragraph";
            var pagePr = section.Descendants(hp + "pagePr").Single();
            Assert.True(long.Parse((string)pagePr.Attribute("width")!) > 0); Assert.True(long.Parse((string)pagePr.Attribute("height")!) > 0);
            Assert.Empty(section.Descendants(hp + "pic"));
        }
    }

    [Fact]
    public async Task Multi_source_job_keeps_selected_logical_order()
    {
        using var d = new TempDirectory(); var firstPath = Path.Combine(d.Path, "first.pdf"); var secondPath = Path.Combine(d.Path, "second.pdf"); File.WriteAllText(firstPath, "one"); File.WriteAllText(secondPath, "two");
        var workspace = new DocumentWorkspace(); workspace.AddSource(SourceDocument.Create(firstPath, 1)); workspace.AddSource(SourceDocument.Create(secondPath, 1));
        workspace.MovePage(workspace.Pages[1].Id, 0); workspace.DuplicatePage(workspace.Pages[0].Id);
        var service = new ConversionJobService(new FakeAnalyzer(), new CapturingWriter(), new FakeInspector());
        var result = await service.RunAsync(workspace.CreateOutputSnapshot(), d.Path, RenderQuality.Standard, null, CancellationToken.None, "multi-source");
        Assert.Equal(new[] { "second.pdf p.1", "second.pdf p.1", "first.pdf p.1" }, result.Report.Pages.Select(page => page.SourceDisplayName));
        Assert.Equal(new[] { 0, 0, 0 }, result.Report.Pages.Select(page => page.SourcePageIndex));
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
    public async Task Cancellation_during_validation_never_publishes_and_cleans_partial()
    {
        using var d = new TempDirectory(); var source = Path.Combine(d.Path, "source.pdf"); File.WriteAllText(source, "placeholder");
        var page = DocumentPage.FromSource(SourceDocument.Create(source, 1), 0); using var cancellation = new CancellationTokenSource();
        var inspector = new BlockingInspector(); var service = new ConversionJobService(new FakeAnalyzer(), new FakeWriter(), inspector);
        var run = Task.Run(() => service.RunAsync([page], d.Path, RenderQuality.Standard, null, cancellation.Token, "cancelled"));
        Assert.True(inspector.Entered.Wait(TimeSpan.FromSeconds(5)));
        cancellation.Cancel(); inspector.Release.Set();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        Assert.False(File.Exists(Path.Combine(d.Path, "cancelled.hwpx")));
        Assert.Empty(Directory.GetFiles(d.Path, "*.partial-*"));
        Assert.Empty(Directory.GetFiles(d.Path, "cancelled*.hwpx"));
    }

    [Fact]
    public async Task Warning_validation_is_reported_as_warning_not_pass()
    {
        using var d = new TempDirectory(); var source = Path.Combine(d.Path, "source.pdf"); File.WriteAllText(source, "placeholder"); var page = DocumentPage.FromSource(SourceDocument.Create(source, 1), 0);
        var progress = new ProgressRecorder(); var warning = new ValidationIssue(ValidationStatus.Warning, 1, "test-warning", "visual review required");
        var service = new ConversionJobService(new FakeAnalyzer(), new FakeWriter(), new WarningInspector(warning));
        var result = await service.RunAsync([page], d.Path, RenderQuality.Standard, progress, CancellationToken.None, "warning");
        Assert.Equal(ValidationStatus.Warning, result.Report.Validation.Status);
        Assert.Contains("visual review required", result.Report.Warnings);
        Assert.Contains(progress.Events, item => item.Status == ConversionJobStatus.CompletedWithWarnings);
        var history = new ConversionHistoryEntry(DateTimeOffset.UtcNow, ["source.pdf"], result.Report.OutputPath, 1, RenderQuality.Standard, TimeSpan.Zero, ConversionJobStatus.CompletedWithWarnings, "0.1.0");
        Assert.Equal("경고", history.StatusLabel);
        Assert.Equal(6, (int)ConversionJobStatus.Cancelled); Assert.Equal(7, (int)ConversionJobStatus.Failed); Assert.Equal(8, (int)ConversionJobStatus.CompletedWithWarnings);
        Assert.True(File.Exists(Path.Combine(d.Path, "warning.hwpx")));
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

    [Fact]
    public async Task Successful_visual_job_cleans_workspace_and_allows_the_next_job()
    {
        using var fixture = new WorkspaceFixture();
        var renderer = new TrackingPageRenderer(fixture.Factory);
        var service = CreateVisualService(fixture, renderer, new FakeInspector());

        var first = await RunVisualJob(service, fixture, "normal");
        fixture.AssertWorkspaceCleanup();
        var next = await RunVisualJob(service, fixture, "normal-next");

        Assert.True(File.Exists(first.Report.OutputPath));
        Assert.True(File.Exists(next.Report.OutputPath));
        fixture.AssertWorkspaceCleanup();
    }

    [Fact]
    public async Task Cancellation_cleans_workspace_and_allows_the_next_job()
    {
        using var fixture = new WorkspaceFixture();
        var renderer = new TrackingPageRenderer(fixture.Factory, blockFirstRender: true);
        var service = CreateVisualService(fixture, renderer, new FakeInspector());
        using var cancellation = new CancellationTokenSource();
        var run = RunVisualJob(service, fixture, "cancelled", cancellation.Token);
        await renderer.FirstRenderStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        fixture.AssertActiveMarkerPresent();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        Assert.False(File.Exists(Path.Combine(fixture.OutputDirectory, "cancelled.hwpx")));
        Assert.Empty(Directory.GetFiles(fixture.OutputDirectory, "*.partial-*"));
        fixture.AssertWorkspaceCleanup();
        var next = await RunVisualJob(service, fixture, "cancelled-retry");

        Assert.True(File.Exists(next.Report.OutputPath));
        fixture.AssertWorkspaceCleanup();
    }

    [Fact]
    public async Task Render_failure_cleans_workspace_and_allows_the_next_job()
    {
        using var fixture = new WorkspaceFixture();
        var renderer = new TrackingPageRenderer(fixture.Factory, failFirstRender: true);
        var service = CreateVisualService(fixture, renderer, new FakeInspector());

        await Assert.ThrowsAsync<IOException>(() => RunVisualJob(service, fixture, "render-failure"));

        Assert.False(File.Exists(Path.Combine(fixture.OutputDirectory, "render-failure.hwpx")));
        Assert.Empty(Directory.GetFiles(fixture.OutputDirectory, "*.partial-*"));
        fixture.AssertWorkspaceCleanup();
        var next = await RunVisualJob(service, fixture, "render-retry");

        Assert.True(File.Exists(next.Report.OutputPath));
        fixture.AssertWorkspaceCleanup();
    }

    [Fact]
    public async Task Validation_failure_cleans_workspace_and_allows_the_next_job()
    {
        using var fixture = new WorkspaceFixture();
        var renderer = new TrackingPageRenderer(fixture.Factory);
        var service = CreateVisualService(fixture, renderer, new FailFirstInspector(fixture.Factory));

        await Assert.ThrowsAsync<InvalidDataException>(() => RunVisualJob(service, fixture, "validation-failure"));

        Assert.False(File.Exists(Path.Combine(fixture.OutputDirectory, "validation-failure.hwpx")));
        Assert.Empty(Directory.GetFiles(fixture.OutputDirectory, "*.partial-*"));
        fixture.AssertWorkspaceCleanup();
        var next = await RunVisualJob(service, fixture, "validation-retry");

        Assert.True(File.Exists(next.Report.OutputPath));
        fixture.AssertWorkspaceCleanup();
    }

    [Fact]
    public async Task Publish_failure_cleans_workspace_preserves_collision_and_allows_the_next_job()
    {
        using var fixture = new WorkspaceFixture();
        var renderer = new TrackingPageRenderer(fixture.Factory);
        var inspector = new PublishCollisionInspector(fixture.Factory, fixture.OutputDirectory, "publish-failure.hwpx");
        var service = CreateVisualService(fixture, renderer, inspector);

        await Assert.ThrowsAsync<IOException>(() => RunVisualJob(service, fixture, "publish-failure"));

        var collidedOutput = Path.Combine(fixture.OutputDirectory, "publish-failure.hwpx");
        Assert.Equal("external collision", File.ReadAllText(collidedOutput));
        Assert.Empty(Directory.GetFiles(fixture.OutputDirectory, "*.partial-*"));
        Assert.Empty(Directory.GetFiles(fixture.OutputDirectory, ".*.tmp"));
        fixture.AssertWorkspaceCleanup();
        var next = await RunVisualJob(service, fixture, "publish-retry");

        Assert.True(File.Exists(next.Report.OutputPath));
        Assert.Equal("external collision", File.ReadAllText(collidedOutput));
        fixture.AssertWorkspaceCleanup();
    }

    private static ConversionJobService CreateVisualService(WorkspaceFixture fixture, IPdfPageRenderer renderer, IHwpxPackageInspector inspector)
        => new(new FakeAnalyzer(), new FakeWriter(), inspector, renderer, new HwpxImagePackageWriter(), fixture.Factory);

    private static Task<ConversionJobResult> RunVisualJob(ConversionJobService service, WorkspaceFixture fixture, string outputName, CancellationToken token = default)
        => service.RunAsync([DocumentPage.FromSource(SourceDocument.Create(fixture.SourcePath, 1), 0)], fixture.OutputDirectory, RenderQuality.Standard, null, token, outputName);

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
    private sealed class WarningInspector(ValidationIssue warning) : IHwpxPackageInspector { public ValidationReport Inspect(string path) => new(ValidationStatus.Warning, [warning]); }
    private sealed class BlockingInspector : IHwpxPackageInspector
    {
        public ManualResetEventSlim Entered { get; } = new();
        public ManualResetEventSlim Release { get; } = new();
        public ValidationReport Inspect(string path) { Entered.Set(); Release.Wait(TimeSpan.FromSeconds(5)); return new(ValidationStatus.Pass, []); }
    }
    private sealed class TrackingPageRenderer(TrackingWorkspaceFactory factory, bool failFirstRender = false, bool blockFirstRender = false) : IPdfPageRenderer
    {
        private int _calls;
        public TaskCompletionSource FirstRenderStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task RenderAsync(RenderRequest request, CancellationToken token)
        {
            factory.AssertActiveMarkerPresent();
            var call = Interlocked.Increment(ref _calls);
            FirstRenderStarted.TrySetResult();
            if (call == 1 && blockFirstRender) await Task.Delay(Timeout.InfiniteTimeSpan, token);
            if (call == 1 && failFirstRender) throw new IOException("synthetic render failure");
            await File.WriteAllBytesAsync(request.OutputPngPath, Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII="), token);
        }
    }
    private sealed class FailFirstInspector(TrackingWorkspaceFactory factory) : IHwpxPackageInspector
    {
        private int _calls;
        public ValidationReport Inspect(string path)
        {
            factory.AssertActiveMarkerPresent();
            return Interlocked.Increment(ref _calls) == 1
                ? new(ValidationStatus.Fail, [new ValidationIssue(ValidationStatus.Fail, null, "synthetic-validation-failure", "synthetic validation failure")])
                : new(ValidationStatus.Pass, []);
        }
    }
    private sealed class PublishCollisionInspector(TrackingWorkspaceFactory factory, string outputDirectory, string targetName) : IHwpxPackageInspector
    {
        private int _calls;
        public ValidationReport Inspect(string path)
        {
            factory.AssertActiveMarkerPresent();
            if (Interlocked.Increment(ref _calls) == 1) File.WriteAllText(Path.Combine(outputDirectory, targetName), "external collision");
            return new(ValidationStatus.Pass, []);
        }
    }
    private sealed class TrackingWorkspaceFactory : IConversionWorkspaceFactory
    {
        private readonly ConversionWorkspaceFactory _inner = new();
        private readonly List<string> _paths = [];
        public IReadOnlyList<string> Paths { get { lock (_paths) return _paths.ToArray(); } }
        public IConversionWorkspace Create()
        {
            var workspace = _inner.Create();
            lock (_paths) _paths.Add(workspace.DirectoryPath);
            return new TrackedWorkspace(workspace);
        }
        public int CleanupStale(TimeSpan olderThan) => _inner.CleanupStale(olderThan);
        public void AssertActiveMarkerPresent()
        {
            var path = Paths.LastOrDefault();
            Assert.NotNull(path);
            Assert.True(File.Exists(Path.Combine(path!, ".pdf2hwp-workspace")));
            Assert.True(File.Exists(Path.Combine(path!, ".active")));
        }
        public void AssertAllDisposed()
        {
            Assert.NotEmpty(Paths);
            foreach (var path in Paths)
            {
                Assert.False(Directory.Exists(path));
                Assert.False(File.Exists(Path.Combine(path, ".active")));
                Assert.False(File.Exists(Path.Combine(path, ".pdf2hwp-workspace")));
            }
        }
        private sealed class TrackedWorkspace(IConversionWorkspace inner) : IConversionWorkspace
        {
            public string DirectoryPath => inner.DirectoryPath;
            public string CreateFilePath(string extension) => inner.CreateFilePath(extension);
            public void Dispose() => inner.Dispose();
        }
    }
    private sealed class WorkspaceFixture : IDisposable
    {
        private readonly TempDirectory _directory = new();
        private readonly string _unrelatedPath;
        private const string ActiveContents = "unrelated active workspace";
        public string OutputDirectory { get; }
        public string SourcePath { get; }
        public TrackingWorkspaceFactory Factory { get; } = new();
        public WorkspaceFixture()
        {
            OutputDirectory = Path.Combine(_directory.Path, "output"); Directory.CreateDirectory(OutputDirectory);
            SourcePath = Path.Combine(_directory.Path, "source.pdf"); File.WriteAllText(SourcePath, "placeholder");
            _unrelatedPath = Path.Combine(_directory.Path, "unrelated-job"); Directory.CreateDirectory(_unrelatedPath);
            File.WriteAllText(Path.Combine(_unrelatedPath, ".pdf2hwp-workspace"), "unrelated marker");
            File.WriteAllText(Path.Combine(_unrelatedPath, ".active"), ActiveContents);
        }
        public void AssertActiveMarkerPresent() => Factory.AssertActiveMarkerPresent();
        public void AssertWorkspaceCleanup()
        {
            Factory.AssertAllDisposed();
            Assert.True(Directory.Exists(_unrelatedPath));
            Assert.Equal("unrelated marker", File.ReadAllText(Path.Combine(_unrelatedPath, ".pdf2hwp-workspace")));
            Assert.Equal(ActiveContents, File.ReadAllText(Path.Combine(_unrelatedPath, ".active")));
        }
        public void Dispose() => _directory.Dispose();
    }
    private sealed class ProgressRecorder : IProgress<ConversionProgress> { public List<ConversionProgress> Events { get; } = []; public void Report(ConversionProgress value) => Events.Add(value); }
    private sealed class TempDirectory : IDisposable { public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N")); public TempDirectory() => Directory.CreateDirectory(Path); public void Dispose() { try { Directory.Delete(Path, true); } catch { } } }
}
