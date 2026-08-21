using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SereneCycle.Application.Abstractions;
using SereneCycle.Application.Auth;
using SereneCycle.Application.Content;
using SereneCycle.Application.Logs;
using SereneCycle.Application.Phases;
using SereneCycle.Application.Privacy;
using SereneCycle.Application.Profiles;
using SereneCycle.Application.Risk;
using SereneCycle.Infrastructure.Content;
using SereneCycle.Infrastructure.Email;
using SereneCycle.Infrastructure.Identity;
using SereneCycle.Infrastructure.Options;
using SereneCycle.Infrastructure.Persistence;
using SereneCycle.Infrastructure.Security;
using SereneCycle.Infrastructure.Services;

namespace SereneCycle.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool useRealEmailSender)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("Postgres")));

        services.AddIdentityCore<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                // Kilitleme, kaba kuvvet denemelerine karşı.
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        // Ayarlar başlangıçta doğrulanır: eksik yapılandırmayı ilk istekte
        // değil, uygulama ayağa kalkarken fark edelim.
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IAccountDataService, AccountDataService>();
        services.AddScoped<IPhaseService, PhaseService>();
        // Katalog seed verisidir ve çalışma anında değişmez: süreç başına
        // tek kopya yeter, üstelik her öneri isteğinden bir sorgu düşer.
        services.AddSingleton<ContentCatalog>();
        services.AddScoped<IContentService, ContentService>();
        services.AddScoped<ITasteProfileService, TasteProfileService>();
        services.AddScoped<ILogService, LogService>();
        services.AddScoped<IRiskService, RiskService>();
        services.AddScoped<CycleRegistrar>();
        services.AddScoped<RiskSummaryUpdater>();

        if (useRealEmailSender)
        {
            services.AddOptions<GmailOptions>()
                .Bind(configuration.GetSection(GmailOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // Gmail istemcisi token yenilemeyi kendi yönetir; tek örnek yeter.
            services.AddSingleton<IEmailSender, GmailApiEmailSender>();
        }
        else
        {
            // Gmail OAuth kurulumu olmadan da akış denenebilsin diye.
            services.AddSingleton<IEmailSender, LoggingEmailSender>();
        }

        return services;
    }
}
