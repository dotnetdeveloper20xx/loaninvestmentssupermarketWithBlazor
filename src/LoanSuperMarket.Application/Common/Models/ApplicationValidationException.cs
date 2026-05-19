namespace LoanSuperMarket.Application.Common.Models;

public sealed class ApplicationValidationException : Exception
{
    public ApplicationValidationException(IReadOnlyList<string> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public IReadOnlyList<string> Errors { get; }
}