namespace Pdf2Hwp.Core;

public sealed record SourceDocument(Guid Id, string SourcePath, int PageCount)
{
    public static SourceDocument Create(string sourcePath, int pageCount) => new(Guid.NewGuid(), Path.GetFullPath(sourcePath), pageCount);
}

public sealed record CropRegion(double Left, double Bottom, double Width, double Height);
public sealed record DocumentPage(Guid Id, Guid SourceDocumentId, string SourcePath, int SourcePageIndex, int LogicalOutputIndex, int RotationDegrees, CropRegion? Crop, IReadOnlyList<string> OperationHistory)
{
    public Guid StablePageId => Id;
    public bool IncludeInOutput { get; init; } = true;
    public string DisplayName => $"{Path.GetFileName(SourcePath)} p.{SourcePageIndex + 1}";
    public static DocumentPage FromSource(SourceDocument source, int sourcePageIndex) => new(Guid.NewGuid(), source.Id, source.SourcePath, sourcePageIndex, sourcePageIndex, 0, null, ["Source"]);
}

public sealed class DocumentWorkspace
{
    private readonly List<SourceDocument> _sources = [];
    private readonly List<DocumentPage> _pages = [];
    public IReadOnlyList<SourceDocument> Sources => _sources;
    public IReadOnlyList<DocumentPage> Pages => _pages;
    public IReadOnlyList<DocumentPage> SelectedPages => _pages.Where(p => p.IncludeInOutput).ToArray();
    public WorkspaceState CaptureState() => new(_sources.ToArray(), _pages.Select(p => p with { OperationHistory = p.OperationHistory.ToArray() }).ToArray());
    public void RestoreState(WorkspaceState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _sources.Clear(); _sources.AddRange(state.Sources);
        _pages.Clear(); _pages.AddRange(state.Pages.Select(p => p with { OperationHistory = p.OperationHistory.ToArray() }));
        Validate(); Reindex();
    }
    public void Reset() { _pages.Clear(); _sources.Clear(); }
    public void AddSource(SourceDocument source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (_sources.Any(s => s.Id == source.Id)) throw new InvalidOperationException("Duplicate source document ID.");
        _sources.Add(source);
        _pages.AddRange(Enumerable.Range(0, source.PageCount).Select(index => DocumentPage.FromSource(source, index)));
        Reindex();
    }
    public void AddSource(SourceDocument source, IReadOnlyList<PdfPageInfo> pageInfo)
    {
        ArgumentNullException.ThrowIfNull(pageInfo);
        if (pageInfo.Count != source.PageCount) throw new ArgumentException("Page metadata count does not match source page count.", nameof(pageInfo));
        ArgumentNullException.ThrowIfNull(source);
        if (_sources.Any(s => s.Id == source.Id)) throw new InvalidOperationException("Duplicate source document ID.");
        _sources.Add(source);
        _pages.AddRange(pageInfo.Select((info, index) => new DocumentPage(Guid.NewGuid(), source.Id, source.SourcePath, index, index, info.Rotation, info.CropBox is null ? null : new CropRegion(info.CropBox.Left, info.CropBox.Bottom, info.CropBox.Width, info.CropBox.Height), ["Source"])));
        Reindex();
    }
    public void Apply(PageOperation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        operation.Apply(_pages);
        Validate(); Reindex();
    }
    public void MovePage(Guid pageId, int targetIndex) => Apply(new MovePage(pageId, targetIndex));
    public void MoveBefore(Guid pageId, Guid beforePageId) { var target = _pages.FindIndex(p => p.Id == beforePageId); if (target < 0) throw new InvalidOperationException("Target page was not found."); MovePage(pageId, target); }
    public void MoveAfter(Guid pageId, Guid afterPageId) { var target = _pages.FindIndex(p => p.Id == afterPageId); if (target < 0) throw new InvalidOperationException("Target page was not found."); MovePage(pageId, target + 1); }
    public void RemovePage(Guid pageId) => Apply(new RemovePage(pageId));
    public void RemovePages(IEnumerable<Guid> pageIds) { foreach (var id in pageIds.Distinct().ToArray()) RemovePage(id); }
    public DocumentPage DuplicatePage(Guid pageId) { var index = _pages.FindIndex(p => p.Id == pageId); if (index < 0) throw new InvalidOperationException("Page was not found."); Apply(new DuplicatePage(pageId, index + 1)); return _pages[index + 1]; }
    public void SetIncluded(Guid pageId, bool included) { var i = _pages.FindIndex(p => p.Id == pageId); if (i < 0) throw new InvalidOperationException("Page was not found."); _pages[i] = _pages[i] with { IncludeInOutput = included }; }
    public IReadOnlyList<DocumentPage> CreateOutputSnapshot() { var selected = SelectedPages; if (selected.Count == 0) throw new InvalidOperationException("No pages selected for output."); return selected.Select((p, i) => p with { LogicalOutputIndex = i }).ToArray(); }
    public void Validate()
    {
        if (_pages.Select(page => page.Id).Distinct().Count() != _pages.Count) throw new InvalidOperationException("Duplicate stable page ID.");
        if (_pages.Any(page => !_sources.Any(source => source.Id == page.SourceDocumentId))) throw new InvalidOperationException("Page references an unknown source.");
        if (_pages.Any(page => page.SourcePageIndex < 0 || page.SourcePageIndex >= _sources.Single(source => source.Id == page.SourceDocumentId).PageCount)) throw new InvalidOperationException("Page source index is out of range.");
    }
    private void Reindex() { for (var index = 0; index < _pages.Count; index++) _pages[index] = _pages[index] with { LogicalOutputIndex = index }; }
}

