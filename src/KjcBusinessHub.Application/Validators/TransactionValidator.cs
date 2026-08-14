using FluentValidation;
using KjcBusinessHub.Application.Entities;

namespace KjcBusinessHub.Application.Validators;

public class TransactionValidator : AbstractValidator<Transaction>
{
    public TransactionValidator()
    {
        RuleFor(t => t.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");

        RuleFor(t => t.Id)
            .NotEqual(Guid.Empty).WithMessage("Id must not be empty.");
    }
}
