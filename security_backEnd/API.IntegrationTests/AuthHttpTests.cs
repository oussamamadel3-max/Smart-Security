using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using API.IntegrationTests.Support;
using Domain.Constants;
using Domain.Entities.Devices;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.IntegrationTests;

[Collection(nameof(AuthHttpCollection))]
public sealed class AuthHttpTests : IAsyncLifetime
{
    private readonly AuthApiFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();
        _client = _factory.CreateJsonClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Login_Succeeds_For_Active_User_Body_Channel()
    {
        var res = await LoginBodyAsync(AuthApiFactory.AdminUserName);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Contains("no-store", res.Headers.CacheControl?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);

        var data = await AuthApiFactory.ReadDataAsync(res);
        Assert.False(string.IsNullOrWhiteSpace(data.GetProperty("accessToken").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(data.GetProperty("refreshToken").GetString()));
        Assert.Equal(AuthStatusCodes.RefreshChannel.Body, data.GetProperty("refreshChannel").GetString());
    }

    [Fact]
    public async Task Login_Fails_For_Bad_Password()
    {
        var res = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            userName = AuthApiFactory.AdminUserName,
            password = "wrong-password",
            refreshDelivery = "body"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Refresh_Rotates_And_Rejects_Old_Token_Replay()
    {
        var login = await LoginBodyAsync(AuthApiFactory.OwnerUserName);
        var data = await AuthApiFactory.ReadDataAsync(login);
        var refresh1 = data.GetProperty("refreshToken").GetString()!;

        var refreshRes = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = refresh1 });
        Assert.Equal(HttpStatusCode.OK, refreshRes.StatusCode);
        var refreshData = await AuthApiFactory.ReadDataAsync(refreshRes);
        var refresh2 = refreshData.GetProperty("refreshToken").GetString()!;
        Assert.NotEqual(refresh1, refresh2);

        var replay = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = refresh1 });
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);

        // Family revoked — current token also dead after reuse detection.
        var afterReuse = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = refresh2 });
        Assert.Equal(HttpStatusCode.Unauthorized, afterReuse.StatusCode);
    }

    [Fact]
    public async Task Refresh_Three_Generations_Then_Replay_R1_Revokes_Family()
    {
        var login = await LoginBodyAsync(AuthApiFactory.OwnerUserName);
        var r1 = (await AuthApiFactory.ReadDataAsync(login)).GetProperty("refreshToken").GetString()!;

        var gen2 = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = r1 });
        Assert.Equal(HttpStatusCode.OK, gen2.StatusCode);
        var r2 = (await AuthApiFactory.ReadDataAsync(gen2)).GetProperty("refreshToken").GetString()!;
        Assert.NotEqual(r1, r2);

        var gen3 = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = r2 });
        Assert.Equal(HttpStatusCode.OK, gen3.StatusCode);
        var r3 = (await AuthApiFactory.ReadDataAsync(gen3)).GetProperty("refreshToken").GetString()!;
        Assert.NotEqual(r2, r3);

        var replayR1 = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = r1 });
        Assert.Equal(HttpStatusCode.Unauthorized, replayR1.StatusCode);

        var currentAfterReplay = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = r3 });
        Assert.Equal(HttpStatusCode.Unauthorized, currentAfterReplay.StatusCode);
    }

    [Fact]
    public async Task Concurrent_Refresh_Only_One_Succeeds()
    {
        var login = await LoginBodyAsync(AuthApiFactory.OwnerUserName);
        var refresh = (await AuthApiFactory.ReadDataAsync(login)).GetProperty("refreshToken").GetString()!;

        var t1 = _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = refresh });
        var t2 = _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = refresh });
        var results = await Task.WhenAll(t1, t2);

        var ok = results.Count(r => r.StatusCode == HttpStatusCode.OK);
        var unauthorized = results.Count(r => r.StatusCode == HttpStatusCode.Unauthorized);
        Assert.Equal(1, ok);
        Assert.Equal(1, unauthorized);
    }

    [Fact]
    public async Task Logout_And_LogoutAll_Invalidate_Sessions()
    {
        var login1 = await LoginBodyAsync(AuthApiFactory.OwnerUserName);
        var d1 = await AuthApiFactory.ReadDataAsync(login1);
        var access1 = d1.GetProperty("accessToken").GetString()!;
        var refresh1 = d1.GetProperty("refreshToken").GetString()!;

        var login2 = await LoginBodyAsync(AuthApiFactory.OwnerUserName);
        var d2 = await AuthApiFactory.ReadDataAsync(login2);
        var access2 = d2.GetProperty("accessToken").GetString()!;
        var refresh2 = d2.GetProperty("refreshToken").GetString()!;

        using var c1 = _factory.CreateJsonClient();
        c1.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access1);
        var logout = await c1.PostAsJsonAsync("/api/auth/logout", new { refreshToken = refresh1 });
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);

        var meAfterLogout = await c1.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meAfterLogout.StatusCode);

        using var c2 = _factory.CreateJsonClient();
        c2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access2);
        var logoutAll = await c2.PostAsync("/api/auth/logout-all", null);
        Assert.Equal(HttpStatusCode.OK, logoutAll.StatusCode);

        var refreshAfter = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = refresh2 });
        Assert.Equal(HttpStatusCode.Unauthorized, refreshAfter.StatusCode);
    }

    [Fact]
    public async Task Disabled_User_Access_Is_Rejected_Immediately()
    {
        var ownerLogin = await LoginBodyAsync(AuthApiFactory.OwnerUserName);
        var ownerAccess = (await AuthApiFactory.ReadDataAsync(ownerLogin)).GetProperty("accessToken").GetString()!;

        var adminLogin = await LoginBodyAsync(AuthApiFactory.AdminUserName);
        var adminAccess = (await AuthApiFactory.ReadDataAsync(adminLogin)).GetProperty("accessToken").GetString()!;

        using var adminClient = _factory.CreateJsonClient();
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminAccess);
        var disable = await adminClient.PostAsync($"/api/admin/owners/{AuthApiFactory.OwnerId}/disable", null);
        Assert.Equal(HttpStatusCode.OK, disable.StatusCode);

        using var ownerClient = _factory.CreateJsonClient();
        ownerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerAccess);
        var me = await ownerClient.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    [Fact]
    public async Task Archived_User_Is_Rejected_And_Archive_Blocked_When_Device_Linked()
    {
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTimeOffset.UtcNow;
            var deviceId = Guid.NewGuid();
            db.Devices.Add(new Device
            {
                Id = deviceId,
                HardwareUid = "HW-ARCHIVE-TEST",
                DisplayName = "Archive Guard",
                LifecycleStatus = DbStatusCodes.DeviceLifecycle.Registered,
                CurrentOwnershipVersion = 1,
                CreatedByUserId = AuthApiFactory.AdminId,
                CreatedAt = now,
                UpdatedAt = now
            });
            db.DeviceOwnerships.Add(new DeviceOwnership
            {
                Id = Guid.NewGuid(),
                DeviceId = deviceId,
                OwnerUserId = AuthApiFactory.OwnerId,
                Version = 1,
                Status = DbStatusCodes.Ownership.Active,
                StartedAt = now,
                ActivatedAt = now,
                CreatedByUserId = AuthApiFactory.AdminId
            });
            await db.SaveChangesAsync();
        }

        var adminLogin = await LoginBodyAsync(AuthApiFactory.AdminUserName);
        var adminAccess = (await AuthApiFactory.ReadDataAsync(adminLogin)).GetProperty("accessToken").GetString()!;
        using var adminClient = _factory.CreateJsonClient();
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminAccess);

        var blocked = await adminClient.PostAsync($"/api/admin/owners/{AuthApiFactory.OwnerId}/archive", null);
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);

        // Remove ownership then archive.
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var open = await db.DeviceOwnerships.Where(o => o.OwnerUserId == AuthApiFactory.OwnerId).ToListAsync();
            foreach (var o in open)
            {
                o.Status = DbStatusCodes.Ownership.Ended;
                o.EndedAt = DateTimeOffset.UtcNow;
                o.EndedByUserId = AuthApiFactory.AdminId;
            }
            await db.SaveChangesAsync();
        }

        var ownerLogin = await LoginBodyAsync(AuthApiFactory.OwnerUserName);
        var ownerAccess = (await AuthApiFactory.ReadDataAsync(ownerLogin)).GetProperty("accessToken").GetString()!;

        var archived = await adminClient.PostAsync($"/api/admin/owners/{AuthApiFactory.OwnerId}/archive", null);
        Assert.Equal(HttpStatusCode.OK, archived.StatusCode);

        using var ownerClient = _factory.CreateJsonClient();
        ownerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerAccess);
        var meAfterArchive = await ownerClient.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meAfterArchive.StatusCode);

        var loginArchived = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            userName = AuthApiFactory.OwnerUserName,
            password = AuthApiFactory.Password,
            refreshDelivery = "body"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, loginArchived.StatusCode);
    }

    [Fact]
    public async Task Access_Token_Rejected_Immediately_After_Archive()
    {
        var ownerLogin = await LoginBodyAsync(AuthApiFactory.OwnerUserName);
        var ownerAccess = (await AuthApiFactory.ReadDataAsync(ownerLogin)).GetProperty("accessToken").GetString()!;

        var adminLogin = await LoginBodyAsync(AuthApiFactory.AdminUserName);
        var adminAccess = (await AuthApiFactory.ReadDataAsync(adminLogin)).GetProperty("accessToken").GetString()!;
        using var adminClient = _factory.CreateJsonClient();
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminAccess);

        var archived = await adminClient.PostAsync($"/api/admin/owners/{AuthApiFactory.OwnerId}/archive", null);
        Assert.Equal(HttpStatusCode.OK, archived.StatusCode);

        using var ownerClient = _factory.CreateJsonClient();
        ownerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerAccess);
        var me = await ownerClient.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    [Fact]
    public async Task Owner_Lifecycle_Writes_Audit_Logs_Without_Secrets()
    {
        var adminLogin = await LoginBodyAsync(AuthApiFactory.AdminUserName);
        var adminAccess = (await AuthApiFactory.ReadDataAsync(adminLogin)).GetProperty("accessToken").GetString()!;
        using var adminClient = _factory.CreateJsonClient();
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminAccess);

        var create = await adminClient.PostAsJsonAsync("/api/admin/owners", new
        {
            userName = "audit_owner_1",
            fullName = "Audit Owner",
            password = AuthApiFactory.Password,
            email = "audit@example.com"
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await AuthApiFactory.ReadDataAsync(create);
        var ownerId = created.GetProperty("id").GetGuid();

        var update = await adminClient.PutAsJsonAsync($"/api/admin/owners/{ownerId}", new
        {
            fullName = "Audit Owner Updated",
            email = "audit2@example.com"
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var disable = await adminClient.PostAsync($"/api/admin/owners/{ownerId}/disable", null);
        Assert.Equal(HttpStatusCode.OK, disable.StatusCode);

        var archive = await adminClient.PostAsync($"/api/admin/owners/{ownerId}/archive", null);
        Assert.Equal(HttpStatusCode.OK, archive.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var actions = await db.AuditLogs
            .Where(a => a.TargetId == ownerId)
            .OrderBy(a => a.OccurredAt)
            .Select(a => new { a.Action, a.Result, a.ActorUserId, a.Details })
            .ToListAsync();

        Assert.Equal(4, actions.Count);
        Assert.Equal(AuditActions.OwnerCreated, actions[0].Action);
        Assert.Equal(AuditActions.OwnerUpdated, actions[1].Action);
        Assert.Equal(AuditActions.OwnerDisabled, actions[2].Action);
        Assert.Equal(AuditActions.OwnerArchived, actions[3].Action);
        Assert.All(actions, a =>
        {
            Assert.Equal(DbStatusCodes.AuditResult.Success, a.Result);
            Assert.Equal(AuthApiFactory.AdminId, a.ActorUserId);
            Assert.DoesNotContain(AuthApiFactory.Password, a.Details ?? "", StringComparison.Ordinal);
            Assert.DoesNotContain("password", a.Details ?? "", StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("refresh", a.Details ?? "", StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task Owner_Is_Forbidden_On_Admin_Owner_Apis()
    {
        var ownerLogin = await LoginBodyAsync(AuthApiFactory.OwnerUserName);
        var access = (await AuthApiFactory.ReadDataAsync(ownerLogin)).GetProperty("accessToken").GetString()!;
        using var client = _factory.CreateJsonClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access);

        var res = await client.PostAsJsonAsync("/api/admin/owners", new
        {
            userName = "x_owner",
            fullName = "X",
            password = AuthApiFactory.Password
        });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Me_Without_Token_Is_401()
    {
        var res = await _client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Cookie_Login_Requires_Origin_And_Sets_HttpOnly_Secure_Cookie()
    {
        using var client = _factory.CreateJsonClient();
        client.DefaultRequestHeaders.Add("Origin", AuthApiFactory.AllowedOrigin);
        var res = await client.PostAsJsonAsync("/api/auth/login", new
        {
            userName = AuthApiFactory.AdminUserName,
            password = AuthApiFactory.Password,
            refreshDelivery = "cookie"
        });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        Assert.True(res.Headers.TryGetValues("Set-Cookie", out var cookies));
        var cookieList = cookies.ToList();
        var joined = string.Join(' ', cookieList);
        Assert.Contains("esp32_rt=", joined, StringComparison.Ordinal);
        Assert.Contains("esp32_csrf=", joined, StringComparison.Ordinal);
        Assert.Contains("httponly", joined, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", joined, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", joined, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/auth", joined, StringComparison.OrdinalIgnoreCase);

        var csrfLine = cookieList.First(c => c.StartsWith("esp32_csrf=", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("httponly", csrfLine, StringComparison.OrdinalIgnoreCase);

        var data = await AuthApiFactory.ReadDataAsync(res);
        Assert.True(data.TryGetProperty("refreshToken", out var rt) && rt.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
                    || string.IsNullOrEmpty(rt.GetString()));
        Assert.False(data.TryGetProperty("sessionId", out _));
    }

    [Fact]
    public async Task Cookie_Refresh_Succeeds_With_Valid_Signed_Csrf()
    {
        var login = await CookieLoginAsync(AuthApiFactory.AdminUserName);
        using var req = BuildCookieRefreshRequest(login.RefreshCookie, login.CsrfToken, AuthApiFactory.AllowedOrigin);
        var refresh = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        var data = await AuthApiFactory.ReadDataAsync(refresh);
        Assert.False(string.IsNullOrWhiteSpace(data.GetProperty("accessToken").GetString()));
        Assert.Equal(AuthStatusCodes.RefreshChannel.Cookie, data.GetProperty("refreshChannel").GetString());
    }

    [Fact]
    public async Task Cookie_Refresh_Rejects_Wrong_Csrf_Token()
    {
        var login = await CookieLoginAsync(AuthApiFactory.AdminUserName);
        using var req = BuildCookieRefreshRequest(login.RefreshCookie, "wrong-csrf-token", AuthApiFactory.AllowedOrigin);
        var refresh = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Cookie_Refresh_Rejects_Disallowed_Origin()
    {
        var login = await CookieLoginAsync(AuthApiFactory.AdminUserName);
        using var req = BuildCookieRefreshRequest(login.RefreshCookie, login.CsrfToken, "https://evil.example");
        var refresh = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, refresh.StatusCode);
    }

    [Fact]
    public async Task Cookie_Refresh_Rejects_Injected_Unsigned_Csrf_Cookie()
    {
        var login = await CookieLoginAsync(AuthApiFactory.AdminUserName);
        // Client-controlled matching cookie+header without valid HMAC / session binding.
        const string forged =
            "00000000000000000000000000000000.deadbeefdeadbeefdeadbeefdeadbeef.9999999999.0000000000000000000000000000000000000000000000000000000000000000";
        using var req = BuildCookieRefreshRequest(login.RefreshCookie, forged, AuthApiFactory.AllowedOrigin);
        var refresh = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Cookie_Refresh_Without_Csrf_Is_Rejected()
    {
        var login = await CookieLoginAsync(AuthApiFactory.AdminUserName);
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        req.Headers.Add("Origin", AuthApiFactory.AllowedOrigin);
        req.Headers.TryAddWithoutValidation("Cookie", $"esp32_rt={login.RefreshCookie}; esp32_csrf={login.CsrfToken}");
        req.Content = JsonContent.Create(new { });
        var refresh = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Ambiguous_Cookie_And_Body_Refresh_Is_Rejected()
    {
        var cookieLogin = await CookieLoginAsync(AuthApiFactory.AdminUserName);
        var bodyLogin = await LoginBodyAsync(AuthApiFactory.OwnerUserName);
        var bodyRefresh = (await AuthApiFactory.ReadDataAsync(bodyLogin)).GetProperty("refreshToken").GetString()!;

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        req.Headers.Add("Origin", AuthApiFactory.AllowedOrigin);
        req.Headers.TryAddWithoutValidation("Cookie", $"esp32_rt={cookieLogin.RefreshCookie}");
        req.Content = JsonContent.Create(new { refreshToken = bodyRefresh });
        var ambiguous = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, ambiguous.StatusCode);
    }

    private async Task<(string RefreshCookie, string CsrfToken)> CookieLoginAsync(string userName)
    {
        using var client = _factory.CreateJsonClient();
        client.DefaultRequestHeaders.Add("Origin", AuthApiFactory.AllowedOrigin);
        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            userName,
            password = AuthApiFactory.Password,
            refreshDelivery = "cookie"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.True(login.Headers.TryGetValues("Set-Cookie", out var setCookies));
        var pairs = setCookies.Select(ParseCookiePair).ToList();
        var refresh = pairs.First(p => p.Name == "esp32_rt").Value;
        var csrf = pairs.First(p => p.Name == "esp32_csrf").Value;
        Assert.False(string.IsNullOrWhiteSpace(refresh));
        Assert.False(string.IsNullOrWhiteSpace(csrf));
        return (refresh, csrf);
    }

    private static HttpRequestMessage BuildCookieRefreshRequest(string refreshCookie, string csrfToken, string origin)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        req.Headers.Add("Origin", origin);
        req.Headers.TryAddWithoutValidation("Cookie", $"esp32_rt={refreshCookie}; esp32_csrf={csrfToken}");
        req.Headers.TryAddWithoutValidation("X-CSRF-TOKEN", csrfToken);
        req.Content = JsonContent.Create(new { });
        return req;
    }

    private static (string Name, string Value) ParseCookiePair(string setCookie)
    {
        var first = setCookie.Split(';', 2)[0];
        var parts = first.Split('=', 2);
        return (parts[0].Trim(), parts.Length > 1 ? parts[1].Trim() : string.Empty);
    }

    [Fact]
    public async Task Role_Seed_Is_Not_Duplicated()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await db.Roles.CountAsync();
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task Responses_Do_Not_Echo_Raw_Secrets()
    {
        var login = await LoginBodyAsync(AuthApiFactory.AdminUserName);
        var text = await login.Content.ReadAsStringAsync();
        Assert.DoesNotContain("SigningKey", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password=", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(AuthApiFactory.Password, text, StringComparison.Ordinal);
    }

    private async Task<HttpResponseMessage> LoginBodyAsync(string userName)
    {
        return await _client.PostAsJsonAsync("/api/auth/login", new
        {
            userName,
            password = AuthApiFactory.Password,
            refreshDelivery = "body"
        });
    }
}

[CollectionDefinition(nameof(AuthHttpCollection), DisableParallelization = true)]
public sealed class AuthHttpCollection;


