using System;
using Avalonia.Data.Converters;
using KjcBusinessHub.Application.Enums;

namespace KjcBusinessHub.UI.ViewModels;

public static class Converters
{
    public static readonly IValueConverter IsNotActive =
        new FuncValueConverter<SourceDocumentStatus, bool>(status => status != SourceDocumentStatus.Active);
}
