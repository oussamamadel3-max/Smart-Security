using API.Extensions;
using API.Filters;
using API.Middleware;
using Application.Options;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Seed;

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

builder.Services.AddHealthChecks();

#endregion

var app = builder.Build();

#region ================== STARTUP DATA ==================

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (await db.Database.CanConnectAsync())
        await SystemRoleSeeder.EnsureAsync(db);
}

#endregion

#region ================== MIDDLEWARE PIPELINE ==================

app.UseErrorHandling();
app.UseSwaggerDocumentation(app.Environment);
app.UseHttpsRedirection();
app.UseCors("AppCors");
app.UseAuthentication();
app.UseActiveSessionValidation();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

#endregion

app.Run();

public partial class Program { }
