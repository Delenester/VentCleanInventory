using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Areas.Dispatcher.Models.Requests;
using VentCleanInventory.Web.Data;
using VentCleanInventory.Web.Data.Entities;
using VentCleanInventory.Web.Services;

namespace VentCleanInventory.Web.Areas.Dispatcher.Controllers;

[Area(DispatcherArea.Name)]
[Authorize(Roles = AppUserRole.Dispatcher)]
public class RequestsController(
    ApplicationDbContext db,
    StockService stockService,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var items = await db.StockTransactions
            .AsNoTracking()
            .Include(r => r.WorkObject)
            .Where(r => r.RequestStatusValue != null && r.RequestStatusValue != RequestStatus.Completed && r.RequestStatusValue != RequestStatus.Rejected)
            .OrderByDescending(r => r.Date)
            .ToListAsync();

        var userIds = items.Select(r => r.UserId).Where(id => id != null).Distinct().ToList();
        var users = await userManager.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.UserName ?? "");

        var vm = items.Select(r => new RequestListItemViewModel
        {
            Id = r.Id,
            DateCreated = r.Date,
            MasterName = r.AssignedMasterId != null ? users.GetValueOrDefault(r.AssignedMasterId, "") : "",
            ObjectName = r.WorkObject?.Name ?? "",
            Status = r.RequestStatusValue!.Value,
            ItemsCount = r.GetItems().Count,
        }).ToList();

        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var req = await db.StockTransactions
            .AsNoTracking()
            .Include(r => r.WorkObject)
            .Include(r => r.Client)
            .Include(r => r.Supplier)
            .FirstOrDefaultAsync(r => r.Id == id && r.RequestStatusValue != null);

        if (req is null) return NotFound();

        var master = req.UserId != null ? await userManager.FindByIdAsync(req.UserId) : null;
        ViewBag.MasterName = master?.FullName ?? master?.UserName ?? "";
        return View(req);
    }

    [HttpGet]
    public async Task<IActionResult> Issue(int id)
    {
        var req = await db.StockTransactions
            .Include(r => r.WorkObject)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (req is null || req.RequestStatusValue == null) return NotFound();
        if (req.RequestStatusValue != RequestStatus.New && req.RequestStatusValue != RequestStatus.ClientConfirmed)
        {
            TempData["Error"] = "Выдача доступна только для заявок со статусами «Новая» или «Договор подтверждён».";
            return RedirectToAction(nameof(Details), new { id });
        }

        var items = req.GetItems();

        var centralWarehouseId = await db.Warehouses.AsNoTracking()
            .Where(w => w.Type == WarehouseType.Central)
            .Select(w => w.Id)
            .FirstOrDefaultAsync();
        if (centralWarehouseId == 0) return BadRequest("Central warehouse not found.");

        var master = req.UserId != null ? await userManager.FindByIdAsync(req.UserId) : null;
        var masterFullName = master?.FullName ?? master?.UserName ?? "?";

        var mobile = await db.Warehouses.FirstOrDefaultAsync(w => w.Type == WarehouseType.Mobile && w.MasterUserId == req.UserId);
        if (mobile is null)
        {
            mobile = new Warehouse
            {
                Type = WarehouseType.Mobile,
                Name = $"Карманный склад: {masterFullName}",
            MasterUserId = req.UserId ?? "",
            };
            db.Warehouses.Add(mobile);
            await db.SaveChangesAsync();
        }

        var balances = await db.StockBalances
            .AsNoTracking()
            .Where(b => b.WarehouseId == centralWarehouseId && b.Quantity > 0)
            .Include(b => b.InventoryItem)
            .ThenInclude(ii => ii.Nomenclature)
            .OrderBy(b => b.InventoryItem.Nomenclature.Name)
            .ThenBy(b => b.InventoryItem.ExpirationDate)
            .ThenBy(b => b.InventoryItem.Id)
            .ToListAsync();

        var lines = new List<RequestIssueViewModel.Line>();

        foreach (var ri in items)
        {
            if (ri.NomenclatureId == null) continue;
            var nom = await db.Nomenclatures.AsNoTracking().FirstOrDefaultAsync(n => n.Id == ri.NomenclatureId);
            if (nom is null) continue;
            var nomBalances = balances.Where(b => b.InventoryItem.NomenclatureId == ri.NomenclatureId).ToList();
            if (nom.IsEquipment)
            {
                var availableEquip = nomBalances
                    .Where(b => !string.IsNullOrWhiteSpace(b.InventoryItem.SerialNumber))
                    .ToList();

                var needed = (int)Math.Round(ri.Quantity, MidpointRounding.AwayFromZero);
                var take = availableEquip.Take(needed).ToList();

                foreach (var b in take)
                {
                    lines.Add(new RequestIssueViewModel.Line
                    {
                        InventoryItemId = b.InventoryItemId,
                        ItemDisplay = $"{nom.Name} [{b.InventoryItem.SerialNumber}]",
                        Available = b.Quantity,
                        Quantity = 1,
                        Condition = EquipmentCondition.Ok,
                    });
                }
            }
            else
            {
                var remaining = ri.Quantity;
                foreach (var b in nomBalances)
                {
                    if (remaining <= 0) break;
                    var qty = Math.Min(remaining, b.Quantity);
                    remaining -= qty;

                    var batch = string.IsNullOrWhiteSpace(b.InventoryItem.BatchNumber) ? "" : $" партия {b.InventoryItem.BatchNumber}";
                    var exp = b.InventoryItem.ExpirationDate.HasValue ? $" годен до {b.InventoryItem.ExpirationDate:yyyy-MM-dd}" : "";

                    lines.Add(new RequestIssueViewModel.Line
                    {
                        InventoryItemId = b.InventoryItemId,
                        ItemDisplay = $"{nom.Name}{batch}{exp}",
                        Available = b.Quantity,
                        Quantity = qty,
                    });
                }
            }
        }

        var header = $"Заявка №{req.Id} от {req.Date:yyyy-MM-dd} — {req.WorkObject?.Name ?? ""}";
        var vm = new RequestIssueViewModel
        {
            RequestId = req.Id,
            MasterUserId = req.UserId ?? "",
            CentralWarehouseId = centralWarehouseId,
            MobileWarehouseId = mobile.Id,
            Header = header,
            Lines = lines,
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Issue(RequestIssueViewModel model)
    {
        if (model.Lines.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Нет строк для выдачи.");
            return View(model);
        }

        if (!ModelState.IsValid) return View(model);

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Forbid();

        var invIds = model.Lines.Select(l => l.InventoryItemId).Distinct().ToList();
        var centralBalances = await db.StockBalances
            .AsNoTracking()
            .Where(b => b.WarehouseId == model.CentralWarehouseId && invIds.Contains(b.InventoryItemId))
            .ToDictionaryAsync(b => b.InventoryItemId, b => b.Quantity);

        foreach (var l in model.Lines)
        {
            if (l.Quantity <= 0) continue;
            if (!centralBalances.TryGetValue(l.InventoryItemId, out var avail) || avail < l.Quantity)
            {
                ModelState.AddModelError(string.Empty, $"Недостаточно остатка по позиции: {l.ItemDisplay}");
                return View(model);
            }
        }

        var priceMap = await db.InventoryItems
            .AsNoTracking()
            .Where(ii => invIds.Contains(ii.Id))
            .ToDictionaryAsync(ii => ii.Id, ii => ii.PurchasePrice);

        var lines = model.Lines
            .Where(l => l.Quantity > 0)
            .Select(l => (l.InventoryItemId, l.Quantity, unitPrice: priceMap.GetValueOrDefault(l.InventoryItemId), l.Condition, l.ConditionNote))
            .ToList();

        await stockService.ApplyMovementAsync(
            type: TransactionType.Issue,
            fromWarehouseId: model.CentralWarehouseId,
            toWarehouseId: model.MobileWarehouseId,
            requestId: null,
            userId: userId,
            date: DateTime.UtcNow,
            lines: lines,
            note: $"Выдача по заявке №{model.RequestId}");

        var req = await db.StockTransactions.FirstOrDefaultAsync(r => r.Id == model.RequestId);
        if (req is not null)
        {
            if (req.RequestStatusValue != RequestStatus.New && req.RequestStatusValue != RequestStatus.ClientConfirmed)
                return BadRequest("Заявка уже обработана.");
            req.RequestStatusValue = RequestStatus.Assigned;
            await db.SaveChangesAsync();
        }

        TempData["Info"] = $"Выдача по заявке №{model.RequestId} проведена.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteWork(int id)
    {
        var req = await db.StockTransactions.FindAsync(id);
        if (req == null) return NotFound();

        if (req.RequestStatusValue != RequestStatus.ClientConfirmed
            && req.RequestStatusValue != RequestStatus.Assigned && req.RequestStatusValue != RequestStatus.InProgress)
            return BadRequest("Клиент ещё не подтвердил договор.");

        req.RequestStatusValue = RequestStatus.Completed;
        await db.SaveChangesAsync();

        db.Notifications.Add(new Notification
        {
            UserId = req.UserId,
            Title = "Работа выполнена",
            Message = $"Работа по заявке №{req.Id} завершена.",
            Link = "/Client/ClientHome",
        });
        await db.SaveChangesAsync();

        TempData["Info"] = $"Заявка №{id} отмечена как выполненная.";
        return RedirectToAction(nameof(Index));
    }
}
