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
            result.AddError(nameof(document.Amount), "Amount or CcyAmount is required.");
        }

        if (document.CcyAmount.HasValue && !document.Ccy.HasValue)
        {
            result.AddError(nameof(document.Ccy), "Ccy is required when CcyAmount is set.");
        }

        return result;
    }
}
