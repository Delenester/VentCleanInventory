using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Data;
using VentCleanInventory.Web.Data.Entities;

namespace VentCleanInventory.Web.Services;

public class BackgroundJobService(
    IServiceScopeFactory scopeFactory,
    ILogger<BackgroundJobService> logger) : BackgroundService
{
    private const int AutoConfirmDays = 7;
    private const int ExpiryWarnDays = 30;
    private DateTime _lastCycleRun = DateTime.MinValue;
    private DateTime _lastBackupRun = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("BackgroundJobService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;

                // Run auto-confirm + expiry every 6 hours (once per cycle)
                if ((now.Hour is 0 or 6 or 12 or 18) && (now - _lastCycleRun).TotalHours >= 5)
                {
                    _lastCycleRun = now;
                    await AutoConfirmContractsAsync(stoppingToken);
                    await WarnExpiringItemsAsync(stoppingToken);
                    await AutoCreateSupplyRequestsAsync(stoppingToken);
                }

                // Backup every night at 2:00-2:05 (once per day)
                if (now.Hour == 2 && now.Minute <= 5 && (now - _lastBackupRun).TotalHours >= 20)
                {
                    _lastBackupRun = now;
                    await RunBackupAsync(stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Background job error");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task AutoConfirmContractsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var cutoff = DateTime.UtcNow.AddDays(-AutoConfirmDays);
        var pending = await db.StockTransactions
            .Where(t => t.RequestStatusValue == RequestStatus.Approved && t.Date <= cutoff)
            .ToListAsync(ct);

        foreach (var req in pending)
        {
            req.RequestStatusValue = RequestStatus.ClientConfirmed;
            logger.LogInformation("Auto-confirmed contract for request #{Id}", req.Id);
        }

        if (pending.Count > 0)
        {
            await db.SaveChangesAsync(ct);
            await notificationService.NotifyRoleAsync(userManager, AppUserRole.Manager,
                "Автоподтверждение договора",
                $"Автоматически подтверждено {pending.Count} договоров (истекло {AutoConfirmDays} дней).",
                "/Manager/Request");
        }
    }

    private async Task WarnExpiringItemsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var warnDate = DateTime.UtcNow.AddDays(ExpiryWarnDays);
        var expiring = await db.InventoryItems.AsNoTracking()
            .Include(i => i.Nomenclature)
            .Where(i => i.ExpirationDate != null && i.ExpirationDate <= warnDate && i.ExpirationDate > DateTime.UtcNow)
            .OrderBy(i => i.ExpirationDate)
            .ToListAsync(ct);

        if (expiring.Count == 0) return;

        var groups = expiring.GroupBy(i => i.ExpirationDate!.Value.Date)
            .OrderBy(g => g.Key)
            .Select(g => $"{g.Key:dd.MM.yyyy}: {string.Join(", ", g.Select(i => i.Nomenclature.Name))}")
            .ToList();

        await notificationService.NotifyRoleAsync(userManager, AppUserRole.Manager,
            "Истекает срок годности",
            $"Следующие материалы истекают в ближайшие {ExpiryWarnDays} дней:\n{string.Join("\n", groups)}",
            "/Admin/Warehouses");
    }

    private async Task AutoCreateSupplyRequestsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var nomenclatures = await db.Nomenclatures.AsNoTracking()
            .Where(n => n.MinStockQuantity > 0 && n.PreferredSupplierId != null)
            .Include(n => n.PreferredSupplier)
            .ToListAsync(ct);

        if (nomenclatures.Count == 0) return;

        var now = DateTime.UtcNow;
        var created = 0;

        foreach (var nom in nomenclatures)
        {
            var totalStock = await db.StockBalances.AsNoTracking()
                .Where(b => b.InventoryItem.NomenclatureId == nom.Id)
                .SumAsync(b => (decimal?)b.Quantity, ct) ?? 0;

            if (totalStock >= nom.MinStockQuantity) continue;

            // Check if there's already a pending request for this nomenclature + supplier
            var alreadyRequested = await db.SupplyRequests.AsNoTracking()
                .Include(r => r.Items)
                .Where(r => r.OrganizationId == nom.PreferredSupplierId
                    && r.Status != SupplyRequestStatus.Completed
                    && r.Status != SupplyRequestStatus.Cancelled)
                .AnyAsync(r => r.Items.Any(i => i.NomenclatureId == nom.Id), ct);

            if (alreadyRequested) continue;

            var orderQty = nom.MinStockQuantity * 2 - totalStock;
            if (orderQty <= 0) continue;

            // Find or create a request for this supplier that is still New
            var pendingReq = await db.SupplyRequests
                .Include(r => r.Items)
                .Where(r => r.OrganizationId == nom.PreferredSupplierId
                    && r.Status == SupplyRequestStatus.New)
                .FirstOrDefaultAsync(ct);

            if (pendingReq == null)
            {
                var reqCount = await db.SupplyRequests.CountAsync(ct) + 1;
                pendingReq = new SupplyRequest
                {
                    Number = $"З-{now:yyyyMMdd}-{reqCount:D4}",
                    OrganizationId = nom.PreferredSupplierId!.Value,
                    CreatedAt = now,
                    Status = SupplyRequestStatus.New,
                };
                db.SupplyRequests.Add(pendingReq);
                await db.SaveChangesAsync(ct);
            }

            pendingReq.Items.Add(new SupplyRequestItem
            {
                SupplyRequestId = pendingReq.Id,
                NomenclatureId = nom.Id,
                Quantity = Math.Ceiling(orderQty),
            });
            created++;
        }

        if (created > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Auto-created {Count} supply request items", created);

            await notificationService.NotifyRoleAsync(userManager, AppUserRole.Admin,
                "Автосоздание запросов на поставку",
                $"Автоматически создано {created} позиций в запросах на поставку (низкий остаток).",
                "/Admin/SupplyRequests");
        }
    }

    private async Task RunBackupAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var backupService = scope.ServiceProvider.GetRequiredService<BackupService>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var backupDir = Path.Combine(Directory.GetCurrentDirectory(), "Backups");
        try
        {
            var path = await backupService.RunBackupAsync(backupDir, ct);
            logger.LogInformation("Scheduled backup created at {Path}", path);

            // Clean old backups (keep last 14)
            var files = Directory.GetFiles(backupDir, "*.bak")
                .OrderByDescending(f => f)
                .Skip(14)
                .ToList();
            foreach (var f in files) File.Delete(f);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Scheduled backup failed");
        }
    }
}
