using KjcBusinessHub.Application.Entities;
using KjcBusinessHub.Application.Validators;
using Xunit;

namespace KjcBusinessHub.Application.Tests.Validators;

public class TransactionValidatorTests
{
    private readonly TransactionValidator _validator = new();

    private static Transaction ValidTransaction() => new()
    {
        Id = Guid.NewGuid(),
        AccountingDate = DateOnly.FromDateTime(DateTime.Today),
        TransactionDate = DateOnly.FromDateTime(DateTime.Today),
        Amount = 100m,
        Balance = 1000m,
        Description = "Test transaction",
    };

    [Fact]
    public void Valid_transaction_passes_validation()
    {
        var result = _validator.Validate(ValidTransaction());
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Empty_description_fails_validation()
    {
        var tx = ValidTransaction();
        tx.Description = string.Empty;
        var result = _validator.Validate(tx);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(Transaction.Description));
    }

    [Fact]
    public void Description_exceeding_500_chars_fails_validation()
    {
        var tx = ValidTransaction();
        tx.Description = new string('x', 501);
        var result = _validator.Validate(tx);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Empty_guid_fails_validation()
    {
        var tx = ValidTransaction();
        tx.Id = Guid.Empty;
        var result = _validator.Validate(tx);
        Assert.False(result.IsValid);
    }
}
