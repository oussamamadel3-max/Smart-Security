using API.Configuration;
using API.Extensions;
using API.Filters;
using API.Middleware;
using Application.Options;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

#region ================== CONFIGURE SERVICES ==================

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddScoped<NoStoreResultFilter>();

builder.Services
    .AddApplicationAndInfrastructure(builder.Configuration)
    .AddSwaggerDocumentation();

var authOptions = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AppCors", policy =>
    {
        if (authOptions.Cors.AllowedOrigins.Length > 0)
        {
            policy.WithOrigins(authOptions.Cors.AllowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

// OPS-T-03: distinguish process liveness from dependency readiness (DB).
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("process alive"), tags: ["live"])
    .AddDbContextCheck<AppDbContext>("postgres", tags: ["ready"]);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
    ForwardedHeadersSetup.Configure(options, builder.Configuration));

#endregion

var app = builder.Build();

#region ================== STARTUP DATA ==================

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (await db.Database.CanConnectAsync())
    {
        // Staging/lab only: apply EF migrations when explicitly enabled (never bake secrets).
        if (app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", false))
            await db.Database.MigrateAsync();

        await SystemRoleSeeder.EnsureAsync(db);
    }
}

#endregion

#region ================== MIDDLEWARE PIPELINE ==================

app.UseForwardedHeaders();
app.UseErrorHandling();
app.UseSwaggerDocumentation(app.Environment);
app.UseHttpsRedirection();
app.UseCors("AppCors");
app.UseAuthentication();
app.UseActiveSessionValidation();
app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
// Backward-compatible alias: process liveness only (not readiness).
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

#endregion

app.Run();

public partial class Program { }
