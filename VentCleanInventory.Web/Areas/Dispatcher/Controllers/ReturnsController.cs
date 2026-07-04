using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Areas.Dispatcher.Models.Returns;
using VentCleanInventory.Web.Data;
using VentCleanInventory.Web.Services;

namespace VentCleanInventory.Web.Areas.Dispatcher.Controllers;

[Area(DispatcherArea.Name)]
[Authorize(Roles = AppUserRole.Dispatcher)]
public class ReturnsController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    StockService stockService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await BuildMastersAsync(new ReturnCreateViewModel()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Load(ReturnCreateViewModel model)
    {
        model = await BuildMastersAsync(model);

        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        var centralWarehouseId = await db.Warehouses.AsNoTracking()
            .Where(w => w.Type == WarehouseType.Central)
            .Select(w => w.Id)
            .FirstOrDefaultAsync();
        if (centralWarehouseId == 0)
        {
            ModelState.AddModelError(string.Empty, "Не найден центральный склад.");
            return View("Index", model);
        }

        var mobile = await db.Warehouses.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Type == WarehouseType.Mobile && w.MasterUserId == model.MasterUserId);
        if (mobile is null)
        {
            ModelState.AddModelError(string.Empty, "У мастера нет мобильного склада.");
            return View("Index", model);
        }

        var balances = await db.StockBalances.AsNoTracking()
            .Where(b => b.WarehouseId == mobile.Id && b.Quantity > 0)
            .Include(b => b.InventoryItem)
            .ThenInclude(ii => ii.Nomenclature)
            .OrderBy(b => b.InventoryItem.Nomenclature.Name)
            .ThenBy(b => b.InventoryItem.SerialNumber)
            .ToListAsync();

        model.CentralWarehouseId = centralWarehouseId;
        model.MobileWarehouseId = mobile.Id;
        model.Lines = balances.Select(b => new ReturnCreateViewModel.Line
        {
            InventoryItemId = b.InventoryItemId,
            Available = b.Quantity,
            Quantity = 0,
            ItemDisplay = b.InventoryItem.Nomenclature.IsEquipment
                ? $"{b.InventoryItem.Nomenclature.Name} [{b.InventoryItem.SerialNumber}]"
                : b.InventoryItem.Nomenclature.Name,
            Condition = b.InventoryItem.Nomenclature.IsEquipment ? EquipmentCondition.Ok : null,
        }).ToList();

        return View("Index", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(ReturnCreateViewModel model)
    {
        model = await BuildMastersAsync(model);

        if (string.IsNullOrWhiteSpace(model.MasterUserId))
        {
            ModelState.AddModelError(nameof(model.MasterUserId), "Выберите мастера.");
        }

        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Forbid();

        var lines = model.Lines
            .Where(l => l.Quantity > 0)
            .Select(l => (l.InventoryItemId, l.Quantity, unitPrice: (decimal?)null, l.Condition, l.ConditionNote))
            .ToList();

        if (lines.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Укажите хотя бы одну позицию к возврату.");
            return View("Index", model);
        }

        await stockService.ApplyMovementAsync(
            type: TransactionType.Return,
            fromWarehouseId: model.MobileWarehouseId,
            toWarehouseId: model.CentralWarehouseId,
            requestId: null,
            userId: userId,
            date: DateTime.UtcNow,
            lines: lines,
            note: "Возврат от мастера");

        TempData["Info"] = "Возврат проведён.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<ReturnCreateViewModel> BuildMastersAsync(ReturnCreateViewModel model)
    {
        var users = await userManager.Users.AsNoTracking().OrderBy(u => u.UserName).ToListAsync();
        var options = new List<ReturnCreateViewModel.MasterOption>();
        foreach (var u in users)
        {
            if (await userManager.IsInRoleAsync(u, AppUserRole.Master))
            {
                options.Add(new ReturnCreateViewModel.MasterOption
                {
                    Id = u.Id,
                    Display = string.IsNullOrWhiteSpace(u.FullName) ? (u.UserName ?? u.Id) : $"{u.FullName} ({u.UserName})",
                });
            }
        }

        model.Masters = options;
        model.Lines ??= [];
        return model;
    }
}

