using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using Pdf2Hwp.Core;
using Pdf2Hwp.Infrastructure;

namespace Pdf2Hwp.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();
    private CancellationTokenSource? _cancellation;
    private Point _dragStart;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        _viewModel.LoadSettings();
    }

    private void SelectFiles(object sender, RoutedEventArgs e) { var dialog = new OpenFileDialog { Filter = "PDF files|*.pdf", Multiselect = true }; if (dialog.ShowDialog() is true) _ = AddFilesAsync(dialog.FileNames); }
    private void OnDrop(object sender, DragEventArgs e) { if (e.Data.GetData(DataFormats.FileDrop) is string[] paths) _ = AddFilesAsync(paths.Where(p => string.Equals(Path.GetExtension(p), ".pdf", StringComparison.OrdinalIgnoreCase))); }
    private async Task AddFilesAsync(IEnumerable<string> paths)
    {
        try { await _viewModel.AddFilesAsync(paths); }
        catch (Exception ex) { _viewModel.Status = "PDF 추가 실패: " + ex.Message; }
    }
    private void SelectOutput(object sender, RoutedEventArgs e) { var dialog = new OpenFolderDialog { Title = "출력 폴더 선택" }; if (dialog.ShowDialog() is true) _viewModel.OutputDirectory = dialog.FolderName; }
    private IReadOnlyList<PageViewModel> Selected => PageList.SelectedItems.Cast<PageViewModel>().ToArray();
    private void RemovePages(object sender, RoutedEventArgs e) { _viewModel.Remove(Selected); PageList.SelectedItems.Clear(); }
    private void DuplicatePage(object sender, RoutedEventArgs e) { _viewModel.Duplicate(Selected.FirstOrDefault()); }
    private void ToggleIncluded(object sender, RoutedEventArgs e) { _viewModel.ToggleIncluded(Selected); }
    private void SelectSourcePages(object sender, RoutedEventArgs e) { var source = Selected.FirstOrDefault()?.SourcePath; if (source is null) return; PageList.SelectedItems.Clear(); foreach (var page in _viewModel.Pages.Where(p => p.SourcePath == source)) PageList.SelectedItems.Add(page); }
    private void RetryPreview(object sender, RoutedEventArgs e) { var target = Selected.FirstOrDefault(); if (target is not null) _ = _viewModel.LoadPreviewsAsync([target], true); }
    private void ClearPages(object sender, RoutedEventArgs e) { _viewModel.ClearPages(); }
    private async void Convert(object sender, RoutedEventArgs e)
    {
        if (_cancellation is not null) return;
        try
        {
            _viewModel.ValidateOutput();
            _cancellation = new CancellationTokenSource();
            var progress = new Progress<ConversionProgress>(p => { _viewModel.Progress = p.Percentage; _viewModel.Status = $"{p.Stage} ({p.Current}/{p.Total})"; });
            var service = new ConversionJobService(new PdfPigAnalyzer(), new HwpxWriter(), new HwpxPackageInspector());
            var jobSnapshot = _viewModel.Workspace.CreateOutputSnapshot();
            _viewModel.BeginJob(jobSnapshot);
            var result = await service.RunAsync(jobSnapshot, _viewModel.OutputDirectory, _viewModel.RenderQuality, progress, _cancellation.Token, _viewModel.OutputName);
            _viewModel.LastReport = result.Report;
            _viewModel.RecordHistory(result.Report, result.Report.Validation.Status == ValidationStatus.Fail ? ConversionJobStatus.Failed : ConversionJobStatus.Completed);
            _viewModel.ReportSummary = $"완료: {result.OutputFiles.Count}개 · 검증 {result.Report.Validation.Status}";
            _viewModel.Status = result.Report.Warnings.Count == 0 ? "완료: 패키지 검증 PASS" : "완료: 경고가 있는 결과입니다.";
            _viewModel.Progress = 100;
        }
        catch (OperationCanceledException) { _viewModel.RecordOutcome(ConversionJobStatus.Cancelled); _viewModel.Status = "취소됨"; }
        catch (Exception ex) { _viewModel.RecordOutcome(ConversionJobStatus.Failed); _viewModel.Status = "실패: " + ConversionErrorMapper.ToUserMessage(ex); }
        finally { _cancellation?.Dispose(); _cancellation = null; }
    }
    private void Cancel(object sender, RoutedEventArgs e) { _cancellation?.Cancel(); }
    private void OpenResults(object sender, RoutedEventArgs e) { if (Directory.Exists(_viewModel.OutputDirectory)) Process.Start(new ProcessStartInfo("explorer.exe", _viewModel.OutputDirectory) { UseShellExecute = true }); }
    private void ExportReport(object sender, RoutedEventArgs e) { _viewModel.ExportReport(); }
    private ConversionHistoryEntry? SelectedHistory => _viewModel.SelectedHistory;
    private void HistoryOpenFolder(object sender, RoutedEventArgs e) => _viewModel.OpenHistoryFolder();
    private void HistoryOpenOutput(object sender, RoutedEventArgs e) => _viewModel.OpenHistoryOutput();
    private void HistoryOpenReport(object sender, RoutedEventArgs e) => _viewModel.OpenHistoryReport();
    private void HistoryRemove(object sender, RoutedEventArgs e) => _viewModel.RemoveHistory();
    private void HistoryClear(object sender, RoutedEventArgs e) => _viewModel.ClearHistory();
    private async void HistoryRetry(object sender, RoutedEventArgs e) { if (await _viewModel.PrepareRetryAsync()) Convert(sender, new RoutedEventArgs()); }
    private void ClearPreviewCache(object sender, RoutedEventArgs e) { _viewModel.ClearPreviewCache(); }
    private void OpenDiagnostics(object sender, RoutedEventArgs e) { _viewModel.ExportDiagnostics(); }
    private void Undo(object sender, RoutedEventArgs e) { _viewModel.Undo(); PageList.SelectedItems.Clear(); }
    private void Redo(object sender, RoutedEventArgs e) { _viewModel.Redo(); PageList.SelectedItems.Clear(); }
    private void OpenSettings(object sender, RoutedEventArgs e) { _viewModel.Status = "설정: 마지막 폴더와 렌더 품질은 자동 저장됩니다."; }
    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.O) { SelectFiles(sender, new RoutedEventArgs()); e.Handled = true; }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Z) { Undo(sender, new RoutedEventArgs()); e.Handled = true; }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Y) { Redo(sender, new RoutedEventArgs()); e.Handled = true; }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.A) { PageList.SelectAll(); e.Handled = true; }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.D) { DuplicatePage(sender, new RoutedEventArgs()); e.Handled = true; }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Enter) { Convert(sender, new RoutedEventArgs()); e.Handled = true; }
        else if (e.Key == Key.Delete) { RemovePages(sender, new RoutedEventArgs()); e.Handled = true; }
        else if (e.Key == Key.Escape) { Cancel(sender, new RoutedEventArgs()); e.Handled = true; }
    }
    private void OnPageMouseDown(object sender, MouseButtonEventArgs e) => _dragStart = e.GetPosition(PageList);
    private void OnPageSelectionChanged(object sender, SelectionChangedEventArgs e) => _ = _viewModel.LoadPreviewsAsync(PageList.SelectedItems.Cast<PageViewModel>());
    private void OnPageListLoaded(object sender, RoutedEventArgs e) => QueueViewportPreviews();
    private void OnPageScrollChanged(object sender, ScrollChangedEventArgs e) { if (e.VerticalChange != 0 || e.ViewportHeightChange != 0) QueueViewportPreviews(); }
    private void QueueViewportPreviews()
    {
        var visible = PageList.Items.Cast<PageViewModel>().Where(page =>
        {
            var container = PageList.ItemContainerGenerator.ContainerFromItem(page) as FrameworkElement;
            if (container is null || !container.IsVisible) return false;
            var bounds = container.TransformToAncestor(PageList).TransformBounds(new Rect(0, 0, container.ActualWidth, container.ActualHeight));
            return bounds.Bottom >= 0 && bounds.Top <= PageList.ActualHeight;
        }).Take(12).ToArray();
        _ = _viewModel.LoadPreviewsAsync(visible);
    }
    private void OnPageMouseMove(object sender, MouseEventArgs e) { if (e.LeftButton != MouseButtonState.Pressed) return; var delta = e.GetPosition(PageList) - _dragStart; if (Math.Abs(delta.X) + Math.Abs(delta.Y) < 8) return; if (PageList.SelectedItem is PageViewModel page) DragDrop.DoDragDrop(PageList, page, DragDropEffects.Move); }
    private void OnPageDragOver(object sender, DragEventArgs e) { e.Effects = e.Data.GetDataPresent(typeof(PageViewModel)) ? DragDropEffects.Move : DragDropEffects.None; e.Handled = true; }
    private void OnPageDrop(object sender, DragEventArgs e) { if (e.Data.GetData(typeof(PageViewModel)) is not PageViewModel source || (e.OriginalSource as DependencyObject) is not DependencyObject target) return; if ((target as FrameworkElement)?.DataContext is PageViewModel destination && source != destination) _viewModel.MoveBefore(source, destination); }
    private void OnClosing(object? sender, CancelEventArgs e) { if (_cancellation is not null) { _cancellation.Cancel(); _viewModel.Status = "변환 취소 중입니다. 완료 후 다시 닫아 주세요."; e.Cancel = true; return; } _viewModel.CancelAllPreviews(); _viewModel.SaveSettings(); }
}

