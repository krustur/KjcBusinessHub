using KjcBusinessHub.Application.Entities;
using KjcBusinessHub.Application.Enums;
using KjcBusinessHub.Application.Validators;
using Xunit;

namespace KjcBusinessHub.Application.Tests.Validators;

public class SourceDocumentValidatorTests
{
    private readonly SourceDocumentValidator _validator = new();

    private static SourceDocument ValidDocument() => new()
    {
        Id = Guid.NewGuid(),
        FileSubPath = "2026-07/2026-07-01 Invoice.pdf",
        FileHash = "abc123",
        FileNameDate = new DateOnly(2026, 7, 1),
        Description = "Invoice",
        FileCreatedDate = DateTimeOffset.UtcNow,
        FileModifiedDate = DateTimeOffset.UtcNow,
    };

    [Fact]
    public void Valid_document_passes_validation()
    {
        var result = _validator.Validate(ValidDocument());
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Empty_file_sub_path_fails_validation()
    {
        var doc = ValidDocument();
        doc.FileSubPath = string.Empty;
        var result = _validator.Validate(doc);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Default_file_created_date_fails_validation()
    {
        var doc = ValidDocument();
        doc.FileCreatedDate = default;
        var result = _validator.Validate(doc);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Empty_guid_fails_validation()
    {
        var doc = ValidDocument();
        doc.Id = Guid.Empty;
        var result = _validator.Validate(doc);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Set_amount_validation_passes_with_amount_only()
    {
        var doc = ValidDocument();
        doc.Amount = 123.45m;

        var result = _validator.ValidateSetAmount(doc);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Set_amount_validation_passes_with_currency_amount_only()
    {
        var doc = ValidDocument();
        doc.Ccy = SourceDocumentCurrency.USD;
        doc.CcyAmount = 25m;

        var result = _validator.ValidateSetAmount(doc);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Set_amount_validation_fails_when_all_amounts_missing()
    {
        var result = _validator.ValidateSetAmount(ValidDocument());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(SourceDocument.Amount));
    }

    [Fact]
    public void Set_amount_validation_fails_when_currency_amount_has_no_currency()
    {
        var doc = ValidDocument();
        doc.CcyAmount = 25m;

        var result = _validator.ValidateSetAmount(doc);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(SourceDocument.Ccy));
    }

}
