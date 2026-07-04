using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Areas.Master.Models.Work;
using VentCleanInventory.Web.Data;
using VentCleanInventory.Web.Data.Entities;
using VentCleanInventory.Web.Services;

namespace VentCleanInventory.Web.Areas.Master.Controllers;

[Area(MasterArea.Name)]
[Authorize(Roles = AppUserRole.Master)]
public class WorkController(ApplicationDbContext db, StockService stockService, UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Consume(int? workObjectId = null)
    {
        var model = new WorkConsumeViewModel { WorkObjectId = workObjectId };
        var viewModel = await BuildAsync(model);
        if (viewModel is null) return RedirectToAction("Index", "MyWork");
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Consume(WorkConsumeViewModel model)
    {
        var built = await BuildAsync(model);
        if (built is null) return RedirectToAction("Index", "MyWork");
        model = built;
        if (!ModelState.IsValid) return View(model);

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Forbid();

        var mobileWarehouseId = await EnsureMobileWarehouseAsync(userId);

        var usedLines = model.Lines.Where(l => l.InventoryItemId.HasValue && l.Quantity > 0).ToList();
        if (usedLines.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Добавьте хотя бы один материал.");
            return View(model);
        }

        var invIds = usedLines.Select(l => l.InventoryItemId!.Value).Distinct().ToList();
        var balances = await db.StockBalances.AsNoTracking()
            .Where(b => b.WarehouseId == mobileWarehouseId && invIds.Contains(b.InventoryItemId))
            .ToDictionaryAsync(b => b.InventoryItemId, b => b.Quantity);

        foreach (var l in usedLines)
        {
            if (!balances.TryGetValue(l.InventoryItemId!.Value, out var avail) || avail < l.Quantity)
            {
                ModelState.AddModelError(string.Empty, "Недостаточно остатка по выбранному материалу.");
                return View(model);
            }
        }

        var priceMap = await db.InventoryItems.AsNoTracking()
            .Where(ii => invIds.Contains(ii.Id))
            .ToDictionaryAsync(ii => ii.Id, ii => ii.PurchasePrice);

        var materials = new List<string>();
        foreach (var l in usedLines)
        {
            var item = await db.InventoryItems.AsNoTracking()
                .Include(ii => ii.Nomenclature)
                .FirstAsync(ii => ii.Id == l.InventoryItemId!.Value);
            if (item.Nomenclature.IsEquipment)
            {
                ModelState.AddModelError(string.Empty, "Нельзя списывать оборудование как материал.");
                return View(model);
            }
            materials.Add($"{item.Nomenclature.Name} x{l.Quantity}");
        }

        var workLog = new WorkLog
        {
            MasterUserId = userId,
            WorkObjectId = model.WorkObjectId!.Value,
            ZoneName = model.ZoneName,
            WorkDate = DateTime.UtcNow,
            Description = null,
            MaterialsUsed = string.Join("; ", materials),
            Meters = model.Meters,
            Grids = model.Grids.HasValue ? (int)model.Grids.Value : null,
        };

        db.WorkLogs.Add(workLog);
        await db.SaveChangesAsync();

        var txId = await stockService.ApplyWorkConsumptionAsync(
            fromWarehouseId: mobileWarehouseId,
            workObjectId: workLog.WorkObjectId,
            workLogId: workLog.Id,
            userId: userId,
            date: DateTime.UtcNow,
            lines: usedLines.Select(l => (l.InventoryItemId!.Value, l.Quantity, priceMap.GetValueOrDefault(l.InventoryItemId!.Value))).ToList(),
            note: $"Расход по работам (WorkLog #{workLog.Id})");

        var tx = await db.StockTransactions.FirstAsync(t => t.Id == txId);
        tx.ActNumber = $"Р-{DateTime.UtcNow:yyyyMMdd}-{txId}";
        tx.ActDate = DateTime.UtcNow;
        tx.ActCreatedByUserId = userId;
        tx.ActStatus = WriteOffActStatus.Draft;
        tx.WriteOffReason = WriteOffReason.Wear;
        await db.SaveChangesAsync();

        // save work photo
        if (model.DefectPhoto is { Length: > 0 })
        {
            var ext = Path.GetExtension(model.DefectPhoto.FileName).ToLowerInvariant();
            if (ext is ".jpg" or ".jpeg" or ".png")
            {
                var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "work");
                Directory.CreateDirectory(dir);
                var fileName = $"work-{workLog.Id}{ext}";
                var filePath = Path.Combine(dir, fileName);
                await using (var stream = new FileStream(filePath, FileMode.Create))
                    await model.DefectPhoto.CopyToAsync(stream);
                workLog.PhotoPath = $"/uploads/work/{fileName}";
                workLog.IsCompleted = true;
                await db.SaveChangesAsync();
            }
        }

        TempData["Info"] = "Работа добавлена, акт списания создан.";
        return RedirectToAction("Index", "MyWork");
    }

    private async Task<WorkConsumeViewModel?> BuildAsync(WorkConsumeViewModel model)
    {
        model.Objects = await db.WorkObjects.AsNoTracking().OrderBy(o => o.Name).ToListAsync();

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrWhiteSpace(userId) && await userManager.Users.AnyAsync(u => u.Id == userId))
        {
            var warehouseId = await EnsureMobileWarehouseAsync(userId);
            var items = await db.StockBalances.AsNoTracking()
                .Where(b => b.WarehouseId == warehouseId && b.Quantity > 0 && !b.InventoryItem.Nomenclature.IsEquipment)
                .Include(b => b.InventoryItem).ThenInclude(ii => ii.Nomenclature)
                .OrderBy(b => b.InventoryItem.Nomenclature.Name)
                .Select(b => new { b.InventoryItemId, Display = b.InventoryItem.Nomenclature.Name, b.Quantity })
                .ToListAsync();

            foreach (var l in model.Lines)
            {
                if (l.InventoryItemId.HasValue)
                {
                    l.ItemDisplay = items.FirstOrDefault(x => x.InventoryItemId == l.InventoryItemId.Value)?.Display;
                }
            }

            ViewBag.MaterialOptions = items;
        }

        model.Lines ??= [];
        if (model.Lines.Count == 0) model.Lines.Add(new WorkConsumeViewModel.Line());
        return model;
    }

    private async Task<int> EnsureMobileWarehouseAsync(string userId)
    {
        var w = await db.Warehouses.FirstOrDefaultAsync(x => x.Type == WarehouseType.Mobile && x.MasterUserId == userId);
        if (w is not null) return w.Id;
        w = new Warehouse { Type = WarehouseType.Mobile, MasterUserId = userId, Name = "Карманный склад" };
        db.Warehouses.Add(w);
        await db.SaveChangesAsync();
        return w.Id;
    }
}
