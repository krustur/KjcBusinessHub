using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using KjcBusinessHub.UI.ViewModels;

namespace KjcBusinessHub.UI.Views;

public partial class CalendarView : UserControl
{
    public CalendarView()
    {
        InitializeComponent();
    }

    private async void OnDayCellPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control source || !e.GetCurrentPoint(source).Properties.IsRightButtonPressed)
            return;

        if (sender is not Button { DataContext: CalendarDayCell { Date: { } date } })
            return;

        if (DataContext is not CalendarViewModel viewModel)
            return;

        e.Handled = true;
        await viewModel.ResetAbsenceCommand.ExecuteAsync(date);
    }
}