public abstract record PageOperation
{
    internal abstract void Apply(List<DocumentPage> pages);
    protected static int IndexOf(List<DocumentPage> pages, Guid id) => pages.FindIndex(page => page.Id == id) is var index && index >= 0 ? index : throw new InvalidOperationException("Page was not found.");
    protected static DocumentPage WithHistory(DocumentPage page, string operation) => page with { OperationHistory = [.. page.OperationHistory, operation] };
}
public sealed record MovePage(Guid PageId, int TargetIndex) : PageOperation
{ internal override void Apply(List<DocumentPage> pages) { var index = IndexOf(pages, PageId); var page = pages[index]; pages.RemoveAt(index); pages.Insert(Math.Clamp(TargetIndex, 0, pages.Count), WithHistory(page, "Move")); } }
public sealed record InsertPage(DocumentPage Page, int TargetIndex) : PageOperation
{ internal override void Apply(List<DocumentPage> pages) { if (pages.Any(p => p.Id == Page.Id)) throw new InvalidOperationException("Cannot insert duplicate page ID."); pages.Insert(Math.Clamp(TargetIndex, 0, pages.Count), WithHistory(Page, "Insert")); } }
public sealed record RemovePage(Guid PageId) : PageOperation
{ internal override void Apply(List<DocumentPage> pages) => pages.RemoveAt(IndexOf(pages, PageId)); }
public sealed record DuplicatePage(Guid PageId, int TargetIndex) : PageOperation
{ internal override void Apply(List<DocumentPage> pages) { var original = pages[IndexOf(pages, PageId)]; pages.Insert(Math.Clamp(TargetIndex, 0, pages.Count), WithHistory(original with { Id = Guid.NewGuid() }, "Duplicate")); } }
public sealed record RotatePage(Guid PageId, int Degrees) : PageOperation
{ internal override void Apply(List<DocumentPage> pages) { var index = IndexOf(pages, PageId); var page = pages[index]; pages[index] = WithHistory(page with { RotationDegrees = ((page.RotationDegrees + Degrees) % 360 + 360) % 360 }, "Rotate"); } }
