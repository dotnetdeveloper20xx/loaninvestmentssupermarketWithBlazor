using LoanSuperMarket.Blazor;
using LoanSuperMarket.Blazor.Services;
using LoanSuperMarket.Blazor.Services.ApiClients;
using LoanSuperMarket.Blazor.Services.Auth;
using LoanSuperMarket.Blazor.Services.Drawers;
using LoanSuperMarket.Blazor.Services.Modals;
using LoanSuperMarket.Blazor.Services.Notifications;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;


var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl configuration is missing.");

// Auth state provider (must be registered before HttpClient so it's available to the handler)
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<JwtAuthenticationStateProvider>());
builder.Services.AddAuthorizationCore();

// Register AuthTokenHandler
builder.Services.AddScoped<AuthTokenHandler>();

// Register the primary HttpClient with AuthTokenHandler for automatic Bearer token attachment
// and 401 interception with token refresh
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthTokenHandler>();
    handler.InnerHandler = new HttpClientHandler();

    return new HttpClient(handler)
    {
        BaseAddress = new Uri(apiBaseUrl)
    };
});

// Register AuthApiClient
builder.Services.AddScoped<AuthApiClient>();

builder.Services.AddScoped<LoanProductsApiClient>();
builder.Services.AddScoped<BorrowersApiClient>();
builder.Services.AddScoped<LendersApiClient>();
builder.Services.AddScoped<LoanApplicationsApiClient>();
builder.Services.AddScoped<WizardApiClient>();
builder.Services.AddScoped<ReviewQueueApiClient>();
builder.Services.AddScoped<DashboardApiClient>();
builder.Services.AddScoped<FundingApiClient>();
builder.Services.AddScoped<PaymentsApiClient>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<ModalService>();
builder.Services.AddScoped<DrawerService>();
builder.Services.AddScoped<WizardStateService>();

await builder.Build().RunAsync();
