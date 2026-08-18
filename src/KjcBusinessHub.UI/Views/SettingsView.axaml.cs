using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using KjcBusinessHub.UI.ViewModels;

namespace KjcBusinessHub.UI.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private async void BrowseFolder(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this)?.StorageProvider is not { } storageProvider)
            return;

        var folders = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select source document folder",
            AllowMultiple = false,
        });

        var selectedPath = folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
        if (!string.IsNullOrWhiteSpace(selectedPath) && DataContext is SettingsViewModel viewModel)
        {
            viewModel.SourceDocumentFolder = selectedPath;
        }
    }
}
