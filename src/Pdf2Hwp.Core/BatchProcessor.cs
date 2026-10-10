namespace Pdf2Hwp.Core;

public sealed record BatchItemFailure<T>(T Item, Exception Error);

public static class BatchProcessor
{
    public static async Task<IReadOnlyList<BatchItemFailure<T>>> ContinueOnFailureAsync<T>(IEnumerable<T> items, Func<T, Task> process)
    {
        var failures = new List<BatchItemFailure<T>>();
        foreach (var item in items)
        {
            try { await process(item); }
            catch (OperationCanceledException) { throw; }
            catch (Exception error) { failures.Add(new(item, error)); }
        }
        return failures;
    }
}
