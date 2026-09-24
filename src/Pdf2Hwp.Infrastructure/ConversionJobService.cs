using Pdf2Hwp.Core;
using System.Diagnostics;

namespace Pdf2Hwp.Infrastructure;

public sealed record ConversionJobResult(ConversionReport Report, IReadOnlyList<string> OutputFiles);

/// <summary>Application-level orchestration boundary. The workspace snapshot is copied before any async work starts.</summary>
public interface IConversionJobRunner
{
    Task<ConversionJobResult> RunAsync(IReadOnlyList<DocumentPage> pages, string outputDirectory, RenderQuality quality, IProgress<ConversionProgress>? progress, CancellationToken cancellationToken, string? outputName = null);
}

public sealed class ConversionJobService(
    IPdfAnalyzer analyzer,
    IHwpxWriter writer,
    IHwpxPackageInspector inspector) : IConversionJobRunner
{
    public async Task<ConversionJobResult> RunAsync(
        IReadOnlyList<DocumentPage> pages,
        string outputDirectory,
        RenderQuality quality,
        IProgress<ConversionProgress>? progress,
        CancellationToken cancellationToken,
        string? outputName = null)
    {
        ArgumentNullException.ThrowIfNull(pages);
        var snapshot = pages.Select(p => p with { OperationHistory = p.OperationHistory.ToArray() }).ToArray();
        if (snapshot.Length == 0) throw new InvalidOperationException("No pages selected for output.");
        OutputPathValidator.ValidateDirectory(outputDirectory);
        var jobId = Guid.NewGuid();
        var started = DateTimeOffset.UtcNow;
        var totalWatch = Stopwatch.StartNew(); var analyzeDuration = TimeSpan.Zero; var writeDuration = TimeSpan.Zero; var validateDuration = TimeSpan.Zero; var publishDuration = TimeSpan.Zero;
        var outputs = new List<string>();
        var pageReports = new List<PageConversionReport>();
        var warnings = new List<string>();
        var sourcePaths = snapshot.Select(p => p.SourcePath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var analyzed = new Dictionary<string, PdfDocumentInfo>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < sourcePaths.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new(ConversionJobStatus.Analyzing, "PDF 분석", index, sourcePaths.Length, index * 30d / sourcePaths.Length));
            var analyzeWatch = Stopwatch.StartNew(); analyzed[sourcePaths[index]] = await analyzer.AnalyzeAsync(sourcePaths[index], cancellationToken).ConfigureAwait(false); analyzeDuration += analyzeWatch.Elapsed;
        }
        // The logical snapshot, not source order, is the sole writer input.
        var selectedPages = new PdfPageInfo[snapshot.Length];
        for (var outputIndex = 0; outputIndex < snapshot.Length; outputIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var page = snapshot[outputIndex];
            var source = analyzed[page.SourcePath];
            if (page.SourcePageIndex < 0 || page.SourcePageIndex >= source.Pages.Count)
                throw new InvalidDataException($"페이지 인덱스가 PDF 범위를 벗어났습니다: {page.DisplayName}");
            selectedPages[outputIndex] = source.Pages[page.SourcePageIndex] with { Number = outputIndex + 1 };
            progress?.Report(new(ConversionJobStatus.Writing, "선택된 페이지 구성", outputIndex + 1, snapshot.Length, 30 + 5d * (outputIndex + 1) / snapshot.Length));
        }
        var documentInput = new PdfDocumentInfo(snapshot[0].SourcePath, selectedPages.Length, selectedPages);
        var target = OutputNamingPolicy.Choose(outputDirectory, snapshot[0].SourcePath, combined: true, requestedStem: outputName);
        var temporary = target + ".partial-" + Guid.NewGuid().ToString("N");
        try
        {
            progress?.Report(new(ConversionJobStatus.Writing, "HWPX 작성", 0, 1, 35));
            var writeWatch = Stopwatch.StartNew(); await writer.WriteAsync(documentInput, temporary, cancellationToken).ConfigureAwait(false); writeDuration += writeWatch.Elapsed;
            progress?.Report(new(ConversionJobStatus.Validating, "패키지 검증", 0, 1, 75));
            var validateWatch = Stopwatch.StartNew();
            var validation = inspector.Inspect(temporary);
            validateDuration += validateWatch.Elapsed;
            if (validation.Status == ValidationStatus.Fail)
                throw new InvalidDataException(string.Join("; ", validation.Issues.Select(i => i.Message)));
            var publishWatch = Stopwatch.StartNew(); File.Move(temporary, target, false); publishDuration += publishWatch.Elapsed;
            outputs.Add(target);
            foreach (var page in snapshot)
            {
                pageReports.Add(new(page.StablePageId, page.DisplayName, page.SourcePageIndex, page.LogicalOutputIndex, null, TimeSpan.Zero, false));
            }
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
        progress?.Report(new(ConversionJobStatus.Completed, "완료", 1, 1, 100));
        var outputPath = outputs.Count == 1 ? outputs[0] : outputDirectory;
        var validationReport = outputs.Select(inspector.Inspect).Aggregate(
            new ValidationReport(ValidationStatus.Pass, []),
            (current, next) => new(
                current.Status == ValidationStatus.Fail || next.Status == ValidationStatus.Fail ? ValidationStatus.Fail :
                    current.Status == ValidationStatus.Warning || next.Status == ValidationStatus.Warning ? ValidationStatus.Warning : ValidationStatus.Pass,
                current.Issues.Concat(next.Issues).ToArray()));
        totalWatch.Stop();
        return new ConversionJobResult(new(
            "0.1.0", jobId, started, DateTimeOffset.UtcNow, sourcePaths.Length, snapshot.Length,
            outputPath, outputs.Sum(path => new FileInfo(path).Length), quality, validationReport, warnings, pageReports,
            new ConversionStageDurations(analyzeDuration, TimeSpan.Zero, writeDuration, validateDuration, publishDuration, totalWatch.Elapsed)), outputs);
    }
}

public static class OutputPathValidator
{
    public static void ValidateDirectory(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("출력 폴더를 지정하세요.", nameof(directory));
        if (!Directory.Exists(directory)) throw new DirectoryNotFoundException(directory);
        var probe = Path.Combine(directory, ".pdf2hwp-write-test-" + Guid.NewGuid().ToString("N"));
        try { using (File.Create(probe)) { } }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { throw new IOException("출력 폴더에 쓸 수 없습니다.", ex); }
        finally { try { if (File.Exists(probe)) File.Delete(probe); } catch { } }
    }
    public static string ValidateFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException("파일명이 올바르지 않습니다.", nameof(fileName));
        return fileName;
    }
}
