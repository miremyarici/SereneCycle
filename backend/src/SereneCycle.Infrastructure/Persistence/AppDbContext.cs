using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SereneCycle.Domain.Entities;
using SereneCycle.Infrastructure.Identity;

namespace SereneCycle.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Cycle> Cycles => Set<Cycle>();
    public DbSet<DailyLog> DailyLogs => Set<DailyLog>();
    public DbSet<Symptom> Symptoms => Set<Symptom>();
    public DbSet<LogSymptom> LogSymptoms => Set<LogSymptom>();
    public DbSet<ContentItem> ContentItems => Set<ContentItem>();
    public DbSet<FoodPhaseScore> FoodPhaseScores => Set<FoodPhaseScore>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly);
    }
}
