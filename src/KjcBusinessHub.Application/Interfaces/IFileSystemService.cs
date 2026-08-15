namespace KjcBusinessHub.Application.Interfaces;

public interface IFileSystemService
{
    /// <summary>Opens a file using the default OS application.</summary>
    void OpenFile(string fullPath);

    /// <summary>Opens the file explorer and highlights the specified file.</summary>
    void ShowInExplorer(string fullPath);
}
