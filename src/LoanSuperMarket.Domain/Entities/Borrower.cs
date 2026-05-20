using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Enums;

namespace LoanSuperMarket.Domain.Entities;

public sealed class Borrower : AuditableEntity
{
    private Borrower()
    {
        FirstName = string.Empty;
        LastName = string.Empty;
        Email = string.Empty;
        PhoneNumber = string.Empty;
    }

    private Borrower(
        string firstName,
        string lastName,
        string email,
        string phoneNumber,
        DateTime dateOfBirth)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PhoneNumber = phoneNumber;
        DateOfBirth = dateOfBirth;
        Status = BorrowerStatus.PendingVerification;
    }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public string Email { get; private set; }

    public string PhoneNumber { get; private set; }

    public DateTime DateOfBirth { get; private set; }

    public BorrowerStatus Status { get; private set; }

    public string FullName => $"{FirstName} {LastName}";

    public static Borrower Create(
        string firstName,
        string lastName,
        string email,
        string phoneNumber,
        DateTime dateOfBirth)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new DomainException("First name is required.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new DomainException("Last name is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new DomainException("Phone number is required.");
        }

        if (dateOfBirth.Date > DateTime.UtcNow.Date.AddYears(-18))
        {
            throw new DomainException("Borrower must be at least 18 years old.");
        }

        return new Borrower(
            firstName.Trim(),
            lastName.Trim(),
            email.Trim().ToLowerInvariant(),
            phoneNumber.Trim(),
            dateOfBirth.Date);
    }

    public void Verify()
    {
        if (Status != BorrowerStatus.PendingVerification)
        {
            throw new DomainException("Only pending borrowers can be verified.");
        }

        Status = BorrowerStatus.Verified;
        MarkUpdated();
    }

    public void Suspend()
    {
        if (Status == BorrowerStatus.Archived)
        {
            throw new DomainException("Archived borrowers cannot be suspended.");
        }

        Status = BorrowerStatus.Suspended;
        MarkUpdated();
    }

    public void Archive()
    {
        if (Status == BorrowerStatus.Archived)
        {
            throw new DomainException("Borrower is already archived.");
        }

        Status = BorrowerStatus.Archived;
        MarkUpdated();
    }
}