using KjcBusinessHub.Application.Entities;

namespace KjcBusinessHub.Application.Validators;

public class TransactionValidator
{
    public ValidationResult Validate(Transaction transaction)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(transaction.Description))
            result.AddError(nameof(transaction.Description), "Description is required.");
        else if (transaction.Description.Length > 500)
            result.AddError(nameof(transaction.Description), "Description must not exceed 500 characters.");

        if (transaction.Id == Guid.Empty)
            result.AddError(nameof(transaction.Id), "Id must not be empty.");

        return result;
    }
}
