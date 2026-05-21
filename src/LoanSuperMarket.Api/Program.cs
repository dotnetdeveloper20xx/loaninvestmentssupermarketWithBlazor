using System.Text;
using LoanSuperMarket.Api.Middleware;
using LoanSuperMarket.Application;
using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Infrastructure;
using LoanSuperMarket.Infrastructure.Identity;
using LoanSuperMarket.Shared.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Configuration binding
// ---------------------------------------------------------------------------
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName));

builder.Services.Configure<AccountSettings>(
    builder.Configuration.GetSection(AccountSettings.SectionName));

// ---------------------------------------------------------------------------
// CORS - origins configurable from appsettings (CorsSettings:AllowedOrigins)
// ---------------------------------------------------------------------------
const string BlazorCorsPolicy = "BlazorCorsPolicy";

var allowedOrigins = builder.Configuration
    .GetSection("CorsSettings:AllowedOrigins")
    .Get<string[]>() ?? ["https://localhost:5036", "http://localhost:5036"];

builder.Services.AddCors(options =>
{
    options.AddPolicy(BlazorCorsPolicy, policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ---------------------------------------------------------------------------
// JWT Bearer Authentication
// ---------------------------------------------------------------------------
var jwtSettings = builder.Configuration
    .GetSection(JwtSettings.SectionName)
    .Get<JwtSettings>() ?? new JwtSettings();

var signingKey = new SymmetricSecurityKey(
    Encoding.UTF8.GetBytes(jwtSettings.SecretKey));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,

        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,

        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30),

        ValidateIssuerSigningKey = true,
        IssuerSigningKey = signingKey,

        // Map standard claims
        NameClaimType = "email",
        RoleClaimType = "roles"
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception is SecurityTokenExpiredException)
            {
                context.Response.Headers.Append("X-Token-Expired", "true");
            }
            return Task.CompletedTask;
        }
    };
});

// ---------------------------------------------------------------------------
// Authorization policies
// ---------------------------------------------------------------------------
builder.Services.AddAuthorization(options =>
{
    AuthorizationPolicies.Configure(options);
});

// ---------------------------------------------------------------------------
// Controllers & Swagger
// ---------------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ---------------------------------------------------------------------------
// Application & Infrastructure DI (Identity, DbContexts, repositories, etc.)
// ---------------------------------------------------------------------------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Register ITokenService (JwtTokenService) - not registered in Infrastructure DI
builder.Services.AddScoped<ITokenService, JwtTokenService>();

// ---------------------------------------------------------------------------
// Build the app
// ---------------------------------------------------------------------------
var app = builder.Build();

// ---------------------------------------------------------------------------
// Seed Identity roles and default Admin account
// ---------------------------------------------------------------------------
await IdentitySeeder.SeedAsync(app.Services);

// ---------------------------------------------------------------------------
// Middleware pipeline (order matters)
// ---------------------------------------------------------------------------
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(BlazorCorsPolicy);

// Authentication MUST come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
