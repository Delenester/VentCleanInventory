using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Areas.Dispatcher.Models;
using VentCleanInventory.Web.Data;
using VentCleanInventory.Web.Data.Entities;

namespace VentCleanInventory.Web.Areas.Dispatcher.Controllers;

[Area("Dispatcher")]
[Authorize(Roles = "Диспетчер")]
public class NomenclatureController(
    ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var items = await db.Nomenclatures.AsNoTracking()
            .Include(n => n.PreferredSupplier)
            .OrderBy(n => n.Name)
            .ToListAsync();
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Suppliers = await db.Organizations.AsNoTracking()
            .Where(o => o.Type == OrganizationType.Supplier)
            .OrderBy(o => o.Name)
            .ToListAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(NomenclatureCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Suppliers = await db.Organizations.AsNoTracking()
                .Where(o => o.Type == OrganizationType.Supplier)
                .OrderBy(o => o.Name)
                .ToListAsync();
            return View(model);
        }

        var exists = await db.Nomenclatures.AnyAsync(n => n.Name.ToLower() == model.Name.Trim().ToLower());
        if (exists)
        {
            ModelState.AddModelError("Name", "Номенклатура с таким названием уже существует.");
            ViewBag.Suppliers = await db.Organizations.AsNoTracking()
                .Where(o => o.Type == OrganizationType.Supplier)
                .OrderBy(o => o.Name)
                .ToListAsync();
            return View(model);
        }

        db.Nomenclatures.Add(new Nomenclature
        {
            Name = model.Name.Trim(),
            IsEquipment = model.IsEquipment,
            Unit = model.Unit ?? "",
            MinStockQuantity = model.MinStockQuantity,
            PreferredSupplierId = model.PreferredSupplierId,
        });

        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.Nomenclatures.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id);
        if (item is null) return NotFound();

        ViewBag.Suppliers = await db.Organizations.AsNoTracking()
            .Where(o => o.Type == OrganizationType.Supplier)
            .OrderBy(o => o.Name)
            .ToListAsync();

        return View(new NomenclatureEditViewModel
        {
            Id = item.Id,
            Name = item.Name,
            IsEquipment = item.IsEquipment,
            Unit = item.Unit,
            MinStockQuantity = item.MinStockQuantity,
            PreferredSupplierId = item.PreferredSupplierId,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, NomenclatureEditViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Suppliers = await db.Organizations.AsNoTracking()
                .Where(o => o.Type == OrganizationType.Supplier)
                .OrderBy(o => o.Name)
                .ToListAsync();
            return View(model);
        }

        var exists = await db.Nomenclatures.AnyAsync(n => n.Name.ToLower() == model.Name.Trim().ToLower() && n.Id != id);
        if (exists)
        {
            ModelState.AddModelError("Name", "Номенклатура с таким названием уже существует.");
            ViewBag.Suppliers = await db.Organizations.AsNoTracking()
                .Where(o => o.Type == OrganizationType.Supplier)
                .OrderBy(o => o.Name)
                .ToListAsync();
            return View(model);
        }

        var item = await db.Nomenclatures.FirstOrDefaultAsync(n => n.Id == id);
        if (item is null) return NotFound();

        item.Name = model.Name.Trim();
        item.IsEquipment = model.IsEquipment;
        item.Unit = model.Unit ?? "";
        item.MinStockQuantity = model.MinStockQuantity;
        item.PreferredSupplierId = model.PreferredSupplierId;

        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.Nomenclatures.FindAsync(id);
        if (item is null) return NotFound();

        var hasSupplyItems = await db.SupplyRequests
            .AnyAsync(r => r.Items.Any(i => i.NomenclatureId == id));
        if (hasSupplyItems)
        {
            TempData["Error"] = $"Нельзя удалить «{item.Name}» — используется в запросах на поставку.";
            return RedirectToAction(nameof(Index));
        }

        var hasInventory = await db.InventoryItems.AnyAsync(i => i.NomenclatureId == id);
        if (hasInventory)
        {
            TempData["Error"] = $"Нельзя удалить «{item.Name}» — есть записи на складе.";
            return RedirectToAction(nameof(Index));
        }

        db.Nomenclatures.Remove(item);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Номенклатура «{item.Name}» удалена.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult ExportExcel()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExportExcel(DateTime? from = null, DateTime? to = null)
    {
        var items = await db.Nomenclatures.AsNoTracking().OrderBy(n => n.Name).ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Номенклатура");

        ws.Cell(1, 1).Value = "№";
        ws.Cell(1, 2).Value = "Название";
        ws.Cell(1, 3).Value = "Оборудование";
        ws.Cell(1, 4).Value = "Ед. изм.";

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            ws.Cell(i + 2, 1).Value = i + 1;
            ws.Cell(i + 2, 2).Value = item.Name;
            ws.Cell(i + 2, 3).Value = item.IsEquipment ? "Да" : "Нет";
            ws.Cell(i + 2, 4).Value = item.Unit ?? "-";
        }

        ws.Columns().AdjustToContents();

        await using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;

        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"nomenclature-{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    [HttpGet]
    public IActionResult ImportExcel()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportExcel(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("", "Выберите файл для импорта");
            return View();
        }

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("", "Выберите Excel файл (.xlsx)");
            return View();
        }

        try
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;

            using var wb = new XLWorkbook(ms);
            var ws = wb.Worksheet(1);
            var rows = ws.RangeUsed()?.RowsUsed().Skip(1);

            if (rows == null) return BadRequest("Файл пустой");

            var existingNames = (await db.Nomenclatures.AsNoTracking()
                .Select(n => n.Name.ToLower())
                .ToListAsync())
                .ToHashSet();

            var imported = 0;
            var skipped = 0;
            foreach (var row in rows)
            {
                var name = row.Cell(1).GetString()?.Trim();
                if (string.IsNullOrWhiteSpace(name)) continue;

                if (existingNames.Contains(name.ToLower()))
                {
                    skipped++;
                    continue;
                }

                var isEquipment = row.Cell(2).GetString()?.ToLower() == "да";
                var unit = row.Cell(3).GetString()?.Trim();

                db.Nomenclatures.Add(new Nomenclature
                {
                    Name = name,
                    IsEquipment = isEquipment,
                    Unit = string.IsNullOrWhiteSpace(unit) ? "" : unit
                });
                existingNames.Add(name.ToLower());
                imported++;
            }

            await db.SaveChangesAsync();
            var msg = $"Импортировано {imported} записей";
            if (skipped > 0) msg += $", пропущено {skipped} (дубликаты)";
            TempData["Info"] = msg;
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Ошибка: {ex.Message}");
            return View();
        }
    }
}
