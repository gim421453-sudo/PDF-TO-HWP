using Pdf2Hwp.Core;

namespace Pdf2Hwp.Tests;

public sealed class DocumentWorkspaceTests
{
    [Fact] public void Stable_ids_survive_reorder_and_source_is_immutable()
    {
        var source = SourceDocument.Create("C:\\in\\a.pdf", 3); var workspace = new DocumentWorkspace(); workspace.AddSource(source); var first = workspace.Pages[0]; workspace.Apply(new MovePage(first.Id, 2));
        Assert.Equal(first.Id, workspace.Pages[2].Id); Assert.Equal("C:\\in\\a.pdf", source.SourcePath); Assert.Equal(new[] { 0, 1, 2 }, workspace.Pages.Select(p => p.LogicalOutputIndex));
    }
    [Fact] public void Merge_insert_duplicate_remove_and_rotate_keep_integrity()
    {
        var workspace = new DocumentWorkspace(); var first = SourceDocument.Create("a.pdf", 2); var second = SourceDocument.Create("b.pdf", 1); workspace.AddSource(first); workspace.AddSource(second);
        var page = workspace.Pages[0]; workspace.Apply(new DuplicatePage(page.Id, 1)); var duplicate = workspace.Pages[1]; workspace.Apply(new RotatePage(duplicate.Id, 90)); workspace.Apply(new RemovePage(page.Id)); workspace.Validate();
        Assert.Equal(3, workspace.Pages.Count); Assert.NotEqual(duplicate.Id, page.Id); Assert.Equal(90, workspace.Pages.Single(p => p.Id == duplicate.Id).RotationDegrees);
    }
    [Fact] public void Insert_rejects_duplicate_page_id()
    {
        var workspace = new DocumentWorkspace(); workspace.AddSource(SourceDocument.Create("a.pdf", 1));
        Assert.Throws<InvalidOperationException>(() => workspace.Apply(new InsertPage(workspace.Pages[0], 0)));
    }
    [Fact] public void Duplicate_gets_new_stable_id_but_preserves_source_reference()
    { var s = SourceDocument.Create("a.pdf", 1); var w = new DocumentWorkspace(); w.AddSource(s); var copy = w.DuplicatePage(w.Pages[0].Id); Assert.NotEqual(w.Pages[0].Id, copy.Id); Assert.Equal(s.Id, copy.SourceDocumentId); Assert.Equal(0, copy.SourcePageIndex); }
    [Fact] public void Selection_snapshot_rejects_empty_and_preserves_order()
    { var w = new DocumentWorkspace(); w.AddSource(SourceDocument.Create("a.pdf", 2)); w.SetIncluded(w.Pages[0].Id, false); Assert.Single(w.CreateOutputSnapshot()); w.SetIncluded(w.Pages[1].Id, false); Assert.Throws<InvalidOperationException>(() => w.CreateOutputSnapshot()); }
    [Fact] public void Multiple_sources_can_be_reordered_without_changing_identity()
    { var w = new DocumentWorkspace(); w.AddSource(SourceDocument.Create("a.pdf", 2)); w.AddSource(SourceDocument.Create("b.pdf", 1)); var b = w.Pages[2]; w.MoveBefore(b.Id, w.Pages[0].Id); Assert.Equal(b.Id, w.Pages[0].Id); Assert.Equal(0, w.Pages[0].SourcePageIndex); }
}
