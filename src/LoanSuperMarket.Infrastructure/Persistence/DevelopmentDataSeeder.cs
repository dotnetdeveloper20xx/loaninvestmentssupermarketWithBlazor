using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Domain.Entities.Identity;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LoanSuperMarket.Infrastructure.Persistence;

/// <summary>
/// Seeds comprehensive development/demo data for the lending lifecycle.
/// Creates users, lenders, borrowers, products, applications in various states,
/// funded loans with repayment schedules, and audit logs.
/// Idempotent — skips if data already exists.
/// </summary>
public static class DevelopmentDataSeeder
{
    private const string DefaultPassword = "Demo@12345!";

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        if (await context.RepaymentSchedules.AnyAsync())
        {
            logger.LogDebug("Development data already seeded, skipping.");
            return;
        }

        logger.LogInformation("Seeding comprehensive development data...");

        // ═══════════════════════════════════════════════════════════════════
        // STEP 1: Create Identity Users (Admin, CRM, Staff, Lenders, Borrowers)
        // ═══════════════════════════════════════════════════════════════════
        await SeedUsersAsync(userManager, logger);

        // ═══════════════════════════════════════════════════════════════════
        // STEP 2: Create Lenders (business entities)
        // ═══════════════════════════════════════════════════════════════════
        var lenders = new[]
        {
            Lender.Create("Apex Capital Partners", "James Richardson", "lender1@demo.com", "020 7946 0001", 1_200_000m),
            Lender.Create("Sterling Finance Group", "Sarah Mitchell", "lender2@demo.com", "020 7946 0002", 850_000m),
            Lender.Create("Meridian Investments", "Robert Clarke", "lender3@demo.com", "020 7946 0003", 600_000m),
            Lender.Create("Horizon Capital Ltd", "Victoria Adams", "lender4@demo.com", "020 7946 0004", 950_000m),
            Lender.Create("Pinnacle Lending Co", "Thomas Wright", "lender5@demo.com", "020 7946 0005", 400_000m),
        };

        foreach (var lender in lenders) lender.Verify();
        await context.Lenders.AddRangeAsync(lenders);

        // ═══════════════════════════════════════════════════════════════════
        // STEP 3: Create Borrowers
        // ═══════════════════════════════════════════════════════════════════
        var borrowers = new[]
        {
            Borrower.Create("Michael", "Thompson", "borrower1@demo.com", "07700 900001", new DateTime(1985, 3, 15)),
            Borrower.Create("Emma", "Williams", "borrower2@demo.com", "07700 900002", new DateTime(1990, 7, 22)),
            Borrower.Create("David", "Chen", "borrower3@demo.com", "07700 900003", new DateTime(1988, 11, 8)),
            Borrower.Create("Sophie", "Taylor", "borrower4@demo.com", "07700 900004", new DateTime(1992, 5, 30)),
            Borrower.Create("James", "Patel", "borrower5@demo.com", "07700 900005", new DateTime(1987, 9, 12)),
        };

        foreach (var borrower in borrowers) borrower.Verify();
        await context.Borrowers.AddRangeAsync(borrowers);

        await context.SaveChangesAsync();

        // Link borrowers and lenders to their user accounts
        var borrowerEmails = new[] { "borrower1@demo.com", "borrower2@demo.com", "borrower3@demo.com", "borrower4@demo.com", "borrower5@demo.com" };
        for (var i = 0; i < borrowers.Length; i++)
        {
            var user = await userManager.FindByEmailAsync(borrowerEmails[i]);
            if (user is not null)
            {
                context.Entry(borrowers[i]).Property("UserId").CurrentValue = user.Id;
            }
        }

        var lenderEmails = new[] { "lender1@demo.com", "lender2@demo.com", "lender3@demo.com", "lender4@demo.com", "lender5@demo.com" };
        for (var i = 0; i < lenders.Length; i++)
        {
            var user = await userManager.FindByEmailAsync(lenderEmails[i]);
            if (user is not null)
            {
                context.Entry(lenders[i]).Property("UserId").CurrentValue = user.Id;
            }
        }

        await context.SaveChangesAsync();
        logger.LogDebug("Saved 5 lenders and 5 borrowers (linked to user accounts).");

        // ═══════════════════════════════════════════════════════════════════
        // STEP 4: Create Loan Products (4 per lender = 20 total)
        // ═══════════════════════════════════════════════════════════════════
        var products = CreateLoanProducts(lenders);
        await context.LoanProducts.AddRangeAsync(products);
        await context.SaveChangesAsync();
        logger.LogDebug("Saved {Count} loan products.", products.Count);

