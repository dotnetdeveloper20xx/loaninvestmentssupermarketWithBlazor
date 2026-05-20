using LoanSuperMarket.Blazor;
using LoanSuperMarket.Blazor.Services.ApiClients;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using LoanSuperMarket.Blazor.Services.Notifications;


var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl configuration is missing.");

builder.Services.AddScoped(_ =>
{
    return new HttpClient
    {
        BaseAddress = new Uri(apiBaseUrl)
    };
});

builder.Services.AddScoped<LoanProductsApiClient>();
builder.Services.AddScoped<BorrowersApiClient>();
builder.Services.AddScoped<LendersApiClient>();
builder.Services.AddScoped<LoanApplicationsApiClient>();
builder.Services.AddScoped<DashboardApiClient>();
builder.Services.AddScoped<ToastService>();

await builder.Build().RunAsync();