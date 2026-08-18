using System;
using Avalonia.Data.Converters;
using Avalonia.Media;
using KjcBusinessHub.Application.Enums;

namespace KjcBusinessHub.UI.ViewModels;

public static class Converters
{
    public static readonly IValueConverter IsNotActive =
        new FuncValueConverter<SourceDocumentStatus, bool>(status => status != SourceDocumentStatus.Active);

    public static readonly IValueConverter StatusToneBackground =
        new FuncValueConverter<StatusTone, IBrush>(tone => tone switch
        {
            StatusTone.Success => Brush.Parse("#EAF7EE"),
            StatusTone.Warning => Brush.Parse("#FFF4E5"),
            StatusTone.Error => Brush.Parse("#FDECEC"),
            _ => Brush.Parse("#EEF5FF"),
        });

    public static readonly IValueConverter StatusToneBorder =
        new FuncValueConverter<StatusTone, IBrush>(tone => tone switch
        {
            StatusTone.Success => Brush.Parse("#7CC58F"),
            StatusTone.Warning => Brush.Parse("#F2B263"),
            StatusTone.Error => Brush.Parse("#DD7D7D"),
            _ => Brush.Parse("#7CAAF2"),
        });
}