        // ═══════════════════════════════════════════════════════════════════
        // STEP 5: Create Loan Applications in various states
        // ═══════════════════════════════════════════════════════════════════
        var publishedProducts = products.Where(p => p.Status == LoanProductStatus.Published).ToList();
        var applications = CreateLoanApplications(borrowers, publishedProducts);
        await context.LoanApplications.AddRangeAsync(applications);
        await context.SaveChangesAsync();
        logger.LogDebug("Saved {Count} loan applications.", applications.Count);

        // ═══════════════════════════════════════════════════════════════════
        // STEP 6: Generate Repayment Schedules for funded loans
        // ═══════════════════════════════════════════════════════════════════
        var amortizationService = scope.ServiceProvider.GetRequiredService<IAmortizationService>();
        var fundedApps = applications.Where(a => a.Status == LoanApplicationStatus.Funded).ToList();

        foreach (var fundedApp in fundedApps)
        {
            var product = publishedProducts.First(p => p.Id == fundedApp.LoanProductId);
            var lender = lenders.First(l => l.Id == product.LenderId);
            var effectiveRate = product.InterestRate.Percentage + 2m; // base + tier adjustment

            var monthsAgo = Random.Shared.Next(2, 8);
            var schedule = amortizationService.GenerateSchedule(
                fundedApp.Id, lender.Id, fundedApp.RequestedAmount.Amount,
                effectiveRate, fundedApp.TermMonths, DateTime.UtcNow.AddMonths(-monthsAgo));

            // Simulate payments for past-due installments
            var installments = schedule.Installments.OrderBy(i => i.InstallmentNumber).ToList();
            var paymentsMade = Math.Min(monthsAgo - 1, installments.Count);
            for (var i = 0; i < paymentsMade; i++)
            {
                installments[i].RecordFullPayment(installments[i].DueDate.AddDays(Random.Shared.Next(0, 3)));
            }

            lender.DeductFunds(fundedApp.RequestedAmount.Amount);
            await context.RepaymentSchedules.AddAsync(schedule);
        }

        await context.SaveChangesAsync();
        logger.LogDebug("Saved {Count} repayment schedules.", fundedApps.Count);

        // ═══════════════════════════════════════════════════════════════════
        // STEP 7: Create Audit Logs
        // ═══════════════════════════════════════════════════════════════════
        var auditLogs = new[]
        {
            AuditLog.Create("LoanApplication", applications[0].Id, "Submitted", "Loan application submitted for review.", "borrower1@demo.com"),
            AuditLog.Create("LoanApplication", applications[1].Id, "Approved", "Application approved after credit check.", "crm1@demo.com"),
            AuditLog.Create("LoanApplication", applications[3].Id, "Funded", "Loan funded by Apex Capital Partners.", "lender1@demo.com"),
            AuditLog.Create("Lender", lenders[0].Id, "Verified", "Lender identity and FCA registration verified.", "admin@loansupermarket.com"),
            AuditLog.Create("Borrower", borrowers[0].Id, "Verified", "Borrower KYC documents verified.", "crm1@demo.com"),
            AuditLog.Create("LoanProduct", products[0].Id, "Published", "Loan product approved and published to marketplace.", "crm1@demo.com"),
            AuditLog.Create("RepaymentSchedule", null, "PaymentReceived", "Monthly payment of £1,423.50 received on time.", "System"),
            AuditLog.Create("ApplicationUser", null, "Login", "Admin user logged in from 192.168.1.100.", "admin@loansupermarket.com"),
        };

        await context.AuditLogs.AddRangeAsync(auditLogs);
        await context.SaveChangesAsync();

