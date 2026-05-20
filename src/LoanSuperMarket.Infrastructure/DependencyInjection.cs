using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Infrastructure.Persistence;
using LoanSuperMarket.Infrastructure.Repositories;
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

        services.AddScoped<ILoanProductRepository, LoanProductRepository>();

        services.AddScoped<IBorrowerRepository, BorrowerRepository>();

        return services;
    }
}