namespace Pdf2Hwp.Core;

public sealed record WorkspaceState(IReadOnlyList<SourceDocument> Sources, IReadOnlyList<DocumentPage> Pages)
{
    public WorkspaceState DeepCopy() => new(Sources.ToArray(), Pages.Select(p => p with { OperationHistory = p.OperationHistory.ToArray() }).ToArray());
}

public sealed class WorkspaceHistory(DocumentWorkspace workspace, int maxDepth = 100)
{
    private readonly Stack<WorkspaceState> _undo = new();
    private readonly Stack<WorkspaceState> _redo = new();
    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;
    public int Depth => _undo.Count;
    public void Reset() { _undo.Clear(); _redo.Clear(); }
    public void Execute(string name, Action<DocumentWorkspace> operation)
    {
        var before = workspace.CaptureState();
        operation(workspace);
        _undo.Push(before);
        while (_undo.Count > maxDepth) _undo.TrimBottom();
        _redo.Clear();
    }
    public void Undo()
    {
        if (!CanUndo) return;
        _redo.Push(workspace.CaptureState()); workspace.RestoreState(_undo.Pop());
    }
    public void Redo()
    {
        if (!CanRedo) return;
        _undo.Push(workspace.CaptureState()); workspace.RestoreState(_redo.Pop());
    }
}

internal static class StackExtensions
{
    public static void TrimBottom<T>(this Stack<T> stack)
    {
        var items = stack.ToArray(); stack.Clear();
        for (var i = items.Length - 2; i >= 0; i--) stack.Push(items[i]);
    }
}
