using KjcBusinessHub.Application.Entities;

namespace KjcBusinessHub.Application.Validators;

public class SourceDocumentValidator
{
    public ValidationResult Validate(SourceDocument document)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(document.FileSubPath))
        {
            result.AddError(nameof(document.FileSubPath), "FileSubPath is required.");
        }

        if (document.FileCreatedDate == default)
        {
            result.AddError(nameof(document.FileCreatedDate), "FileCreatedDate is required.");
        }

        if (document.Id == Guid.Empty)
        {
            result.AddError(nameof(document.Id), "Id must not be empty.");
        }

        return result;
    }

    public ValidationResult ValidateSetAmount(SourceDocument document)
    {
        var result = new ValidationResult();

        if (!document.Amount.HasValue && !document.CcyAmount.HasValue)
        {
            result.AddError(nameof(document.Amount), "At least one of Amount or CcyAmount must be provided.");
        }

        if (document.CcyAmount.HasValue && !document.Ccy.HasValue)
        {
            result.AddError(nameof(document.Ccy), "A currency must be selected when a currency amount is provided.");
        }

        return result;
    }
}
