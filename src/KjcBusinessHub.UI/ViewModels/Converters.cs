using System;
using Avalonia.Data.Converters;
using KjcBusinessHub.Application.Enums;

namespace KjcBusinessHub.UI.ViewModels;

public static class Converters
{
    public static readonly IValueConverter IsNotActive =
        new FuncValueConverter<SourceDocumentStatus, bool>(status => status != SourceDocumentStatus.Active);

    public static readonly IValueConverter HasAnnualType =
        new FuncValueConverter<SourceDocumentAnnualType, bool>(type => type != SourceDocumentAnnualType.NotAnnual);

    public static readonly IValueConverter IsNotAnnualType =
        new FuncValueConverter<SourceDocumentAnnualType, bool>(type => type == SourceDocumentAnnualType.NotAnnual);
}