        logger.LogInformation(
            "Development data seeded: 5 lenders, 5 borrowers, {ProductCount} products, " +
            "{AppCount} applications, {FundedCount} funded loans with schedules, {AuditCount} audit logs.",
            products.Count, applications.Count, fundedApps.Count, auditLogs.Length);
    }

    private static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager, ILogger logger)
    {
        var users = new (string Email, string First, string Last, string Role)[]
        {
            // Admins
            ("admin2@demo.com", "Rachel", "Morgan", "Admin"),
            // CRM Managers
            ("crm1@demo.com", "Oliver", "Hughes", "CrmManager"),
            ("crm2@demo.com", "Charlotte", "Evans", "CrmManager"),
            // Customer Service
            ("staff1@demo.com", "Daniel", "Cooper", "CustomerService"),
            ("staff2@demo.com", "Hannah", "Ward", "CustomerService"),
            // Lenders
            ("lender1@demo.com", "James", "Richardson", "Lender"),
            ("lender2@demo.com", "Sarah", "Mitchell", "Lender"),
            ("lender3@demo.com", "Robert", "Clarke", "Lender"),
            ("lender4@demo.com", "Victoria", "Adams", "Lender"),
            ("lender5@demo.com", "Thomas", "Wright", "Lender"),
            // Borrowers
            ("borrower1@demo.com", "Michael", "Thompson", "Borrower"),
            ("borrower2@demo.com", "Emma", "Williams", "Borrower"),
            ("borrower3@demo.com", "David", "Chen", "Borrower"),
            ("borrower4@demo.com", "Sophie", "Taylor", "Borrower"),
            ("borrower5@demo.com", "James", "Patel", "Borrower"),
        };

        foreach (var (email, first, last, role) in users)
        {
            if (await userManager.FindByEmailAsync(email) is not null) continue;

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = first,
                LastName = last,
                EmailConfirmed = true,
                AccountStatus = AccountStatus.Active,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-Random.Shared.Next(10, 90))
            };

            var result = await userManager.CreateAsync(user, DefaultPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role);
            }
            else
            {
                logger.LogWarning("Failed to create user {Email}: {Errors}", email,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        logger.LogDebug("Seeded demo user accounts.");
    }

    private static List<LoanProduct> CreateLoanProducts(Lender[] lenders)
    {
        var productDefinitions = new (string Title, string Desc, decimal Min, decimal Max, decimal Rate, int MinTerm, int MaxTerm)[]
        {
            // Lender 1 - Apex Capital (personal/home)
            ("Personal Growth Loan", "Flexible personal loan for home improvements and major purchases.", 5_000m, 50_000m, 10.5m, 12, 60),
            ("Home Renovation Loan", "Dedicated funding for property improvements and extensions.", 10_000m, 75_000m, 9.8m, 24, 84),
            ("Debt Consolidation Loan", "Simplify your finances by combining multiple debts into one.", 3_000m, 40_000m, 11.2m, 12, 48),
            ("Green Energy Loan", "Finance solar panels, heat pumps, and energy efficiency upgrades.", 5_000m, 30_000m, 8.5m, 12, 60),
            // Lender 2 - Sterling Finance (business)
            ("Business Expansion Loan", "Working capital and expansion funding for SMEs.", 10_000m, 150_000m, 12m, 12, 48),
            ("Startup Accelerator Loan", "Seed funding for early-stage businesses with proven traction.", 5_000m, 50_000m, 13.5m, 6, 36),
            ("Invoice Finance Facility", "Bridge cash flow gaps while waiting for invoice payments.", 2_000m, 80_000m, 11m, 3, 12),
            ("Equipment Finance Loan", "Purchase or lease business equipment and machinery.", 5_000m, 100_000m, 10m, 12, 60),
            // Lender 3 - Meridian (short-term)
            ("Quick Cash Loan", "Short-term loan for urgent financial needs.", 1_000m, 15_000m, 14m, 6, 24),
            ("Emergency Bridge Loan", "Fast access to funds for unexpected expenses.", 500m, 10_000m, 15m, 3, 12),
            ("Wedding Finance", "Make your special day perfect without financial stress.", 3_000m, 25_000m, 12.5m, 12, 36),
            ("Holiday Loan", "Spread the cost of your dream holiday.", 1_000m, 15_000m, 13m, 6, 24),
            // Lender 4 - Horizon Capital (property)
            ("Buy-to-Let Mortgage", "Investment property financing for landlords.", 50_000m, 500_000m, 7.5m, 60, 300),
            ("Property Development Loan", "Short-term finance for property development projects.", 25_000m, 250_000m, 9m, 12, 36),
            ("Commercial Mortgage", "Long-term financing for commercial property purchases.", 75_000m, 750_000m, 6.8m, 60, 240),
            ("Bridging Loan", "Short-term property finance to bridge between transactions.", 10_000m, 200_000m, 12m, 1, 18),
            // Lender 5 - Pinnacle (specialist)
            ("Medical Professional Loan", "Tailored lending for doctors, dentists, and healthcare professionals.", 10_000m, 100_000m, 8m, 12, 60),
            ("Legal Professional Loan", "Specialist finance for solicitors and barristers.", 10_000m, 80_000m, 8.5m, 12, 48),
            ("Education Loan", "Fund professional qualifications and postgraduate study.", 5_000m, 50_000m, 9m, 12, 72),
            ("Vehicle Finance", "New and used car finance with competitive rates.", 3_000m, 60_000m, 10.5m, 12, 60),
        };

        var products = new List<LoanProduct>();
        for (var i = 0; i < productDefinitions.Length; i++)
        {
            var def = productDefinitions[i];
            var lender = lenders[i / 4]; // 4 products per lender

            var product = LoanProduct.Create(
                def.Title, def.Desc,
                Money.Create(def.Min), Money.Create(def.Max),
                InterestRate.Create(def.Rate),
                def.MinTerm, def.MaxTerm, lender.Id);

            product.SubmitForApproval();
            product.Approve();
            product.Publish();
            products.Add(product);
        }

        return products;
    }

    private static List<LoanApplication> CreateLoanApplications(
        Borrower[] borrowers, List<LoanProduct> publishedProducts)
    {
        var applications = new List<LoanApplication>();

        var appDefinitions = new (int BorrowerIdx, int ProductIdx, decimal Amount, int Term, string Purpose, string TargetStatus)[]
        {
            // Borrower 1 - Michael Thompson (3 apps: 1 funded, 1 approved, 1 submitted)
            (0, 0, 25_000m, 36, "Kitchen and bathroom renovation", "Funded"),
            (0, 4, 45_000m, 24, "Expanding my e-commerce warehouse", "Approved"),
            (0, 8, 5_000m, 12, "Emergency boiler replacement", "Submitted"),

            // Borrower 2 - Emma Williams (3 apps: 1 funded, 1 under review, 1 rejected)
            (1, 1, 35_000m, 48, "Loft conversion and new roof", "Funded"),
            (1, 5, 20_000m, 18, "Launch new product line for online store", "UnderReview"),
            (1, 10, 8_000m, 24, "Destination wedding in Italy", "Rejected"),

            // Borrower 3 - David Chen (3 apps: 1 funded, 1 approved, 1 draft)
            (2, 7, 30_000m, 36, "CNC machine for workshop", "Funded"),
            (2, 2, 15_000m, 24, "Consolidate credit card debts", "Approved"),
            (2, 11, 4_000m, 12, "Family holiday to Japan", "Draft"),

            // Borrower 4 - Sophie Taylor (3 apps: 2 funded, 1 submitted)
            (3, 3, 12_000m, 36, "Solar panel installation", "Funded"),
            (3, 16, 25_000m, 48, "MBA at London Business School", "Funded"),
            (3, 9, 3_000m, 6, "Laptop replacement for freelance work", "Submitted"),

            // Borrower 5 - James Patel (3 apps: 1 funded, 1 approved, 1 documents requested)
            (4, 19, 18_000m, 36, "Tesla Model 3 deposit and finance", "Funded"),
            (4, 6, 12_000m, 6, "Bridge invoice gap for consulting firm", "Approved"),
            (4, 0, 30_000m, 48, "Extension to family home", "DocumentsRequested"),
        };

        foreach (var def in appDefinitions)
        {
            var borrower = borrowers[def.BorrowerIdx];
            var product = publishedProducts[def.ProductIdx];

            var app = LoanApplication.CreateDraft(borrower.Id, def.Amount, def.Term, def.Purpose);
            app.SelectProduct(product.Id);

            switch (def.TargetStatus)
            {
                case "Submitted":
                    app.Submit();
                    break;
                case "UnderReview":
                    app.Submit();
                    app.MarkUnderReview();
                    break;
                case "Approved":
                    app.Submit();
                    app.MarkUnderReview();
                    app.Approve("Credit check passed. Income verified.", "crm1@demo.com");
                    break;
                case "Rejected":
                    app.Submit();
                    app.MarkUnderReview();
                    app.Reject("Insufficient income for requested amount.", "crm2@demo.com");
                    break;
                case "Funded":
                    app.Submit();
                    app.MarkUnderReview();
                    app.Approve("All checks passed. Approved for funding.", "crm1@demo.com");
                    app.Fund();
                    break;
                case "DocumentsRequested":
                    app.Submit();
                    app.MarkUnderReview();
                    app.RequestDocuments("Please provide 3 months of bank statements.", "crm2@demo.com");
                    break;
                // "Draft" - already in draft, no transitions needed
            }

            applications.Add(app);
        }

        return applications;
    }
}
