using System.ComponentModel.DataAnnotations;

namespace LoanSuperMarket.Shared.Auth;

/// <summary>
/// Request model for new user registration.
/// </summary>
public sealed class RegisterRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email address is not valid.")]
    [StringLength(250, ErrorMessage = "Email cannot exceed 250 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 128 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "First name is required.")]
    [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters.")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// The type of user registering. Must be "Borrower" or "Lender".
    /// </summary>
    [Required(ErrorMessage = "User type is required.")]
    public string UserType { get; set; } = string.Empty;

    /// <summary>
    /// Company name, required for Lender registrations.
    /// </summary>
    public string? CompanyName { get; set; }
}
