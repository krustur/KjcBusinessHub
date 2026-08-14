using System;
using Avalonia.Controls;
using KjcBusinessHub.UI.ViewModels;

namespace KjcBusinessHub.UI.Views;

public partial class AppView : UserControl
{
    public AppView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is AppViewModel vm)
        {
            _ = vm.InitialiseAsync();
        }
    }
}
