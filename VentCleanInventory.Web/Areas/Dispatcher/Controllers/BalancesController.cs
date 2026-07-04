using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Areas.Dispatcher.Models.Balances;
using VentCleanInventory.Web.Data;

namespace VentCleanInventory.Web.Areas.Dispatcher.Controllers;

[Area(DispatcherArea.Name)]
[Authorize(Roles = AppUserRole.Dispatcher)]
public class BalancesController(ApplicationDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] BalancesIndexViewModel model)
    {
        model.Warehouses = await db.Warehouses.AsNoTracking().OrderBy(w => w.Type).ThenBy(w => w.Name).ToListAsync();
        model.Nomenclatures = await db.Nomenclatures.AsNoTracking().OrderBy(n => n.Name).ToListAsync();

        var q = db.StockBalances.AsNoTracking()
            .Where(b => b.Quantity > 0)
            .Include(b => b.Warehouse)
            .Include(b => b.InventoryItem)
            .ThenInclude(ii => ii.Nomenclature)
            .AsQueryable();

        if (model.WarehouseId is int wid)
        {
            q = q.Where(b => b.WarehouseId == wid);
        }

        if (model.NomenclatureId is int nid)
        {
            q = q.Where(b => b.InventoryItem.NomenclatureId == nid);
        }

        if (model.ExpirationBefore is DateTime exp)
        {
            q = q.Where(b => b.InventoryItem.ExpirationDate != null && b.InventoryItem.ExpirationDate <= exp);
        }

        var balances = await q
            .OrderBy(b => b.Warehouse.Name)
            .ThenBy(b => b.InventoryItem.Nomenclature.Name)
            .ThenBy(b => b.InventoryItem.ExpirationDate)
            .ThenBy(b => b.InventoryItem.SerialNumber)
            .Select(b => new
            {
                b.Warehouse.Name,
                ItemName = b.InventoryItem.Nomenclature.Name,
                Unit = b.InventoryItem.Nomenclature.Unit,
                IsEquipment = b.InventoryItem.Nomenclature.IsEquipment,
                SerialOrBatch = b.InventoryItem.SerialNumber ?? b.InventoryItem.BatchNumber,
                b.InventoryItem.ExpirationDate,
                b.Quantity,
            })
            .ToListAsync();

        var soon = DateTime.UtcNow.Date.AddDays(30);
        model.Rows = balances.Select(b => new BalancesIndexViewModel.Row
        {
            WarehouseName = b.Name,
            ItemName = b.ItemName,
            SerialOrBatch = b.SerialOrBatch,
            ExpirationDate = b.ExpirationDate,
            Quantity = b.Quantity,
            Unit = b.Unit,
            IsEquipment = b.IsEquipment,
        }).ToList();

        model.TotalPositions = model.Rows.Count;
        model.TotalQuantity = model.Rows.Sum(r => r.Quantity);
        model.ExpiringSoonCount = model.Rows.Count(r =>
            r.ExpirationDate.HasValue && r.ExpirationDate.Value.Date <= soon && r.ExpirationDate.Value.Date >= DateTime.UtcNow.Date);

        model.SummaryByNomenclature = model.Rows
            .GroupBy(r => new { r.ItemName, r.Unit })
            .Select(g => new BalancesIndexViewModel.SummaryRow
            {
                ItemName = g.Key.ItemName,
                Unit = g.Key.Unit,
                TotalQuantity = g.Sum(x => x.Quantity),
                WarehouseCount = g.Select(x => x.WarehouseName).Distinct().Count(),
            })
            .OrderBy(s => s.ItemName)
            .ToList();

        return View(model);
    }
}