public sealed class PageViewModel(DocumentPage page) : INotifyPropertyChanged
{
    public DocumentPage Page { get; private set; } = page;
    public Guid Id => Page.Id; public string SourcePath => Page.SourcePath; public string DisplayName => Page.DisplayName; public int LogicalOutputIndex => Page.LogicalOutputIndex;
    private string? _thumbnailPath;
    public string LogicalLabel => $"페이지 {Page.LogicalOutputIndex + 1}"; public bool Included => Page.IncludeInOutput; public string IncludeLabel => Included ? "출력 포함" : "출력 제외";
    public string OrientationLabel => Page.RotationDegrees == 0 ? "기본 방향" : $"회전 {Page.RotationDegrees}°";
    public string? ThumbnailPath { get => _thumbnailPath; private set { _thumbnailPath = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ThumbnailPath))); } }
    public string PreviewState { get; private set; } = "미리보기 생성 전";
    public void SetPreview(string? path, string state) { ThumbnailPath = path; PreviewState = state; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PreviewState))); }
    public void SetPreviewPath(string path) => SetPreview(path, "");
    public void Update(DocumentPage page) { Page = page; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null)); }
    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly JsonSettingsStore _settings = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PDF2HWP", "settings.json"));
    private readonly RecentFilesStore _recent = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PDF2HWP", "recent.json"));
    private readonly ConversionHistoryStore _history = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PDF2HWP", "history.json"));
    private readonly PreviewThumbnailCache _thumbnailCache = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PDF2HWP", "Cache", "Preview"));
    private readonly CachedPreviewThumbnailRenderer _thumbnailRenderer;
    private readonly Dictionary<Guid, CancellationTokenSource> _previewRequests = [];
    private IReadOnlyList<DocumentPage> _activeSnapshot = [];
    public DocumentWorkspace Workspace { get; } = new();
    private WorkspaceHistory WorkspaceHistory { get; }
    public ObservableCollection<PageViewModel> Pages { get; } = [];
    public ObservableCollection<(string Path, DateTimeOffset OpenedAt)> RecentFiles { get; } = [];
    public IReadOnlyList<ConversionHistoryEntry> History => _history.Load();
    private ConversionHistoryEntry? _selectedHistory;
    public ConversionHistoryEntry? SelectedHistory { get => _selectedHistory; set => Set(ref _selectedHistory, value); }
    public string PreviewCacheSummary { get { var cache = _thumbnailCache.Measure(); return $"미리보기 캐시 {cache.Entries}개 · {cache.Bytes / 1024 / 1024} MB"; } }
    private string _outputDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), _outputName = "PDF2HWP", _status = "PDF를 선택하거나 이 창으로 끌어 놓으세요.", _reportSummary = "페이지를 추가하세요.";
    private double _progress; private OutputFormat _format = OutputFormat.Hwpx; private ConversionMode _mode = ConversionMode.SafeHybrid; private RenderQuality _renderQuality = RenderQuality.Standard; private bool _ocrAuto = true, _verify = true;
    public MainViewModel() { WorkspaceHistory = new WorkspaceHistory(Workspace); _thumbnailRenderer = new CachedPreviewThumbnailRenderer(new PdfiumPreviewThumbnailRenderer(new PdfiumPageRenderer()), _thumbnailCache, 2); }
    public string OutputDirectory { get => _outputDirectory; set => Set(ref _outputDirectory, value); } public string OutputName { get => _outputName; set => Set(ref _outputName, value); } public string Status { get => _status; set => Set(ref _status, value); } public string ReportSummary { get => _reportSummary; set => Set(ref _reportSummary, value); } public double Progress { get => _progress; set => Set(ref _progress, value); }
    public OutputFormat Format { get => _format; set => Set(ref _format, value); } public ConversionMode Mode { get => _mode; set => Set(ref _mode, value); } public RenderQuality RenderQuality { get => _renderQuality; set => Set(ref _renderQuality, value); } public bool OcrAuto { get => _ocrAuto; set => Set(ref _ocrAuto, value); } public bool Verify { get => _verify; set => Set(ref _verify, value); }
    public ConversionReport? LastReport { get; set; } public string PageSummary => $"총 {Pages.Count}페이지 · 출력 {Pages.Count(p => p.Included)}페이지 · PDF {Workspace.Sources.Count}개";
    public bool CanUndo => WorkspaceHistory.CanUndo; public bool CanRedo => WorkspaceHistory.CanRedo;
    public void LoadSettings() { var s = _settings.Load(); if (!string.IsNullOrWhiteSpace(s.LastOutputDirectory)) OutputDirectory = s.LastOutputDirectory; RenderQuality = s.RenderQuality; foreach (var item in _recent.Load()) RecentFiles.Add(item); try { _thumbnailCache.Cleanup(); } catch { } }
    public void SaveSettings() { _settings.Save(new(OutputDirectory, RenderQuality)); }
    public async Task AddFilesAsync(IEnumerable<string> paths)
    {
        var analyzer = new PdfPigAnalyzer();
        foreach (var path in paths.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (Workspace.Sources.Any(s => string.Equals(s.SourcePath, Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase))) continue;
            Status = $"페이지 분석 중: {Path.GetFileName(path)}";
            var document = await analyzer.AnalyzeAsync(path, CancellationToken.None);
            Workspace.AddSource(SourceDocument.Create(path, document.PageCount), document.Pages);
            foreach (var page in Workspace.Pages.Where(p => string.Equals(p.SourcePath, Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase))) Pages.Add(new(page));
            _recent.Add(path);
        }
        Status = Pages.Count == 0 ? "PDF를 선택하거나 이 창으로 끌어 놓으세요." : "페이지를 검토한 뒤 변환을 시작하세요.";
        OnPropertyChanged(nameof(PageSummary));
    }
    public async Task LoadPreviewsAsync(IEnumerable<PageViewModel> selected, bool invalidate = false)
    {
        var items = selected.Distinct().Take(12).ToArray();
        if (items.Length == 0) return;
        foreach (var item in items)
        {
            if (_previewRequests.ContainsKey(item.Id) && !invalidate) continue;
            CancelPreview(item.Id);
            var cancellation = new CancellationTokenSource();
            _previewRequests[item.Id] = cancellation;
            _ = LoadPreviewAsync(item, cancellation, invalidate);
        }
        await Task.CompletedTask;
    }
    private async Task LoadPreviewAsync(PageViewModel item, CancellationTokenSource cancellation, bool invalidate = false)
    {
        var token = cancellation.Token;
        item.SetPreview(null, "미리보기 생성 중");
        try
        {
            if (invalidate) await _thumbnailRenderer.InvalidateAsync(item.SourcePath, item.Page.SourcePageIndex, new PreviewThumbnailOptions(300), token).ConfigureAwait(true);
            var result = await _thumbnailRenderer.RenderAsync(item.SourcePath, item.Page.SourcePageIndex, new PreviewThumbnailOptions(300), token).ConfigureAwait(true);
            item.SetPreviewPath(_thumbnailCache.GetPath(result.SourceHash, item.Page.SourcePageIndex, 300));
        }
        catch (OperationCanceledException) { }
        catch { item.SetPreview(null, "미리보기 실패 · 다시 선택하면 재시도"); }
        finally
        {
            if (_previewRequests.TryGetValue(item.Id, out var current) && ReferenceEquals(current, cancellation)) _previewRequests.Remove(item.Id);
            cancellation.Dispose();
        }
    }
    private void CancelPreview(Guid id) { if (_previewRequests.Remove(id, out var cancellation)) { cancellation.Cancel(); } }
    public void CancelAllPreviews() { foreach (var id in _previewRequests.Keys.ToArray()) CancelPreview(id); }
    public void Remove(IEnumerable<PageViewModel> items) { var selected = items.ToArray(); var ids = selected.Select(i => i.Id).ToArray(); if (ids.Length == 0) return; foreach (var id in ids) CancelPreview(id); WorkspaceHistory.Execute("RemovePages", w => w.RemovePages(ids)); Refresh(); }
    public void Duplicate(PageViewModel? item) { if (item is null) return; WorkspaceHistory.Execute("DuplicatePage", w => w.DuplicatePage(item.Id)); Refresh(); }
    public void ToggleIncluded(IEnumerable<PageViewModel> items) { var ids = items.Select(i => i.Id).ToArray(); WorkspaceHistory.Execute("SetIncluded", w => foreachIncluded(w, ids)); Refresh(); static void foreachIncluded(DocumentWorkspace w, Guid[] ids) { foreach (var id in ids) { var page = w.Pages.First(p => p.Id == id); w.SetIncluded(id, !page.IncludeInOutput); } } }
    public void MoveBefore(PageViewModel source, PageViewModel destination) { WorkspaceHistory.Execute("MovePage", w => w.MoveBefore(source.Id, destination.Id)); Refresh(); }
    public void ClearPages() { var ids = Workspace.Pages.Select(p => p.Id).ToArray(); if (ids.Length == 0) return; foreach (var id in ids) CancelPreview(id); WorkspaceHistory.Execute("ClearPages", w => w.RemovePages(ids)); Refresh(); }
    public void Undo() { WorkspaceHistory.Undo(); Refresh(); NotifyHistory(); }
    public void Redo() { WorkspaceHistory.Redo(); Refresh(); NotifyHistory(); }
    private void NotifyHistory() { OnPropertyChanged(nameof(CanUndo)); OnPropertyChanged(nameof(CanRedo)); }
    private void Refresh() { Pages.Clear(); foreach (var page in Workspace.Pages) Pages.Add(new(page)); OnPropertyChanged(nameof(PageSummary)); NotifyHistory(); }
    public void ValidateOutput() { if (Format == OutputFormat.Hwp) throw new NotSupportedException("HWP 출력은 Hancom adapter 연결 후 활성화됩니다."); OutputPathValidator.ValidateDirectory(OutputDirectory); OutputPathValidator.ValidateFileName(OutputName + ".hwpx"); }
    public void ExportReport() { if (LastReport is null) { Status = "먼저 변환을 완료하세요."; return; } var path = Path.Combine(OutputDirectory, "PDF2HWP-conversion-report.json"); DiagnosticExport.Write(path, LastReport); Status = "변환 리포트를 저장했습니다: " + path; }
    public void BeginJob(IReadOnlyList<DocumentPage> snapshot) => _activeSnapshot = snapshot.ToArray();
    public void RecordHistory(ConversionReport report, ConversionJobStatus status)
    {
        var mapping = _activeSnapshot.ToDictionary(p => p.Id);
        var pages = report.Pages.Where(p => mapping.ContainsKey(p.StablePageId)).Select(p => mapping[p.StablePageId]).Select(p => new HistoryPageEntry(p.SourcePath, p.SourcePageIndex)).ToArray();
        _history.Add(new(DateTimeOffset.UtcNow, report.Pages.Select(p => p.SourceDisplayName).Distinct().ToArray(), report.OutputPath, report.SelectedPageCount, report.RenderQuality, report.CompletedAt - report.StartedAt, status, report.AppVersion, pages));
        OnPropertyChanged(nameof(History));
    }
    public void RecordOutcome(ConversionJobStatus status)
    {
        if (_activeSnapshot.Count == 0) return;
        _history.Add(new(DateTimeOffset.UtcNow, _activeSnapshot.Select(p => Path.GetFileName(p.SourcePath)).Distinct().ToArray(), Path.Combine(OutputDirectory, "PDF2HWP_combined.hwpx"), _activeSnapshot.Count, RenderQuality, TimeSpan.Zero, status, "0.1.0", _activeSnapshot.Select(p => new HistoryPageEntry(p.SourcePath, p.SourcePageIndex)).ToArray()));
        OnPropertyChanged(nameof(History));
    }
    public void ExportDiagnostics() { var path = Path.Combine(OutputDirectory, "PDF2HWP-diagnostics.json"); var cache = _thumbnailCache.Statistics; var size = _thumbnailCache.Measure(); DiagnosticExport.Write(path, new { app = "PDF2HWP", pages = Pages.Count, included = Pages.Count(p => p.Included), status = Status, renderQuality = RenderQuality.ToString(), renderer = "PDFium preview", cacheRoot = _thumbnailCache.Root, cacheHits = cache.Hits, cacheMisses = cache.Misses, cacheEntries = size.Entries, cacheBytes = size.Bytes, historyCount = History.Count, timestamp = DateTimeOffset.UtcNow }); Status = "진단 파일을 저장했습니다: " + path; }
    public void OpenHistoryFolder() => OpenHistoryPath(SelectedHistory is { } item && Directory.Exists(Path.GetDirectoryName(item.OutputPath)) ? Path.GetDirectoryName(item.OutputPath)! : null);
    public void OpenHistoryOutput() => OpenHistoryPath(SelectedHistory is { } item && File.Exists(item.OutputPath) ? item.OutputPath : null);
    public void OpenHistoryReport()
    {
        var path = SelectedHistory is { } item ? Path.Combine(Path.GetDirectoryName(item.OutputPath) ?? "", "PDF2HWP-conversion-report.json") : null;
        OpenHistoryPath(path is not null && File.Exists(path) ? path : null);
    }
    private void OpenHistoryPath(string? path)
    {
        if (path is null) { Status = "파일을 찾을 수 없습니다."; return; }
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); } catch (Exception ex) { Status = ConversionErrorMapper.ToUserMessage(ex); }
    }
    public void RemoveHistory() { if (SelectedHistory is null) return; var index = History.ToList().IndexOf(SelectedHistory); _history.RemoveAt(index); SelectedHistory = null; OnPropertyChanged(nameof(History)); }
    public void ClearHistory() { _history.Clear(); SelectedHistory = null; OnPropertyChanged(nameof(History)); Status = "변환 기록을 지웠습니다. 출력 파일은 그대로 유지됩니다."; }
    public void ClearPreviewCache() { try { _thumbnailCache.Clear(); _thumbnailCache.Cleanup(); OnPropertyChanged(nameof(PreviewCacheSummary)); Status = "미리보기 캐시를 지웠습니다."; } catch (Exception ex) { Status = "캐시를 지울 수 없습니다: " + ex.Message; } }
    public async Task<bool> PrepareRetryAsync()
    {
        var item = SelectedHistory;
        if (item?.CanRetry != true) { Status = "원본 PDF를 찾을 수 없어 다시 시도할 수 없습니다."; return false; }
        try
        {
            var pageReferences = item.Pages ?? Array.Empty<HistoryPageEntry>();
            if (pageReferences.Count == 0) { Status = "이 기록에는 재시도에 필요한 페이지 정보가 없습니다."; return false; }
            var paths = pageReferences.Select(p => Path.GetFullPath(p.SourcePath)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var analyzer = new PdfPigAnalyzer(); var metadata = new Dictionary<string, PdfDocumentInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in paths) metadata[path] = await analyzer.AnalyzeAsync(path, CancellationToken.None);
            Workspace.Reset(); WorkspaceHistory.Reset(); Pages.Clear();
            var sources = new Dictionary<string, SourceDocument>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in paths) { var source = SourceDocument.Create(path, metadata[path].PageCount); Workspace.AddSource(source, metadata[path].Pages); sources[path] = source; }
            var existing = Workspace.Pages.Select(p => p.Id).ToArray(); if (existing.Length > 0) Workspace.RemovePages(existing);
            foreach (var page in pageReferences)
            {
                var path = Path.GetFullPath(page.SourcePath); var source = sources[path];
                if (page.SourcePageIndex < 0 || page.SourcePageIndex >= source.PageCount) throw new InvalidDataException("이력의 페이지 정보가 원본 PDF와 맞지 않습니다.");
                Workspace.Apply(new InsertPage(DocumentPage.FromSource(source, page.SourcePageIndex), Workspace.Pages.Count));
            }
            Refresh(); OutputDirectory = Path.GetDirectoryName(item.OutputPath) ?? OutputDirectory;
            Status = "기록을 복원했습니다. 새 변환 작업으로 재시도합니다."; return true;
        }
        catch (Exception ex) { Status = "재시도 준비 실패: " + ConversionErrorMapper.ToUserMessage(ex); return false; }
    }
    public event PropertyChangedEventHandler? PropertyChanged; private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new(name)); private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null) { if (EqualityComparer<T>.Default.Equals(field, value)) return; field = value; OnPropertyChanged(name!); }
}
