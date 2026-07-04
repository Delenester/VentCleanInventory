using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Areas.Supplier.Models;
using VentCleanInventory.Web.Data;

namespace VentCleanInventory.Web.Areas.Supplier.Controllers;

[Area(SupplierArea.Name)]
[Authorize(Roles = AppUserRole.Supplier)]
public class SuppliesController(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(DateTime? from = null, DateTime? to = null)
    {
        var user = await GetCurrentUserAsync();
        if (user?.OrganizationId is not int orgId || user.AccountType != AccountType.Supplier)
            return View(new SupplierSuppliesViewModel());

        var t = (to ?? DateTime.UtcNow.Date).Date.AddDays(1).AddTicks(-1);
        var f = (from ?? DateTime.UtcNow.Date.AddDays(-90)).Date;
        if (f > t) (f, t) = (t.Date, f.Date.AddDays(1).AddTicks(-1));

        var txns = await db.StockTransactions.AsNoTracking()
            .Where(tx => tx.OrganizationId == orgId && tx.Date >= f && tx.Date <= t)
            .OrderByDescending(tx => tx.Date)
            .ToListAsync();

        var rows = txns.Select(tx =>
        {
            var items = tx.GetItems();
            return new SupplyInfo
            {
                Date = tx.Date,
                TransactionType = tx.TransactionType,
                Note = tx.Note,
                ItemCount = items.Count,
                TotalQuantity = items.Sum(i => i.Quantity),
            };
        }).ToList();

        return View(new SupplierSuppliesViewModel
        {
            From = f,
            To = t,
            Rows = rows,
            TotalQuantity = rows.Sum(r => r.TotalQuantity),
        });
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return userId is null ? null : await userManager.FindByIdAsync(userId);
    }
}
