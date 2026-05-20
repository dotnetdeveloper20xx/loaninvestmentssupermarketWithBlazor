using System.ComponentModel.DataAnnotations;

namespace LoanSuperMarket.Shared.Borrowers;

public sealed class CreateBorrowerRequest : IValidatableObject
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email address is not valid.")]
    [StringLength(250, ErrorMessage = "Email cannot exceed 250 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [StringLength(50, ErrorMessage = "Phone number cannot exceed 50 characters.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Date of birth is required.")]
    public DateTime DateOfBirth { get; set; } = DateTime.Today.AddYears(-25);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateOfBirth.Date > DateTime.Today.AddYears(-18))
        {
            yield return new ValidationResult(
                "Borrower must be at least 18 years old.",
                [nameof(DateOfBirth)]);
        }
    }
}