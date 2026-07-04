using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Areas.Supplier.Models;
using VentCleanInventory.Web.Data;

namespace VentCleanInventory.Web.Areas.Supplier.Controllers;

[Area(SupplierArea.Name)]
[Authorize(Roles = AppUserRole.Supplier)]
public class SupplierHomeController(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var user = await GetCurrentUserAsync();
        if (user?.OrganizationId is not int orgId ||
            user.AccountType != AccountType.Supplier)
        {
            return View(new SupplierPortalViewModel());
        }

        var supplier = await db.Organizations.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == orgId && s.Type == OrganizationType.Supplier);

        var suppliedItemsCount = await db.InventoryItems.AsNoTracking()
            .Where(ii => ii.OrganizationId == orgId)
            .CountAsync();

        var totalStockQty = await db.StockBalances.AsNoTracking()
            .Where(b => b.InventoryItem.OrganizationId == orgId && b.Quantity > 0)
            .SumAsync(b => (decimal?)b.Quantity) ?? 0;

        var recentTxCount = await db.StockTransactions.AsNoTracking()
            .Where(tx => tx.OrganizationId == orgId && tx.Date >= DateTime.UtcNow.AddDays(-30))
            .CountAsync();

        var items = await db.InventoryItems.AsNoTracking()
            .Include(ii => ii.Nomenclature)
            .Where(ii => ii.OrganizationId == orgId)
            .OrderBy(ii => ii.Nomenclature.Name)
            .Select(ii => new SuppliedItemInfo
            {
                NomenclatureName = ii.Nomenclature.Name,
                SerialNumber = ii.SerialNumber,
                BatchNumber = ii.BatchNumber,
                Quantity = db.StockBalances.Where(b => b.InventoryItemId == ii.Id).Sum(b => (decimal?)b.Quantity) ?? 0,
            })
            .ToListAsync();

        return View(new SupplierPortalViewModel
        {
            Supplier = supplier,
            SuppliedItemsCount = suppliedItemsCount,
            TotalStockQuantity = totalStockQty,
            RecentTransactionsCount = recentTxCount,
            SuppliedItems = items,
        });
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return userId is null ? null : await userManager.FindByIdAsync(userId);
    }
}
