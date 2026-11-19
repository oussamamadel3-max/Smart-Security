using System.Text;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Options;
using Application.Services;
using Application.Services.AdminServices;
using Application.Services.AuthServices;
using Application.Services.MqttServices;
using Application.Services.NotificationServices;
using Application.Services.OwnerServices;
using Application.Services.PushTokenServices;
using Domain.Constants;
using Domain.Entities.Auth;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Infrastructure.Security;
using Infrastructure.Services;
using Infrastructure.Services.Fcm;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure;

/// <summary>
/// Registers Infrastructure and Application services.
/// </summary>
public static class InfrastructureServiceRegistration
{
    public static IServiceCollection ConfigureInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is missing. " +
                "Configure it via User Secrets or environment variable ConnectionStrings__DefaultConnection.");
        }

        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.Configure<MqttInboundOptions>(configuration.GetSection(MqttInboundOptions.SectionName));
        services.Configure<MqttOutboxOptions>(configuration.GetSection(MqttOutboxOptions.SectionName));
        services.Configure<MqttLivePublishOptions>(configuration.GetSection(MqttLivePublishOptions.SectionName));
        services.Configure<FcmOptions>(configuration.GetSection(FcmOptions.SectionName));
        var authOptions = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
        if (string.IsNullOrWhiteSpace(authOptions.Jwt.SigningKey) || authOptions.Jwt.SigningKey.Length < 32)
        {
            throw new InvalidOperationException(
                "Auth:Jwt:SigningKey is missing or shorter than 32 characters. Configure via User Secrets / environment.");
        }

        var fcmOptions = configuration.GetSection(FcmOptions.SectionName).Get<FcmOptions>() ?? new FcmOptions();
        RegisterFcmProvider(services, fcmOptions);

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services
            .AddIdentityCore<User>(options =>
            {
                options.User.RequireUniqueEmail = false;
                options.Password.RequiredLength = 8;
            })
            .AddRoles<Role>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptions.Jwt.SigningKey));
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = authOptions.Jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = authOptions.Jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var tokenUse = context.Principal?.FindFirst("token_use")?.Value;
                        if (!string.Equals(tokenUse, AuthStatusCodes.TokenUse.Access, StringComparison.Ordinal))
                            context.Fail("Invalid token type.");
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

        var mapsterConfig = TypeAdapterConfig.GlobalSettings;
        mapsterConfig.Scan(typeof(Application.Mappings.AuthMappingProfile).Assembly);
        services.AddSingleton(mapsterConfig);
        services.AddScoped<IMapper, ServiceMapper>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAuthSessionRepository, AuthSessionRepository>();
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<IDeviceCommandRepository, DeviceCommandRepository>();
        services.AddScoped<IAlarmEventRepository, AlarmEventRepository>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IPermissionGate, PermissionGate>();
        services.AddScoped<IOwnerAdminService, OwnerAdminService>();
        services.AddScoped<IDeviceAdminService, DeviceAdminService>();
        services.AddScoped<IAdminAlertService, AdminAlertService>();
        services.AddScoped<IDeviceOwnershipService, DeviceOwnershipService>();
        services.AddScoped<IOwnerDeviceQueryService, OwnerDeviceQueryService>();
        services.AddScoped<IOwnerDeviceCommandService, OwnerDeviceCommandService>();
        services.AddScoped<IOwnerDeviceHistoryService, OwnerDeviceHistoryService>();
        services.AddScoped<IDeviceInboundTelemetryService, DeviceInboundTelemetryService>();
        services.AddScoped<IAlarmEventIngestService, AlarmEventIngestService>();
        services.AddScoped<INotificationRecipientGuard, NotificationRecipientGuard>();
        services.AddScoped<INotificationDispatchService, NotificationDispatchService>();
        services.AddScoped<IPushTokenService, PushTokenService>();
        services.AddScoped<IPushTokenCrypto, PushTokenCrypto>();
        services.AddScoped<IPushTokenRepository, PushTokenRepository>();
        services.AddScoped<INotificationDispatchRepository, NotificationDispatchRepository>();
        services.AddScoped<IMqttInboundDispatcher, MqttInboundDispatcher>();
        services.AddScoped<IOutboxDispatchService, OutboxDispatchService>();
        services.AddSingleton<IMqttOutboundPublisher>(sp =>
        {
            var live = sp.GetRequiredService<IOptions<MqttLivePublishOptions>>().Value;
            if (!live.LivePublishEnabled)
                return new NoOpMqttOutboundPublisher(sp.GetRequiredService<ILogger<NoOpMqttOutboundPublisher>>());

            return new MqttNetLivePublisher(
                sp.GetRequiredService<IOptions<MqttLivePublishOptions>>(),
                sp.GetRequiredService<ILogger<MqttNetLivePublisher>>());
        });
        services.AddScoped<IDeviceCredentialHasher, DeviceCredentialHasher>();
        services.AddScoped<IDeviceBrokerAccessService, NoOpDeviceBrokerAccessService>();
        services.AddScoped<IOwnerLookup, OwnerLookup>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddHostedService<MqttInboundHostedService>();
        services.AddHostedService<MqttOutboxHostedService>();
        services.AddHostedService<FcmNotificationHostedService>();

        return services;
    }

    private static void RegisterFcmProvider(IServiceCollection services, FcmOptions fcmOptions)
    {
        var provider = (fcmOptions.Provider ?? FcmOptions.ProviderFake).Trim();
        if (string.Equals(provider, FcmOptions.ProviderFirebase, StringComparison.OrdinalIgnoreCase))
        {
            // Real send path — refuse startup when ADC cannot be resolved.
            // Absence of GOOGLE_APPLICATION_CREDENTIALS must not affect Fake; it must block Firebase.
            // Never log credential file paths or JSON contents. Never silently fall back to Fake.
            EnsureFirebaseProjectIdConfigured(fcmOptions);
            EnsureCredentialFileMatchesFirebaseProject(fcmOptions);

            if (!TryProbeApplicationDefaultCredentials())
            {
                throw new InvalidOperationException(
                    "Fcm:Provider=Firebase requires Application Default Credentials " +
                    "(set GOOGLE_APPLICATION_CREDENTIALS to a service-account file outside Git, " +
                    "or configure ADC another supported way). " +
                    "Keep Fcm:Provider=Fake for local/test runs without Firebase credentials.");
            }

            services.AddSingleton<IFcmProvider, FirebaseFcmProvider>();
            return;
        }

        // Default and all automated tests: FakeFcmProvider (no network, no ADC required).
        services.AddSingleton<FakeFcmProvider>();
        services.AddSingleton<IFcmProvider>(sp => sp.GetRequiredService<FakeFcmProvider>());
    }

    /// <summary>
    /// Firebase registration requires an explicit expected project id (dev/staging/prod isolation).
    /// </summary>
    private static void EnsureFirebaseProjectIdConfigured(FcmOptions fcmOptions)
    {
        if (string.IsNullOrWhiteSpace(fcmOptions.FirebaseProjectId))
        {
            throw new InvalidOperationException(
                "Fcm:Provider=Firebase requires Fcm:FirebaseProjectId (e.g. smart-security-dev-28642). " +
                "Set it via User Secrets / environment for the target environment only. " +
                "Keep Fcm:Provider=Fake when no Firebase project is selected.");
        }
    }

    /// <summary>
    /// When GOOGLE_APPLICATION_CREDENTIALS points at a service-account JSON file, compare its
    /// project_id to Fcm:FirebaseProjectId. Reads only the project_id field — never logs secrets.
    /// </summary>
    private static void EnsureCredentialFileMatchesFirebaseProject(FcmOptions fcmOptions)
    {
        var expected = fcmOptions.FirebaseProjectId.Trim();
        var path = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        if (string.IsNullOrWhiteSpace(path))
            return;

        if (!File.Exists(path))
        {
            throw new InvalidOperationException(
                "Fcm:Provider=Firebase: GOOGLE_APPLICATION_CREDENTIALS path does not exist. " +
                "Fix the path or keep Fcm:Provider=Fake.");
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
            if (!doc.RootElement.TryGetProperty("project_id", out var projectIdElement))
            {
                throw new InvalidOperationException(
                    "Fcm:Provider=Firebase: credential file is missing project_id. " +
                    "Keep Fcm:Provider=Fake until a valid service-account file is configured.");
            }

            var actual = projectIdElement.GetString()?.Trim() ?? string.Empty;
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Fcm:Provider=Firebase: credential project_id does not match Fcm:FirebaseProjectId. " +
                    "Refusing cross-environment credentials. Keep Fcm:Provider=Fake or use the matching service account.");
            }
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Fcm:Provider=Firebase: unable to read project_id from GOOGLE_APPLICATION_CREDENTIALS file. " +
                "Keep Fcm:Provider=Fake until credentials are valid.",
                ex);
        }
    }

    /// <summary>
    /// Probes ADC availability without logging secrets or file contents.
    /// </summary>
    private static bool TryProbeApplicationDefaultCredentials()
    {
        try
        {
            _ = Google.Apis.Auth.OAuth2.GoogleCredential.GetApplicationDefault();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
