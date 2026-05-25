using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Application.Features.Funding;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LoanSuperMarket.Infrastructure.Persistence;

/// <summary>
/// Seeds development/demo data for the lending lifecycle.
/// Creates sample lenders, borrowers, products, applications, and funded loans.
/// Idempotent — skips if data already exists.
/// </summary>
public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        // Skip if we already have funded schedules
        if (await context.RepaymentSchedules.AnyAsync())
        {
            logger.LogDebug("Development data already seeded, skipping.");
            return;
        }

        logger.LogInformation("Seeding development data...");

        // --- Step 1: Create and save Lenders and Borrowers (no dependencies) ---
        var lender1 = Lender.Create("Apex Capital Partners", "James Richardson", "james@apexcapital.co.uk", "020 7946 0001", 500_000m);
        var lender2 = Lender.Create("Sterling Finance Group", "Sarah Mitchell", "sarah@sterlingfinance.co.uk", "020 7946 0002", 750_000m);

        // Verify lenders so they can fund
        lender1.Verify();
        lender2.Verify();

        await context.Lenders.AddRangeAsync(lender1, lender2);

        var borrower1 = Borrower.Create("Michael", "Thompson", "michael.thompson@email.com", "07700 900001", new DateTime(1985, 3, 15));
        var borrower2 = Borrower.Create("Emma", "Williams", "emma.williams@email.com", "07700 900002", new DateTime(1990, 7, 22));
        var borrower3 = Borrower.Create("David", "Chen", "david.chen@email.com", "07700 900003", new DateTime(1988, 11, 8));

        borrower1.Verify();
        borrower2.Verify();
        borrower3.Verify();

        await context.Borrowers.AddRangeAsync(borrower1, borrower2, borrower3);

        // SAVE to generate IDs for lenders and borrowers
        await context.SaveChangesAsync();
        logger.LogDebug("Saved lenders and borrowers.");

        // --- Step 2: Create and save Loan Products (depends on Lender IDs) ---
        var product1 = LoanProduct.Create(
            "Personal Growth Loan",
            "Flexible personal loan for home improvements and major purchases.",
            Money.Create(5_000m), Money.Create(50_000m),
            InterestRate.Create(10.5m), 12, 60, lender1.Id);
        product1.SubmitForApproval();
        product1.Approve();
        product1.Publish();

        var product2 = LoanProduct.Create(
            "Business Expansion Loan",
            "Working capital and expansion funding for SMEs.",
            Money.Create(10_000m), Money.Create(100_000m),
            InterestRate.Create(12m), 12, 48, lender2.Id);
        product2.SubmitForApproval();
        product2.Approve();
        product2.Publish();

        var product3 = LoanProduct.Create(
            "Quick Cash Loan",
            "Short-term loan for urgent financial needs.",
            Money.Create(1_000m), Money.Create(15_000m),
            InterestRate.Create(14m), 6, 24, lender1.Id);
        product3.SubmitForApproval();
        product3.Approve();
        product3.Publish();

        await context.LoanProducts.AddRangeAsync(product1, product2, product3);

        // SAVE to generate IDs for products
        await context.SaveChangesAsync();
        logger.LogDebug("Saved loan products.");

        // --- Step 3: Create and save Loan Applications (depends on Borrower and Product IDs) ---
        var app1 = LoanApplication.CreateDraft(borrower1.Id, 25_000m, 36, "Home renovation project");
        app1.SelectProduct(product1.Id);
        app1.Submit();
        app1.MarkUnderReview();
        app1.Approve("Good credit history, stable income.", "admin@loansupermarket.com");

        var app2 = LoanApplication.CreateDraft(borrower2.Id, 40_000m, 24, "Expanding online retail business");
        app2.SelectProduct(product2.Id);
        app2.Submit();
        app2.MarkUnderReview();
        app2.Approve("Strong business plan, existing revenue.", "admin@loansupermarket.com");

        var app3 = LoanApplication.CreateDraft(borrower3.Id, 8_000m, 12, "Emergency car repair");
        app3.SelectProduct(product3.Id);
        app3.Submit();
        app3.MarkUnderReview();
        app3.Approve("Verified employment, low debt ratio.", "admin@loansupermarket.com");

        // --- One already funded application with schedule ---
        var app4 = LoanApplication.CreateDraft(borrower1.Id, 30_000m, 24, "Office equipment purchase");
        app4.SelectProduct(product2.Id);
        app4.Submit();
        app4.MarkUnderReview();
        app4.Approve("Repeat borrower, excellent history.", "admin@loansupermarket.com");
        app4.Fund();

        await context.LoanApplications.AddRangeAsync(app1, app2, app3, app4);

        // SAVE to generate IDs for applications
        await context.SaveChangesAsync();
        logger.LogDebug("Saved loan applications.");

        // --- Step 4: Generate repayment schedule (depends on Application and Lender IDs) ---
        var amortizationService = scope.ServiceProvider.GetRequiredService<IAmortizationService>();
        var effectiveRate = 12m + 2m; // Base 12% + Tier B adjustment

        var schedule = amortizationService.GenerateSchedule(
            app4.Id, lender2.Id, 30_000m, effectiveRate, 24, DateTime.UtcNow.AddMonths(-3));

        // Simulate 3 payments already made
        var installments = schedule.Installments.OrderBy(i => i.InstallmentNumber).ToList();
        if (installments.Count >= 3)
        {
            installments[0].RecordFullPayment(DateTime.UtcNow.AddMonths(-2));
            installments[1].RecordFullPayment(DateTime.UtcNow.AddMonths(-1));
            installments[2].RecordFullPayment(DateTime.UtcNow.AddDays(-5));
        }

        // Deduct from lender (this modifies lender2 which is already tracked)
        lender2.DeductFunds(30_000m);

        await context.RepaymentSchedules.AddAsync(schedule);

        // FINAL SAVE for schedule and lender balance update
        await context.SaveChangesAsync();
        logger.LogDebug("Saved repayment schedule and updated lender balance.");

        logger.LogInformation(
            "Development data seeded: 2 lenders, 3 borrowers, 3 products, " +
            "3 approved applications (ready for funding), 1 funded loan with 3 payments made.");
    }
}
