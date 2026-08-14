using FluentValidation;
using KjcBusinessHub.Application.Entities;

namespace KjcBusinessHub.Application.Validators;

public class SourceDocumentValidator : AbstractValidator<SourceDocument>
{
    public SourceDocumentValidator()
    {
        RuleFor(d => d.FileSubPath)
            .NotEmpty().WithMessage("FileSubPath is required.");

        RuleFor(d => d.FileCreatedDate)
            .NotEqual(default(DateTimeOffset)).WithMessage("FileCreatedDate is required.");

        RuleFor(d => d.Id)
            .NotEqual(Guid.Empty).WithMessage("Id must not be empty.");
    }
}
