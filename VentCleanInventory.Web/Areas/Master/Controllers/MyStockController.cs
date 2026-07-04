using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Areas.Master.Models.MyStock;
using VentCleanInventory.Web.Data;
using VentCleanInventory.Web.Data.Entities;

namespace VentCleanInventory.Web.Areas.Master.Controllers;

[Area(MasterArea.Name)]
[Authorize(Roles = AppUserRole.Master)]
public class MyStockController(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index(string filter = "all")
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId) || !await userManager.Users.AnyAsync(u => u.Id == userId))
            return RedirectToAction("Index", "MasterHome");

        var warehouseId = await EnsureMobileWarehouseAsync(userId);

        var q = db.StockBalances.AsNoTracking()
            .Where(b => b.WarehouseId == warehouseId && b.Quantity > 0)
            .Include(b => b.InventoryItem)
            .ThenInclude(ii => ii.Nomenclature)
            .AsQueryable();

        if (filter == "equip") q = q.Where(b => b.InventoryItem.Nomenclature.IsEquipment);
        if (filter == "cons") q = q.Where(b => !b.InventoryItem.Nomenclature.IsEquipment);

        var rows = await q
            .OrderBy(b => b.InventoryItem.Nomenclature.Name)
            .ThenBy(b => b.InventoryItem.SerialNumber)
            .Select(b => new MyStockIndexViewModel.Row
            {
                InventoryItemId = b.InventoryItemId,
                Name = b.InventoryItem.Nomenclature.Name,
                SerialNumber = b.InventoryItem.SerialNumber,
                Quantity = b.Quantity,
                IsEquipment = b.InventoryItem.Nomenclature.IsEquipment,
            })
            .ToListAsync();

        return View(new MyStockIndexViewModel
        {
            Filter = filter,
            Rows = rows,
        });
    }

    private async Task<int> EnsureMobileWarehouseAsync(string userId)
    {
        var w = await db.Warehouses.FirstOrDefaultAsync(x => x.Type == WarehouseType.Mobile && x.MasterUserId == userId);
        if (w is not null) return w.Id;

        w = new Warehouse
        {
            Type = WarehouseType.Mobile,
            MasterUserId = userId,
            Name = "Карманный склад",
        };
        db.Warehouses.Add(w);
        await db.SaveChangesAsync();
        return w.Id;
    }
}

