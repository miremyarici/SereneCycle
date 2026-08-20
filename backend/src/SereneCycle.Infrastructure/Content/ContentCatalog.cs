using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SereneCycle.Domain.Cycles;
using SereneCycle.Domain.Entities;
using SereneCycle.Infrastructure.Persistence;

namespace SereneCycle.Infrastructure.Content;

/// <summary>
/// Katalogun süreç geneli kopyası. Katalog seed verisidir — çalışma anında
/// değişmez — bu yüzden bir kez okunur ve bellekte tutulur: birkaç yüz öğe
/// ≈ 60 KB, buna karşılık her öneri isteğinden bir veritabanı gidiş-dönüşü
/// düşer.
///
/// Kullanıcıya özel hiçbir şey saklanmaz; bu yüzden bellek kullanımı
/// kullanıcı sayısından bağımsızdır.
/// </summary>
public sealed class ContentCatalog(IServiceScopeFactory scopeFactory)
{
    private readonly SemaphoreSlim _loadGate = new(1, 1);

    private Snapshot? _snapshot;

    /// <summary>Bir faz + tür için bütün adaylar. Sıra kararlıdır (Id).</summary>
    public async ValueTask<IReadOnlyList<ContentItem>> GetCandidatesAsync(
        CyclePhase phase,
        ContentType type,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await EnsureLoadedAsync(cancellationToken);

        return snapshot.ByPhaseAndType.TryGetValue((phase, type), out var items)
            ? items
            : [];
    }

    /// <summary>Geri bildirim yolunun ihtiyacı: id → öğe, O(1).</summary>
    public async ValueTask<ContentItem?> FindAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await EnsureLoadedAsync(cancellationToken);

        return snapshot.ById.GetValueOrDefault(id);
    }

    private async ValueTask<Snapshot> EnsureLoadedAsync(
        CancellationToken cancellationToken)
    {
        // Yaygın yol: zaten yüklü, kilit hiç alınmaz.
        if (_snapshot is { } loaded)
        {
            return loaded;
        }

        await _loadGate.WaitAsync(cancellationToken);

        try
        {
            // İlk isteklerin aynı anda gelmesi mümkün; kilidi alan ikinci
            // istek yeniden okumamalı.
            return _snapshot ??= await LoadAsync(cancellationToken);
        }
        finally
        {
            _loadGate.Release();
        }
    }

    private async Task<Snapshot> LoadAsync(CancellationToken cancellationToken)
    {
        // Katalog tekil (singleton) olarak yaşar, DbContext ise istek
        // kapsamlı: okuma için kendi kapsamı açılır.
        using var scope = scopeFactory.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var items = await db.ContentItems
            .AsNoTracking()
            .OrderBy(item => item.Id)
            .ToListAsync(cancellationToken);

        return new Snapshot(
            ByPhaseAndType: items
                .GroupBy(item => (item.Phase, item.Type))
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<ContentItem>)[.. group]),
            ById: items.ToDictionary(item => item.Id));
    }

    private sealed record Snapshot(
        IReadOnlyDictionary<(CyclePhase Phase, ContentType Type),
            IReadOnlyList<ContentItem>> ByPhaseAndType,
        IReadOnlyDictionary<int, ContentItem> ById);
}
