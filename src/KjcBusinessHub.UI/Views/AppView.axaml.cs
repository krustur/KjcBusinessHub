using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Threading;
using KjcBusinessHub.UI.ViewModels;

namespace KjcBusinessHub.UI.Views;

public partial class AppView : UserControl
{
    private AppViewModel? _viewModel;
    private TransactionImportWindow? _transactionImportWindow;
    private bool _isSyncingTransactionImportWindow;

    public AppView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = DataContext as AppViewModel;
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _ = _viewModel.InitialiseAsync();
            SyncTransactionImportWindow();
        }
        else
        {
            CloseTransactionImportWindow();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppViewModel.IsTransactionImportOpen))
        {
            SyncTransactionImportWindow();
        }
        else if (e.PropertyName == nameof(AppViewModel.DocumentBeingAmounted) && _viewModel?.DocumentBeingAmounted is not null)
        {
            FocusSetAmountInput();
        }
    }

    private void FocusSetAmountInput()
    {
        Dispatcher.UIThread.Post(() =>
        {
            AmountValueTextBox.Focus();
            AmountValueTextBox.SelectAll();
        }, DispatcherPriority.Background);
    }

    private void SyncTransactionImportWindow()
    {
        if (_viewModel?.IsTransactionImportOpen == true)
        {
            ShowTransactionImportWindow();
        }
        else
        {
            CloseTransactionImportWindow();
        }
    }

    private void ShowTransactionImportWindow()
    {
        if (_transactionImportWindow is not null)
        {
            _transactionImportWindow.Activate();
            return;
        }

        _transactionImportWindow = new TransactionImportWindow
        {
            DataContext = _viewModel,
        };
        _transactionImportWindow.Closing += OnTransactionImportWindowClosing;
        _transactionImportWindow.Closed += OnTransactionImportWindowClosed;

        if (TopLevel.GetTopLevel(this) is Window owner)
        {
            _transactionImportWindow.Show(owner);
        }
        else
        {
            _transactionImportWindow.Show();
        }
    }

    private void CloseTransactionImportWindow()
    {
        if (_isSyncingTransactionImportWindow || _transactionImportWindow is null)
        {
            return;
        }

        _transactionImportWindow.Close();
    }

    private void OnTransactionImportWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_viewModel is null || !_viewModel.IsTransactionImportOpen || _isSyncingTransactionImportWindow)
        {
            return;
        }

        _isSyncingTransactionImportWindow = true;
        try
        {
            _viewModel.CloseTransactionImportCommand.Execute(null);
        }
        finally
        {
            _isSyncingTransactionImportWindow = false;
        }
    }

    private void OnTransactionImportWindowClosed(object? sender, EventArgs e)
    {
        if (sender is TransactionImportWindow window)
        {
            window.Closing -= OnTransactionImportWindowClosing;
            window.Closed -= OnTransactionImportWindowClosed;
        }

        if (ReferenceEquals(sender, _transactionImportWindow))
        {
            _transactionImportWindow = null;
        }
    }
}
