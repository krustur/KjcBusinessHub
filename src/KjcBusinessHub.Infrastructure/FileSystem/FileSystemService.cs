using System.Diagnostics;
using System.Runtime.InteropServices;
using KjcBusinessHub.Application.Interfaces;

namespace KjcBusinessHub.Infrastructure.FileSystem;

public class FileSystemService : IFileSystemService
{
    public string GetFullPath(string baseFolder, string fileSubPath)
    {
        var normalizedSubPath = fileSubPath
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);
        return Path.Combine(baseFolder, normalizedSubPath);
    }

    public void OpenFile(string fullPath)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = fullPath,
            UseShellExecute = true
        });
    }

    public void ShowInExplorer(string fullPath)
    {
        if (Directory.Exists(fullPath))
        {
            OpenDirectory(fullPath);
            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start("explorer.exe", $"/select,\"{fullPath}\"");
        }
        else
        {
            var directory = Path.GetDirectoryName(fullPath) ?? fullPath;
            OpenDirectory(directory);
        }
    }

    private static void OpenDirectory(string directory)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start("explorer.exe", $"\"{directory}\"");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", directory);
        }
        else
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "xdg-open",
                UseShellExecute = false,
                ArgumentList = { directory }
            });
        }
    }
}
