using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Areas.Dispatcher.Models.Receipts;
using VentCleanInventory.Web.Data;
using VentCleanInventory.Web.Data.Entities;
using VentCleanInventory.Web.Services;

namespace VentCleanInventory.Web.Areas.Dispatcher.Controllers;

[Area(DispatcherArea.Name)]
[Authorize(Roles = AppUserRole.Dispatcher)]
public class ReceiptsController(
    ApplicationDbContext db,
    StockService stockService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        return View(await BuildModelAsync(new ReceiptCreateViewModel()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReceiptCreateViewModel model)
    {
        model = await BuildModelAsync(model);

        if (model.Lines.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Добавьте хотя бы одну позицию.");
        }

        if (!ModelState.IsValid) return View(model);

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Forbid();

        var centralWarehouseId = await db.Warehouses.AsNoTracking()
            .Where(w => w.Type == WarehouseType.Central)
            .Select(w => w.Id)
            .FirstOrDefaultAsync();

        if (centralWarehouseId == 0)
        {
            ModelState.AddModelError(string.Empty, "Не найден центральный склад.");
            return View(model);
        }

        var receiptLines = new List<(int inventoryItemId, decimal quantity, decimal? unitPrice)>();

        foreach (var line in model.Lines.Where(l => l.NomenclatureId.HasValue))
        {
            var nom = await db.Nomenclatures.AsNoTracking().FirstOrDefaultAsync(x => x.Id == line.NomenclatureId!.Value);
            if (nom is null)
            {
                ModelState.AddModelError(string.Empty, "Номенклатура не найдена.");
                return View(model);
            }

            if (nom.IsEquipment)
            {
                var count = (int)Math.Round(line.Quantity, MidpointRounding.AwayFromZero);
                if (count <= 0)
                {
                    ModelState.AddModelError(string.Empty, "Для оборудования количество должно быть целым и больше 0.");
                    return View(model);
                }

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
                        OrganizationId = model.SupplierId,
                        PurchasePrice = line.UnitPrice,
                    };

                    db.InventoryItems.Add(item);
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
                    OrganizationId = model.SupplierId,
                    PurchasePrice = line.UnitPrice,
                    ExpirationDate = line.ExpirationDate,
                    BatchNumber = string.IsNullOrWhiteSpace(line.BatchNumber) ? null : line.BatchNumber.Trim(),
                };

                db.InventoryItems.Add(item);
                receiptLines.Add((item.Id, line.Quantity, line.UnitPrice));
            }
        }

        await db.SaveChangesAsync();

        var txId = await stockService.ApplyReceiptAsync(
            toWarehouseId: centralWarehouseId,
            organizationId: model.SupplierId,
            userId: userId,
            date: DateTime.UtcNow,
            lines: receiptLines,
            note: model.Note);

        TempData["Info"] = $"Документ поступления проведён (№{txId}).";
        return RedirectToAction(nameof(Create));
    }

    private async Task<ReceiptCreateViewModel> BuildModelAsync(ReceiptCreateViewModel model)
    {
        model.Suppliers = await db.Organizations.AsNoTracking()
            .Where(o => o.Type == OrganizationType.Supplier)
            .OrderBy(s => s.Name)
            .ToListAsync();
        model.Nomenclatures = await db.Nomenclatures.AsNoTracking().OrderBy(n => n.Name).ToListAsync();
        model.Lines ??= [];
        if (model.Lines.Count == 0) model.Lines.Add(new ReceiptCreateViewModel.Line());
        return model;
    }
}
