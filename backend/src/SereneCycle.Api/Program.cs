using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using SereneCycle.Infrastructure;
using SereneCycle.Infrastructure.Options;
using SereneCycle.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Geliştirmede Gmail OAuth kurulumu zorunlu olmasın: e-postalar loga yazılır.
// "Gmail:UseRealSender=true" ile geliştirmede de gerçek gönderim açılabilir.
var useRealEmailSender = !builder.Environment.IsDevelopment()
    || builder.Configuration.GetValue<bool>("Gmail:UseRealSender");

builder.Services.AddInfrastructure(
    builder.Configuration, useRealEmailSender);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Enum'lar mobil tarafta okunabilir metin olarak görünsün.
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy =
            JsonNamingPolicy.CamelCase;
    });

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException(
        "Jwt yapılandırması eksik. appsettings veya ortam değişkenlerini kontrol et.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            // Varsayılan 5 dakikalık tolerans, kısa ömürlü token'larda
            // süre dolmasını anlamsızlaştırır.
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Serene Cycle API",
        Version = "v1",
        Description =
            "Menstrüel döngü takip uygulamasının REST API'si. "
            + "Bu içerik bilgilendirme amaçlıdır, tıbbi tavsiye değildir."
    });

    // Swagger UI'dan korumalı uçları denemek için "Authorize" düğmesi.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT erişim token'ını gir (Bearer öneki olmadan)."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Geliştirmede şema otomatik güncellenir ve örnek veri yazılır.
    // Üretimde migration'lar bilinçli olarak elle uygulanır.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    if (app.Configuration.GetValue("SeedDevelopmentData", true))
    {
        await DevelopmentDataSeeder.SeedAsync(scope.ServiceProvider);
    }
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();
