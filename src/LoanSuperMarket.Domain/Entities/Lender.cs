using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Enums;

namespace LoanSuperMarket.Domain.Entities;

public sealed class Lender : AuditableEntity
{
    private Lender()
    {
        CompanyName = string.Empty;
        ContactName = string.Empty;
        Email = string.Empty;
        PhoneNumber = string.Empty;
    }

    private Lender(
        string companyName,
        string contactName,
        string email,
        string phoneNumber,
        decimal availableFunds)
    {
        CompanyName = companyName;
        ContactName = contactName;
        Email = email;
        PhoneNumber = phoneNumber;
        AvailableFunds = availableFunds;
        Status = LenderStatus.PendingVerification;
    }

    public string CompanyName { get; private set; }

    public string ContactName { get; private set; }

    public string Email { get; private set; }

    public string PhoneNumber { get; private set; }

    public decimal AvailableFunds { get; private set; }

    public LenderStatus Status { get; private set; }

    public static Lender Create(
        string companyName,
        string contactName,
        string email,
        string phoneNumber,
        decimal availableFunds)
    {
        if (string.IsNullOrWhiteSpace(companyName))
        {
            throw new DomainException("Company name is required.");
        }

        if (string.IsNullOrWhiteSpace(contactName))
        {
            throw new DomainException("Contact name is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new DomainException("Phone number is required.");
        }

        if (availableFunds < 0)
        {
            throw new DomainException("Available funds cannot be negative.");
        }

        return new Lender(
            companyName.Trim(),
            contactName.Trim(),
            email.Trim().ToLowerInvariant(),
            phoneNumber.Trim(),
            decimal.Round(availableFunds, 2));
    }

    public void Verify()
    {
        if (Status != LenderStatus.PendingVerification)
        {
            throw new DomainException("Only pending lenders can be verified.");
        }

        Status = LenderStatus.Verified;
        MarkUpdated();
    }

    public void Suspend()
    {
        if (Status == LenderStatus.Archived)
        {
            throw new DomainException("Archived lenders cannot be suspended.");
        }

        Status = LenderStatus.Suspended;
        MarkUpdated();
    }

    public void Archive()
    {
        if (Status == LenderStatus.Archived)
        {
            throw new DomainException("Lender is already archived.");
        }

        Status = LenderStatus.Archived;
        MarkUpdated();
    }
}