using FluentValidation;
using LoanSuperMarket.Application.Common.Behaviours;
using LoanSuperMarket.Application.Features.LoanApplications.ProductMatching;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace LoanSuperMarket.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(assembly);
        });

        services.AddValidatorsFromAssembly(assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AccountStatusBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LimitEnforcementBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ResourceAuthorizationBehaviour<,>));

        services.AddScoped<ProductMatchingService>();

        return services;
    }
}