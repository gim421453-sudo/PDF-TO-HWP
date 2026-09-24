namespace Pdf2Hwp.Core;

public enum ConversionMode { SafeHybrid, VisualFidelity, Editable }
public enum OutputFormat { Hwpx, Hwp }
public enum ValidationStatus { Pass, Warning, Fail }

public sealed record PdfRect(double Left, double Bottom, double Width, double Height);
public sealed record PdfGlyph(string Text, string FontName, double FontSize, PdfRect Bounds);
public sealed record PdfPageInfo(int Number, double WidthPoints, double HeightPoints, int Rotation, PdfRect? CropBox, bool HasText, bool RequiresOcr, IReadOnlyList<PdfGlyph> Glyphs);
public sealed record PdfDocumentInfo(string SourcePath, int PageCount, IReadOnlyList<PdfPageInfo> Pages);
public sealed record RenderRequest(string SourcePath, int PageNumber, string OutputPngPath, int Dpi, int? Width = null, int? Height = null, int RotationDegrees = 0);
public sealed record ValidationIssue(ValidationStatus Status, int? PageNumber, string Code, string Message);
public sealed record ValidationReport(ValidationStatus Status, IReadOnlyList<ValidationIssue> Issues);

public interface IPdfAnalyzer { Task<PdfDocumentInfo> AnalyzeAsync(string sourcePath, CancellationToken cancellationToken); }
public interface IPdfPageRenderer { Task RenderAsync(RenderRequest request, CancellationToken cancellationToken); }
public interface IHwpxWriter { Task WriteAsync(PdfDocumentInfo document, string outputPath, CancellationToken cancellationToken); }
public interface IHwpxPackageInspector { ValidationReport Inspect(string hwpxPath); }
public interface IHwpAdapter { Task ExportAsync(string hwpxPath, string hwpPath, CancellationToken cancellationToken); }
