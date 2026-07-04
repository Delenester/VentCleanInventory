using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Data;
using VentCleanInventory.Web.Data.Entities;
using VentCleanInventory.Web.Services;

namespace VentCleanInventory.Web.Areas.Dispatcher.Controllers;

[Area(DispatcherArea.Name)]
[Authorize(Roles = AppUserRole.Dispatcher)]
public class WriteOffActsController(ApplicationDbContext db, WriteOffActService writeOffActService, UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index(WriteOffActStatus? status = null)
    {
        var q = db.StockTransactions.AsNoTracking()
            .Where(t => t.TransactionType == TransactionType.WriteOff && t.ActNumber != null)
            .Include(t => t.FromWarehouse)
            .AsQueryable();

        if (status.HasValue)
            q = q.Where(a => a.ActStatus == status.Value);

        var acts = await q.OrderByDescending(a => a.ActDate).ToListAsync();

        var userIds = acts.Select(t => t.UserId).Where(id => id != null).Distinct().ToList();
        var users = await userManager.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.UserName ?? "");
        ViewBag.UserNames = users;
        ViewBag.StatusFilter = status;
        return View(acts);
    }

    public async Task<IActionResult> Details(int id)
    {
        var tx = await db.StockTransactions.AsNoTracking()
            .Include(t => t.FromWarehouse)
            .FirstOrDefaultAsync(t => t.Id == id && t.TransactionType == TransactionType.WriteOff && t.ActNumber != null);

        if (tx is null) return NotFound();

        var invIds = tx.GetItems().Select(i => i.InventoryItemId).ToList();
        var invMap = await db.InventoryItems.AsNoTracking()
            .Include(ii => ii.Nomenclature)
            .Where(ii => invIds.Contains(ii.Id))
            .ToDictionaryAsync(ii => ii.Id);

        ViewBag.InventoryMap = invMap;
        return View(tx);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        await writeOffActService.ApproveAsync(id);
        TempData["Info"] = "Акт списания утверждён.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
