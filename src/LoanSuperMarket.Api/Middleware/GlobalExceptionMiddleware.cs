using System.Net;
using LoanSuperMarket.Application.Common.Models;
using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Shared.Common;

namespace LoanSuperMarket.Api.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ApplicationValidationException exception)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";

            var response = ApiResponse<object>.Fail(exception.Errors.ToList());

            await context.Response.WriteAsJsonAsync(response);
        }
        catch (DomainException exception)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";

            var response = ApiResponse<object>.Fail(exception.Message);

            await context.Response.WriteAsJsonAsync(response);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception occurred.");

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var response = ApiResponse<object>.Fail(
                "An unexpected error occurred. Please try again later.");

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}