using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Data;
using VentCleanInventory.Web.Data.Entities;

namespace VentCleanInventory.Web.Services;

public class StockService(ApplicationDbContext db, ILogger<StockService> logger)
{
    public async Task<int> ApplyReceiptAsync(
        int toWarehouseId,
        int? organizationId,
        string userId,
        DateTime date,
        IReadOnlyList<(int inventoryItemId, decimal quantity, decimal? unitPrice)> lines,
        string? note = null,
        CancellationToken ct = default)
    {
        if (lines.Count == 0) throw new ArgumentException("Receipt must have at least one line.", nameof(lines));

        var tx = new StockTransaction
        {
            TransactionType = TransactionType.Receipt,
            FromWarehouseId = null,
            ToWarehouseId = toWarehouseId,
            OrganizationId = organizationId,
            UserId = userId,
            Date = date,
            Note = note,
        };
        tx.SetItems(lines.Select(l => new TransactionItemDto
        {
            InventoryItemId = l.inventoryItemId,
            Quantity = l.quantity,
            UnitPrice = l.unitPrice,
        }).ToList());

        await ApplyTransactionAsync(tx, ct);
        return tx.Id;
    }

    public async Task<int> ApplyWorkConsumptionAsync(
        int fromWarehouseId,
        int workObjectId,
        int workLogId,
        string userId,
        DateTime date,
        IReadOnlyList<(int inventoryItemId, decimal quantity, decimal? unitPrice)> lines,
        string? note = null,
        CancellationToken ct = default)
    {
        if (lines.Count == 0) throw new ArgumentException("Consumption must have at least one line.", nameof(lines));

        var tx = new StockTransaction
        {
            TransactionType = TransactionType.WriteOff,
            FromWarehouseId = fromWarehouseId,
            ToWarehouseId = null,
            WriteOffReason = WriteOffReason.Wear,
            UserId = userId,
            Date = date,
            Note = note,
            WorkObjectId = workObjectId,
            WorkLogId = workLogId,
        };
        tx.SetItems(lines.Select(l => new TransactionItemDto
        {
            InventoryItemId = l.inventoryItemId,
            Quantity = l.quantity,
            UnitPrice = l.unitPrice,
        }).ToList());

        await ApplyTransactionAsync(tx, ct);
        return tx.Id;
    }

    public async Task<int> ApplyMovementAsync(
        TransactionType type,
        int? fromWarehouseId,
        int? toWarehouseId,
        int? requestId,
        string userId,
        DateTime date,
        IReadOnlyList<(int inventoryItemId, decimal quantity, decimal? unitPrice, EquipmentCondition? condition, string? conditionNote)> lines,
        string? note = null,
        CancellationToken ct = default)
    {
        if (lines.Count == 0) throw new ArgumentException("Transaction must have at least one line.", nameof(lines));

        var tx = new StockTransaction
        {
            TransactionType = type,
            FromWarehouseId = fromWarehouseId,
            ToWarehouseId = toWarehouseId,
            RelatedTransactionId = requestId,
            UserId = userId,
            Date = date,
            Note = note,
        };
        tx.SetItems(lines.Select(l => new TransactionItemDto
        {
            InventoryItemId = l.inventoryItemId,
            Quantity = l.quantity,
            UnitPrice = l.unitPrice,
            ConditionAtMoment = l.condition,
            ConditionNote = l.conditionNote,
        }).ToList());

        await ApplyTransactionAsync(tx, ct);
        return tx.Id;
    }

    public async Task<int> ApplyWriteOffAsync(
        int fromWarehouseId,
        WriteOffReason reason,
        string userId,
        DateTime date,
        IReadOnlyList<(int inventoryItemId, decimal quantity, decimal? unitPrice)> lines,
        string? note = null,
        CancellationToken ct = default)
    {
        if (lines.Count == 0) throw new ArgumentException("WriteOff must have at least one line.", nameof(lines));

        var tx = new StockTransaction
        {
            TransactionType = TransactionType.WriteOff,
            FromWarehouseId = fromWarehouseId,
            ToWarehouseId = null,
            WriteOffReason = reason,
            UserId = userId,
            Date = date,
            Note = note,
        };
        tx.SetItems(lines.Select(l => new TransactionItemDto
        {
            InventoryItemId = l.inventoryItemId,
            Quantity = l.quantity,
            UnitPrice = l.unitPrice,
        }).ToList());

        await ApplyTransactionAsync(tx, ct);
        return tx.Id;
    }

    private async Task ApplyTransactionAsync(StockTransaction tx, CancellationToken ct)
    {
        await using var dbTx = await db.Database.BeginTransactionAsync(ct);

        db.StockTransactions.Add(tx);
        await db.SaveChangesAsync(ct);

        foreach (var d in tx.GetItems())
        {
            if (d.Quantity <= 0) throw new InvalidOperationException("Quantity must be positive.");

            if (tx.FromWarehouseId is int fromId && d.InventoryItemId.HasValue)
            {
                await AddToBalanceAsync(fromId, d.InventoryItemId.Value, -d.Quantity, ct);
            }

            if (tx.ToWarehouseId is int toId && d.InventoryItemId.HasValue)
            {
                await AddToBalanceAsync(toId, d.InventoryItemId.Value, d.Quantity, ct);
            }
        }

        await db.SaveChangesAsync(ct);
        await dbTx.CommitAsync(ct);

        logger.LogInformation("Stock transaction applied: {Id} {Type}", tx.Id, tx.TransactionType);
    }

    private async Task AddToBalanceAsync(int warehouseId, int inventoryItemId, decimal delta, CancellationToken ct)
    {
        var balance = await db.StockBalances
            .FirstOrDefaultAsync(x => x.WarehouseId == warehouseId && x.InventoryItemId == inventoryItemId, ct);

        if (balance is null)
        {
            balance = new StockBalance
            {
                WarehouseId = warehouseId,
                InventoryItemId = inventoryItemId,
                Quantity = 0,
            };
            db.StockBalances.Add(balance);
        }

        balance.Quantity += delta;
        if (balance.Quantity < 0)
        {
            throw new InvalidOperationException($"Negative balance for warehouseId={warehouseId}, inventoryItemId={inventoryItemId}");
        }
    }
}
