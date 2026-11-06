using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Domain.Constants;
using Domain.Entities.Auth;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.IntegrationTests.Support;

public sealed class AuthApiFactory : WebApplicationFactory<Program>
{
    public const string AdminUserName = "auth_admin";
    public const string OwnerUserName = "auth_owner";
    public const string Password = "Passw0rd!x";
    public const string AllowedOrigin = "https://localhost:5173";
    public static readonly Guid AdminId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
    public static readonly Guid OwnerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2");

    private readonly string _connectionString;
    private readonly string _signingKey;

    public AuthApiFactory()
    {
        _connectionString = TestDbFactory.ResolveConnectionString("esp32_security_auth_http_test");
        _signingKey = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
        builder.UseSetting("Auth:Jwt:SigningKey", _signingKey);
        builder.UseSetting("Auth:Jwt:Issuer", "esp32-security");
        builder.UseSetting("Auth:Jwt:Audience", "esp32-security-clients");
        builder.UseSetting("Auth:AccessTokenMinutes", "10");
        builder.UseSetting("Auth:RefreshTokenDays", "7");
        builder.UseSetting("Auth:Cors:AllowedOrigins:0", AllowedOrigin);
        builder.UseSetting("Auth:Cookie:Name", "esp32_rt");
        builder.UseSetting("Auth:Cookie:Path", "/api/auth");
        builder.UseSetting("Auth:Cookie:SameSite", "Lax");
        builder.UseSetting("Auth:CsrfCookieName", "esp32_csrf");
        builder.UseSetting("Auth:CsrfHeaderName", "X-CSRF-TOKEN");
    }

    public async Task InitializeDatabaseAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        await SystemRoleSeeder.EnsureAsync(db);
        await SystemRoleSeeder.EnsureAsync(db); // idempotent roles

        var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        await EnsureUserAsync(users, AdminId, AdminUserName, "Auth Admin", DbStatusCodes.RoleCodes.Admin);
        await EnsureUserAsync(users, OwnerId, OwnerUserName, "Auth Owner", DbStatusCodes.RoleCodes.Owner);
    }

    private static async Task EnsureUserAsync(
        UserManager<User> users,
        Guid id,
        string userName,
        string fullName,
        string role)
    {
        var existing = await users.FindByNameAsync(userName);
        if (existing is not null)
            return;

        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            Id = id,
            UserName = userName,
            FullName = fullName,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };
        var result = await users.CreateAsync(user, Password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(';', result.Errors.Select(e => e.Description)));
        await users.AddToRoleAsync(user, role);
    }

    public HttpClient CreateJsonClient()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    public static async Task<JsonElement> ReadDataAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("data");
    }
}
