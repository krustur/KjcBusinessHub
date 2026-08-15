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
}
