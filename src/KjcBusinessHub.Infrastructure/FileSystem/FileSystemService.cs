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
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start("explorer.exe", $"/select,\"{fullPath}\"");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", $"-R \"{fullPath}\"");
        }
        else
        {
            // Linux: open the containing directory
            var directory = Path.GetDirectoryName(fullPath) ?? fullPath;
            Process.Start(new ProcessStartInfo
            {
                FileName = "xdg-open",
                UseShellExecute = false,
                ArgumentList = { directory }
            });
        }
    }
}
