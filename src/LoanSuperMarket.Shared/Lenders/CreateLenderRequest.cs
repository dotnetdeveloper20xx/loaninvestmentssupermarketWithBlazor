using System.ComponentModel.DataAnnotations;

namespace LoanSuperMarket.Shared.Lenders;

public sealed class CreateLenderRequest
{
    [Required(ErrorMessage = "Company name is required.")]
    [StringLength(200, ErrorMessage = "Company name cannot exceed 200 characters.")]
    public string CompanyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Contact name is required.")]
    [StringLength(150, ErrorMessage = "Contact name cannot exceed 150 characters.")]
    public string ContactName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email address is not valid.")]
    [StringLength(250, ErrorMessage = "Email cannot exceed 250 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [StringLength(50, ErrorMessage = "Phone number cannot exceed 50 characters.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Range(0, 999999999, ErrorMessage = "Available funds cannot be negative.")]
    public decimal AvailableFunds { get; set; }
}