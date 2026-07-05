using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Areas.Admin.Models.Warehouses;
using VentCleanInventory.Web.Data;
using VentCleanInventory.Web.Data.Entities;

namespace VentCleanInventory.Web.Areas.Admin.Controllers;

[Area(AdminArea.Name)]
[Authorize(Roles = AppUserRole.Admin)]
public class WarehousesController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var items = await db.Warehouses
            .AsNoTracking()
            .OrderBy(w => w.Type)
            .ThenBy(w => w.Name)
            .ToListAsync();

        var masterIds = items.Select(w => w.MasterUserId).Where(id => id != null).Distinct().ToList();
        var masters = await userManager.Users
            .Where(u => masterIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.UserName ?? "");

        return View(items.Select(w => new WarehouseIndexViewModel
        {
            Id = w.Id,
            Name = w.Name,
            Type = w.Type,
            MasterName = w.MasterUserId != null ? masters.GetValueOrDefault(w.MasterUserId, "-") : "-",
        }).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        return View(await BuildModelAsync(new WarehouseEditViewModel
        {
            Type = WarehouseType.Central,
        }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(WarehouseEditViewModel model)
    {
        if (!await ValidateAsync(model))
        {
            return View(await BuildModelAsync(model));
        }

        db.Warehouses.Add(new Warehouse
        {
            Name = model.Name.Trim(),
            Type = model.Type,
            Address = model.Address?.Trim(),
            Note = model.Note?.Trim(),
            MasterUserId = model.Type == WarehouseType.Mobile ? model.MasterUserId : null,
        });

        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var w = await db.Warehouses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (w is null) return NotFound();

        return View(await BuildModelAsync(new WarehouseEditViewModel
        {
            Id = w.Id,
            Name = w.Name,
            Type = w.Type,
            Address = w.Address,
            Note = w.Note,
            MasterUserId = w.MasterUserId,
        }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, WarehouseEditViewModel model)
    {
        if (id != model.Id) return BadRequest();

        if (!await ValidateAsync(model))
        {
            return View(await BuildModelAsync(model));
        }

        var w = await db.Warehouses.FirstOrDefaultAsync(x => x.Id == id);
        if (w is null) return NotFound();

        w.Name = model.Name.Trim();
        w.Type = model.Type;
        w.Address = model.Address?.Trim();
        w.Note = model.Note?.Trim();
        w.MasterUserId = model.Type == WarehouseType.Mobile ? model.MasterUserId : null;

        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var w = await db.Warehouses.FindAsync(id);
        if (w is null) return NotFound();

        var hasBalances = await db.StockBalances.AsNoTracking()
            .AnyAsync(b => b.WarehouseId == id && b.Quantity > 0);
        if (hasBalances)
        {
            TempData["Error"] = $"Нельзя удалить склад «{w.Name}» — на нём есть остатки.";
            return RedirectToAction(nameof(Index));
        }

        var hasTransactions = await db.StockTransactions.AsNoTracking()
            .AnyAsync(t => t.FromWarehouseId == id || t.ToWarehouseId == id);
        if (hasTransactions)
        {
            TempData["Error"] = $"Нельзя удалить склад «{w.Name}» — он используется в транзакциях.";
            return RedirectToAction(nameof(Index));
        }

        db.Warehouses.Remove(w);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Склад «{w.Name}» удалён.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> ValidateAsync(WarehouseEditViewModel model)
    {
        if (!ModelState.IsValid) return false;

        if (model.Type == WarehouseType.Mobile)
        {
            if (string.IsNullOrWhiteSpace(model.MasterUserId))
            {
                ModelState.AddModelError(nameof(model.MasterUserId), "Для мобильного склада выберите мастера.");
                return false;
            }

            var master = await userManager.FindByIdAsync(model.MasterUserId);
            if (master is null)
            {
                ModelState.AddModelError(nameof(model.MasterUserId), "Мастер не найден.");
                return false;
            }

            var isMaster = await userManager.IsInRoleAsync(master, AppUserRole.Master);
            if (!isMaster)
            {
                ModelState.AddModelError(nameof(model.MasterUserId), "Выбранный пользователь не является мастером.");
                return false;
            }

            var existsOther = await db.Warehouses.AsNoTracking().AnyAsync(w =>
                w.Id != model.Id && w.MasterUserId == model.MasterUserId);
            if (existsOther)
            {
                ModelState.AddModelError(nameof(model.MasterUserId), "У этого мастера уже есть мобильный склад.");
                return false;
            }
        }

        return true;
    }

    private async Task<WarehouseEditViewModel> BuildModelAsync(WarehouseEditViewModel model)
    {
        var masters = await userManager.Users.AsNoTracking().OrderBy(u => u.UserName).ToListAsync();
        var options = new List<WarehouseEditViewModel.MasterOption>();

        foreach (var u in masters)
        {
            if (await userManager.IsInRoleAsync(u, AppUserRole.Master))
            {
                options.Add(new WarehouseEditViewModel.MasterOption
                {
                    Id = u.Id,
                    Display = string.IsNullOrWhiteSpace(u.FullName) ? (u.UserName ?? u.Id) : $"{u.FullName} ({u.UserName})",
                });
            }
        }

        model.Masters = options;
        return model;
    }
}

