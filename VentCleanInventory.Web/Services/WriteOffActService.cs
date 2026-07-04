using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Data;
using VentCleanInventory.Web.Data.Entities;

namespace VentCleanInventory.Web.Services;

public class WriteOffActService(ApplicationDbContext db)
{
    public async Task ApproveAsync(int actId, CancellationToken ct = default)
    {
        var tx = await db.StockTransactions.FirstOrDefaultAsync(t => t.Id == actId, ct)
            ?? throw new InvalidOperationException($"Транзакция #{actId} не найдена.");

        tx.ActStatus = WriteOffActStatus.Approved;
        await db.SaveChangesAsync(ct);
    }
}
