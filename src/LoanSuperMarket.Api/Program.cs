using LoanSuperMarket.Api.Middleware;
using LoanSuperMarket.Application;
using LoanSuperMarket.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

const string BlazorCorsPolicy = "BlazorCorsPolicy";

builder.Services.AddCors(options =>
{
    options.AddPolicy(BlazorCorsPolicy, policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5036",
                "https://localhost:5036")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(BlazorCorsPolicy);

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;