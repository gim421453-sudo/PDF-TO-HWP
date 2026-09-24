namespace Pdf2Hwp.Core;

public interface IConversionWorkspace : IDisposable
{
    string DirectoryPath { get; }
    string CreateFilePath(string extension);
}

public interface IConversionWorkspaceFactory
{
    IConversionWorkspace Create();
    int CleanupStale(TimeSpan olderThan);
}
