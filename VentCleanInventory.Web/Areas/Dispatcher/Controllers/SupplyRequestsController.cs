using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Areas.Dispatcher.Models.SupplyRequests;
using VentCleanInventory.Web.Data;
using VentCleanInventory.Web.Data.Entities;
using VentCleanInventory.Web.Services;

namespace VentCleanInventory.Web.Areas.Dispatcher.Controllers;

[Area(DispatcherArea.Name)]
[Authorize(Roles = AppUserRole.Dispatcher)]
public class SupplyRequestsController(
    ApplicationDbContext db,
    StockService stockService,
    NotificationService notificationService,
    UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new SupplyRequestCreateViewModel
        {
            Suppliers = await db.Organizations.AsNoTracking()
                .Where(o => o.Type == OrganizationType.Supplier)
                .OrderBy(s => s.Name)
                .ToListAsync(),
            Nomenclatures = await db.Nomenclatures.AsNoTracking()
                .OrderBy(n => n.Name)
                .ToListAsync(),
        };
        model.Lines.Add(new SupplyRequestCreateViewModel.Line());
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SupplyRequestCreateViewModel model)
    {
        model.Suppliers = await db.Organizations.AsNoTracking()
            .Where(o => o.Type == OrganizationType.Supplier)
            .OrderBy(s => s.Name)
            .ToListAsync();
        model.Nomenclatures = await db.Nomenclatures.AsNoTracking()
            .OrderBy(n => n.Name)
            .ToListAsync();

        if (!model.SupplierId.HasValue)
            ModelState.AddModelError("SupplierId", "Выберите поставщика.");

        var validLines = model.Lines.Where(l => l.NomenclatureId.HasValue && l.Quantity > 0).ToList();
        if (validLines.Count == 0)
            ModelState.AddModelError("", "Добавьте хотя бы одну позицию.");

        if (!ModelState.IsValid) return View(model);

        var now = DateTime.UtcNow;
        var reqCount = await db.SupplyRequests.CountAsync() + 1;

        var req = new SupplyRequest
        {
            Number = $"З-{now:yyyyMMdd}-{reqCount:D4}",
            OrganizationId = model.SupplierId!.Value,
            CreatedAt = now,
            Status = SupplyRequestStatus.New,
            Note = model.Note,
        };

        foreach (var line in validLines)
        {
            req.Items.Add(new SupplyRequestItem
            {
                NomenclatureId = line.NomenclatureId!.Value,
                Quantity = line.Quantity,
            });
        }

        db.SupplyRequests.Add(req);
        await db.SaveChangesAsync();

        TempData["Success"] = $"Запрос №{req.Number} создан. Отправьте его поставщику.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Index(SupplyRequestStatus? status = null)
    {
        var query = db.SupplyRequests.AsNoTracking()
            .Include(r => r.Organization)
            .Include(r => r.Items).ThenInclude(i => i.Nomenclature)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);
        else
            query = query.Where(r => r.Status != SupplyRequestStatus.Completed
                && r.Status != SupplyRequestStatus.Partial
                && r.Status != SupplyRequestStatus.Cancelled);

        var requests = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        return View(requests);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(int id)
    {
        var req = await db.SupplyRequests.Include(r => r.Organization).FirstOrDefaultAsync(r => r.Id == id);
        if (req is null || req.Status != SupplyRequestStatus.New) return NotFound();

        req.Status = SupplyRequestStatus.Sent;

        var supplierUsers = await userManager.GetUsersInRoleAsync(AppUserRole.Supplier);
        var supplierUserIds = supplierUsers
            .Where(u => u.OrganizationId == req.OrganizationId)
            .Select(u => u.Id)
            .ToList();

        if (supplierUserIds.Count > 0)
        {
            await notificationService.NotifyUsersAsync(supplierUserIds,
                "Новый запрос на поставку",
                $"Запрос №{req.Number} от {req.CreatedAt:dd.MM.yyyy} ожидает подтверждения.",
                "/Supplier/SupplyRequests");
        }

        await db.SaveChangesAsync();
        TempData["Success"] = $"Запрос №{req.Number} отправлен поставщику.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var req = await db.SupplyRequests.FirstOrDefaultAsync(r => r.Id == id);
        if (req is null || req.Status is SupplyRequestStatus.Completed or SupplyRequestStatus.Cancelled)
            return NotFound();

        req.Status = SupplyRequestStatus.Cancelled;
        await db.SaveChangesAsync();
        TempData["Success"] = $"Запрос №{req.Number} отменён.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Receive(int id)
    {
        var req = await db.SupplyRequests.AsNoTracking()
            .Include(r => r.Organization)
            .Include(r => r.Items).ThenInclude(i => i.Nomenclature)
            .FirstOrDefaultAsync(r => r.Id == id && r.Status == SupplyRequestStatus.Confirmed);

        if (req is null) return NotFound();

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Forbid();

        var centralWh = await db.Warehouses.AsNoTracking()
            .Where(w => w.Type == WarehouseType.Central)
            .FirstOrDefaultAsync();

        if (centralWh is null) return BadRequest("Центральный склад не найден.");

        var model = new SupplyRequestReceiveViewModel
        {
            RequestId = req.Id,
            RequestNumber = req.Number,
            SupplierName = req.Organization?.Name ?? "",
            CentralWarehouseId = centralWh.Id,
            Items = req.Items.Select(i => new SupplyRequestReceiveViewModel.ReceiveLine
            {
                SupplyRequestItemId = i.Id,
                NomenclatureId = i.NomenclatureId,
                NomenclatureName = i.Nomenclature?.Name ?? "",
                Unit = i.Nomenclature?.Unit ?? "",
                OrderedQuantity = i.ConfirmedQuantity ?? i.Quantity,
                ReceivedQuantity = i.ConfirmedQuantity ?? i.Quantity,
                UnitPrice = i.UnitPrice,
                IsEquipment = i.Nomenclature?.IsEquipment ?? false,
            }).ToList(),
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Receive(int id, SupplyRequestReceiveViewModel model)
    {
        if (id != model.RequestId) return BadRequest();

        var req = await db.SupplyRequests
            .Include(r => r.Items).ThenInclude(i => i.Nomenclature)
            .FirstOrDefaultAsync(r => r.Id == id && r.Status == SupplyRequestStatus.Confirmed);

        if (req is null) return NotFound();

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Forbid();

        var centralWh = await db.Warehouses.AsNoTracking()
            .Where(w => w.Type == WarehouseType.Central)
            .FirstOrDefaultAsync();

        if (centralWh is null) return BadRequest("Центральный склад не найден.");

        var receiptLines = new List<(int inventoryItemId, decimal quantity, decimal? unitPrice)>();
        var anyReceived = false;

        foreach (var line in model.Items.Where(l => l.ReceivedQuantity > 0))
        {
            anyReceived = true;
            var nom = await db.Nomenclatures.FindAsync(line.NomenclatureId);
            if (nom is null) continue;

            if (nom.IsEquipment)
            {
                var count = (int)Math.Round(line.ReceivedQuantity, MidpointRounding.AwayFromZero);
                if (count <= 0) continue;

                var nextSerial = 1;
                var lastSerial = await db.InventoryItems.AsNoTracking()
                    .Where(i => i.NomenclatureId == nom.Id && i.SerialNumber != null)
                    .OrderByDescending(i => i.SerialNumber)
                    .Select(i => i.SerialNumber)
                    .FirstOrDefaultAsync();
                if (lastSerial != null && int.TryParse(lastSerial.Split('-').Last(), out var parsed))
                    nextSerial = parsed + 1;

                for (var i = 0; i < count; i++)
                {
                    var serial = $"EQ-{nom.Id:D3}-{nextSerial + i:D4}";
                    var item = new InventoryItem
                    {
                        NomenclatureId = nom.Id,
                        SerialNumber = serial,
                        PurchaseDate = DateTime.UtcNow,
                        OrganizationId = req.OrganizationId,
                        PurchasePrice = line.UnitPrice,
                    };
                    db.InventoryItems.Add(item);
                    await db.SaveChangesAsync();
                    receiptLines.Add((item.Id, 1, line.UnitPrice));
                }
            }
            else
            {
                var item = new InventoryItem
                {
                    NomenclatureId = nom.Id,
                    SerialNumber = null,
                    PurchaseDate = DateTime.UtcNow,
                    OrganizationId = req.OrganizationId,
                    PurchasePrice = line.UnitPrice,
                };
                db.InventoryItems.Add(item);
                await db.SaveChangesAsync();
                receiptLines.Add((item.Id, line.ReceivedQuantity, line.UnitPrice));
            }

            var reqItem = req.Items.FirstOrDefault(i => i.Id == line.SupplyRequestItemId);
            if (reqItem != null)
                reqItem.ReceivedQuantity = line.ReceivedQuantity;
        }

        if (!anyReceived)
        {
            ModelState.AddModelError("", "Укажите хотя бы одну полученную позицию.");
            model.SupplierName = req.Organization?.Name ?? "";
            var centralWh2 = await db.Warehouses.AsNoTracking()
                .Where(w => w.Type == WarehouseType.Central).FirstOrDefaultAsync();
            model.CentralWarehouseId = centralWh2?.Id ?? 0;
            return View(model);
        }

        var allReceived = req.Items.All(i => i.ReceivedQuantity >= (i.ConfirmedQuantity ?? i.Quantity));
        var statusText = allReceived ? "выполнена" : "принята частично";

        await notificationService.NotifyRoleAsync(userManager, AppUserRole.Admin,
            "Поставка оприходована",
            $"Поставка №{req.Number} от {req.Organization?.Name} {statusText}.",
            "/Admin/SupplyRequests");
        await notificationService.NotifyRoleAsync(userManager, AppUserRole.Manager,
            "Поставка оприходована",
            $"Поставка №{req.Number} от {req.Organization?.Name} {statusText}.",
            "/Admin/SupplyRequests");

        await stockService.ApplyReceiptAsync(
            toWarehouseId: centralWh.Id,
            organizationId: req.OrganizationId,
            userId: userId,
            date: DateTime.UtcNow,
            lines: receiptLines,
            note: $"Приёмка по запросу №{req.Number}");

        req.Status = allReceived ? SupplyRequestStatus.Completed : SupplyRequestStatus.Partial;
        await db.SaveChangesAsync();

        TempData["Success"] = $"Поставка №{req.Number} оприходована{(allReceived ? "" : " частично")}.";
        return RedirectToAction(nameof(Index));
    }
}
