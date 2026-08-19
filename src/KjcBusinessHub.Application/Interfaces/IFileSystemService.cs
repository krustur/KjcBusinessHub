namespace KjcBusinessHub.Application.Interfaces;

public interface IFileSystemService
{
    /// <summary>Combines the base folder with a (possibly forward-slash) sub-path, normalising separators for the current OS.</summary>
    string GetFullPath(string baseFolder, string fileSubPath);

    /// <summary>Opens a file using the default OS application.</summary>
    void OpenFile(string fullPath);

    /// <summary>Opens the file explorer and highlights the specified file, or opens the directory when a directory path is provided.</summary>
    void ShowInExplorer(string fullPath);
}
