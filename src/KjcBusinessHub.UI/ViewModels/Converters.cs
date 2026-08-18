using System;
using System.Linq;
using Avalonia.Data.Converters;
using Avalonia.Media;
using KjcBusinessHub.Application.Enums;

namespace KjcBusinessHub.UI.ViewModels;

public static class Converters
{
    public static readonly IValueConverter IsNotActive =
        new FuncValueConverter<SourceDocumentStatus, bool>(status => status != SourceDocumentStatus.Active);

    /// <summary>Returns true when both bound values are the same non-null reference.</summary>
    public static readonly IMultiValueConverter AreReferenceEqual =
        new FuncMultiValueConverter<object?, bool>(values =>
        {
            var list = values.ToArray();
            return list.Length == 2 && list[0] is not null && ReferenceEquals(list[0], list[1]);
        });

    /// <summary>Returns true when the two bound values are different references (or either is null).</summary>
    public static readonly IMultiValueConverter AreReferenceNotEqual =
        new FuncMultiValueConverter<object?, bool>(values =>
        {
            var list = values.ToArray();
            return list.Length != 2 || list[0] is null || !ReferenceEquals(list[0], list[1]);
        });

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
