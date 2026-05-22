using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Application.Features.LoanApplications.ProductMatching;
using LoanSuperMarket.Domain.Entities.Identity;
using LoanSuperMarket.Infrastructure.Identity;
using LoanSuperMarket.Infrastructure.Persistence;
using LoanSuperMarket.Infrastructure.Repositories;
using LoanSuperMarket.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LoanSuperMarket.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        services.AddDbContext<AuthIdentityDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        services.AddIdentity<ApplicationUser, CustomRole>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireDigit = true;
            options.Password.RequireNonAlphanumeric = true;

            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.AllowedForNewUsers = true;

            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<AuthIdentityDbContext>()
        .AddDefaultTokenProviders();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddScoped<IIdentityService, IdentityService>();

        services.AddScoped<ILoanProductRepository, LoanProductRepository>();

        services.AddScoped<IBorrowerRepository, BorrowerRepository>();

        services.AddScoped<ILenderRepository, LenderRepository>();

        services.AddScoped<ILoanApplicationRepository, LoanApplicationRepository>();

        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        services.AddScoped<ITwoFactorService, TwoFactorService>();

        services.AddScoped<ISessionService, SessionService>();

        services.AddScoped<IPermissionResolver, PermissionResolver>();

        services.AddScoped<IRoleManagementService, RoleManagementService>();

        services.AddScoped<IClientInfoProvider, ClientInfoProvider>();
        services.AddScoped<IEmailService, NoOpEmailService>();

        services.AddScoped<IApplicationDocumentRepository, ApplicationDocumentRepository>();
        services.AddScoped<IDocumentStorageService, StubDocumentStorageService>();
        services.AddScoped<ProductMatchingService>();

        return services;
    }
}