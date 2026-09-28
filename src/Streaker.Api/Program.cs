using Scalar.AspNetCore;
using Streaker.Api.Middleware;
using Streaker.Application;
using Streaker.Infrastructure;
using Streaker.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddOpenApi(o => o.AddDocumentTransformer((doc, _, _) =>
{
    doc.Info.Title = "Streaker API";
    doc.Info.Version = "v1";
    doc.Info.Description = "Build habits, keep streaks. A clean-architecture .NET showcase.";
    return Task.CompletedTask;
}));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks();

var app = builder.Build();

await DatabaseInitializer.InitializeAsync(app.Services, seed: app.Environment.IsDevelopment());

app.UseExceptionHandler();
app.UseStatusCodePages();

// The web UI lives in wwwroot and is served by the API itself.
app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(o => o.WithTitle("Streaker API").WithTheme(ScalarTheme.Mars));
}

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
