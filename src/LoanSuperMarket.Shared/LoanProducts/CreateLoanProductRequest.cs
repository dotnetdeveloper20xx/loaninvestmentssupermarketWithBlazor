using System.ComponentModel.DataAnnotations;

namespace LoanSuperMarket.Shared.LoanProducts;

public sealed class CreateLoanProductRequest : IValidatableObject
{
    [Required(ErrorMessage = "Product title is required.")]
    [StringLength(150, ErrorMessage = "Product title cannot exceed 150 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
    public string Description { get; set; } = string.Empty;

    [Range(1, 999999999, ErrorMessage = "Minimum amount must be greater than zero.")]
    public decimal MinimumAmount { get; set; }

    [Range(1, 999999999, ErrorMessage = "Maximum amount must be greater than zero.")]
    public decimal MaximumAmount { get; set; }

    [Range(0.01, 100, ErrorMessage = "Interest rate must be between 0.01 and 100.")]
    public decimal InterestRate { get; set; }

    [Range(1, 600, ErrorMessage = "Minimum term must be greater than zero.")]
    public int MinimumTermMonths { get; set; }

    [Range(1, 600, ErrorMessage = "Maximum term must be greater than zero.")]
    public int MaximumTermMonths { get; set; }

    [Required(ErrorMessage = "Lender ID is required.")]
    public Guid LenderId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MaximumAmount < MinimumAmount)
        {
            yield return new ValidationResult(
                "Maximum amount must be greater than or equal to minimum amount.",
                [nameof(MaximumAmount)]);
        }

        if (MaximumTermMonths < MinimumTermMonths)
        {
            yield return new ValidationResult(
                "Maximum term must be greater than or equal to minimum term.",
                [nameof(MaximumTermMonths)]);
        }

        if (LenderId == Guid.Empty)
        {
            yield return new ValidationResult(
                "Lender ID is required.",
                [nameof(LenderId)]);
        }
    }
}