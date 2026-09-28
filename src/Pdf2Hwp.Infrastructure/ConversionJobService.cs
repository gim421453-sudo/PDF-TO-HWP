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
    IHwpxPackageInspector inspector,
    IPdfPageRenderer? pageRenderer = null,
    HwpxImagePackageWriter? imageWriter = null) : IConversionJobRunner
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
        var totalWatch = Stopwatch.StartNew(); var analyzeDuration = TimeSpan.Zero; var renderDuration = TimeSpan.Zero; var writeDuration = TimeSpan.Zero; var validateDuration = TimeSpan.Zero; var publishDuration = TimeSpan.Zero;
        var outputs = new List<string>();
        var validations = new List<ValidationReport>();
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
            var analyzedPage = source.Pages[page.SourcePageIndex];
            selectedPages[outputIndex] = pageRenderer is null
                ? analyzedPage with { Number = outputIndex + 1 }
                : VisualFidelityConverter.PrepareRasterOutputPage(page, analyzedPage);
            progress?.Report(new(ConversionJobStatus.Writing, "선택된 페이지 구성", outputIndex + 1, snapshot.Length, 30 + 5d * (outputIndex + 1) / snapshot.Length));
        }
        var documentInput = new PdfDocumentInfo(snapshot[0].SourcePath, selectedPages.Length, selectedPages);
        var target = OutputNamingPolicy.Choose(outputDirectory, snapshot[0].SourcePath, combined: true, requestedStem: outputName);
        var temporary = target + ".partial-" + Guid.NewGuid().ToString("N");
        var renderDirectory = Path.Combine(Path.GetTempPath(), "PDF2HWP", jobId.ToString("N"));
        try
        {
            if (pageRenderer is null)
            {
                progress?.Report(new(ConversionJobStatus.Writing, "HWPX 작성", 0, 1, 35));
                var writeWatch = Stopwatch.StartNew(); await writer.WriteAsync(documentInput, temporary, cancellationToken).ConfigureAwait(false); writeDuration += writeWatch.Elapsed;
                foreach (var page in snapshot) pageReports.Add(new(page.StablePageId, page.DisplayName, page.SourcePageIndex, page.LogicalOutputIndex, null, TimeSpan.Zero, false));
            }
            else
            {
                var converter = new VisualFidelityConverter(analyzer, pageRenderer, imageWriter ?? new HwpxImagePackageWriter(), inspector);
                var visualResult = await converter.ConvertPagesAsync(documentInput, snapshot, temporary, renderDirectory, quality, progress, cancellationToken).ConfigureAwait(false);
                renderDuration += visualResult.RenderDuration;
                writeDuration += visualResult.WriteDuration;
                for (var index = 0; index < snapshot.Length; index++)
                {
                    var rendered = visualResult.Pages[index]; var page = snapshot[index];
                    pageReports.Add(new(page.StablePageId, page.DisplayName, page.SourcePageIndex, page.LogicalOutputIndex, rendered.RenderHash, rendered.RenderDuration, rendered.WasReused));
                }
            }
            progress?.Report(new(ConversionJobStatus.Validating, "패키지 검증", 0, 1, 75));
            var validateWatch = Stopwatch.StartNew();
            var validation = inspector.Inspect(temporary);
            validateDuration += validateWatch.Elapsed;
            cancellationToken.ThrowIfCancellationRequested();
            if (validation.Status is not (ValidationStatus.Pass or ValidationStatus.Warning))
                throw new InvalidDataException(string.Join("; ", validation.Issues.Select(i => i.Message)));
            validations.Add(validation);
            cancellationToken.ThrowIfCancellationRequested();
            var reopenWatch = Stopwatch.StartNew();
            var reopened = inspector.Inspect(temporary);
            validateDuration += reopenWatch.Elapsed;
            cancellationToken.ThrowIfCancellationRequested();
            if (reopened.Status is not (ValidationStatus.Pass or ValidationStatus.Warning)) throw new InvalidDataException("Reopen validation failed: " + string.Join("; ", reopened.Issues.Select(i => i.Message)));
            cancellationToken.ThrowIfCancellationRequested();
            var publishWatch = Stopwatch.StartNew(); File.Move(temporary, target, false); publishDuration += publishWatch.Elapsed;
            outputs.Add(target);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); if (Directory.Exists(renderDirectory)) try { Directory.Delete(renderDirectory, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
        var outputPath = outputs.Count == 1 ? outputs[0] : outputDirectory;
        var validationReport = validations.Aggregate(
            new ValidationReport(ValidationStatus.Pass, []),
            (current, next) => new(
                current.Status == ValidationStatus.Fail || next.Status == ValidationStatus.Fail ? ValidationStatus.Fail :
                    current.Status == ValidationStatus.Warning || next.Status == ValidationStatus.Warning ? ValidationStatus.Warning : ValidationStatus.Pass,
                current.Issues.Concat(next.Issues).ToArray()));
        warnings.AddRange(validationReport.Issues.Where(issue => issue.Status == ValidationStatus.Warning).Select(issue => issue.Message));
        var hasValidationWarnings = validationReport.Status == ValidationStatus.Warning;
        progress?.Report(new(hasValidationWarnings ? ConversionJobStatus.CompletedWithWarnings : ConversionJobStatus.Completed, hasValidationWarnings ? "경고" : "완료", 1, 1, 100));
        totalWatch.Stop();
        return new ConversionJobResult(new(
            "0.1.0", jobId, started, DateTimeOffset.UtcNow, sourcePaths.Length, snapshot.Length,
            outputPath, outputs.Sum(path => new FileInfo(path).Length), quality, validationReport, warnings, pageReports,
            new ConversionStageDurations(analyzeDuration, renderDuration, writeDuration, validateDuration, publishDuration, totalWatch.Elapsed)), outputs);
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
