namespace KjcBusinessHub.Application.Validators;

public sealed class ValidationResult
{
    private readonly List<ValidationError> _errors = [];

    public bool IsValid => _errors.Count == 0;

    public IReadOnlyList<ValidationError> Errors => _errors;

    internal void AddError(string propertyName, string message) =>
        _errors.Add(new ValidationError(propertyName, message));
}

public sealed record ValidationError(string PropertyName, string Message);
