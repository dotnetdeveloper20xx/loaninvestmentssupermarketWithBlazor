using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Domain.ValueObjects;

namespace LoanSuperMarket.Domain.Entities;

public sealed class LoanProduct : AuditableEntity
{
    private LoanProduct()
    {
        Title = string.Empty;
        Description = string.Empty;
        MinimumAmount = Money.Create(0);
        MaximumAmount = Money.Create(0);
        InterestRate = InterestRate.Create(1);
    }

    private LoanProduct(
        string title,
        string description,
        Money minimumAmount,
        Money maximumAmount,
        InterestRate interestRate,
        int minimumTermMonths,
        int maximumTermMonths,
        Guid lenderId)
    {
        Title = title;
        Description = description;
        MinimumAmount = minimumAmount;
        MaximumAmount = maximumAmount;
        InterestRate = interestRate;
        MinimumTermMonths = minimumTermMonths;
        MaximumTermMonths = maximumTermMonths;
        LenderId = lenderId;
        Status = LoanProductStatus.Draft;
    }

    public string Title { get; private set; }

    public string Description { get; private set; }

    public Money MinimumAmount { get; private set; }

    public Money MaximumAmount { get; private set; }

    public InterestRate InterestRate { get; private set; }

    public int MinimumTermMonths { get; private set; }

    public int MaximumTermMonths { get; private set; }

    public Guid LenderId { get; private set; }

    public LoanProductStatus Status { get; private set; }

    public static LoanProduct Create(
        string title,
        string description,
        Money minimumAmount,
        Money maximumAmount,
        InterestRate interestRate,
        int minimumTermMonths,
        int maximumTermMonths,
        Guid lenderId)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Loan product title is required.");
        }

        if (title.Length > 150)
        {
            throw new DomainException("Loan product title cannot exceed 150 characters.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("Loan product description is required.");
        }

        if (minimumAmount.Amount <= 0)
        {
            throw new DomainException("Minimum loan amount must be greater than zero.");
        }

        if (maximumAmount.Amount <= 0)
        {
            throw new DomainException("Maximum loan amount must be greater than zero.");
        }

        if (minimumAmount.Currency != maximumAmount.Currency)
        {
            throw new DomainException("Minimum and maximum loan amounts must use the same currency.");
        }

        if (minimumAmount.Amount > maximumAmount.Amount)
        {
            throw new DomainException("Minimum loan amount cannot be greater than maximum loan amount.");
        }

        if (minimumTermMonths <= 0)
        {
            throw new DomainException("Minimum term must be greater than zero.");
        }

        if (maximumTermMonths <= 0)
        {
            throw new DomainException("Maximum term must be greater than zero.");
        }

        if (minimumTermMonths > maximumTermMonths)
        {
            throw new DomainException("Minimum term cannot be greater than maximum term.");
        }

        if (lenderId == Guid.Empty)
        {
            throw new DomainException("Lender id is required.");
        }

        return new LoanProduct(
            title.Trim(),
            description.Trim(),
            minimumAmount,
            maximumAmount,
            interestRate,
            minimumTermMonths,
            maximumTermMonths,
            lenderId);
    }

    public void UpdateDetails(
        string title,
        string description,
        Money minimumAmount,
        Money maximumAmount,
        InterestRate interestRate,
        int minimumTermMonths,
        int maximumTermMonths)
    {
        if (Status is LoanProductStatus.Published or LoanProductStatus.Archived)
        {
            throw new DomainException("Published or archived loan products cannot be edited.");
        }

        var updated = Create(
            title,
            description,
            minimumAmount,
            maximumAmount,
            interestRate,
            minimumTermMonths,
            maximumTermMonths,
            LenderId);

        Title = updated.Title;
        Description = updated.Description;
        MinimumAmount = updated.MinimumAmount;
        MaximumAmount = updated.MaximumAmount;
        InterestRate = updated.InterestRate;
        MinimumTermMonths = updated.MinimumTermMonths;
        MaximumTermMonths = updated.MaximumTermMonths;

        MarkUpdated();
    }

    public void SubmitForApproval()
    {
        if (Status != LoanProductStatus.Draft)
        {
            throw new DomainException("Only draft loan products can be submitted for approval.");
        }

        Status = LoanProductStatus.PendingApproval;
        MarkUpdated();
    }

    public void Approve()
    {
        if (Status != LoanProductStatus.PendingApproval)
        {
            throw new DomainException("Only pending loan products can be approved.");
        }

        Status = LoanProductStatus.Approved;
        MarkUpdated();
    }

    public void Publish()
    {
        if (Status != LoanProductStatus.Approved)
        {
            throw new DomainException("Only approved loan products can be published.");
        }

        Status = LoanProductStatus.Published;
        MarkUpdated();
    }

    public void Archive()
    {
        if (Status == LoanProductStatus.Archived)
        {
            throw new DomainException("Loan product is already archived.");
        }

        Status = LoanProductStatus.Archived;
        MarkUpdated();
    }
}