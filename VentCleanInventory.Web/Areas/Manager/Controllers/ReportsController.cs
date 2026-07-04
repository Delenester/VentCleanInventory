using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Areas.Manager.Models.Reports;
using VentCleanInventory.Web.Data;
using VentCleanInventory.Web.Services;

namespace VentCleanInventory.Web.Areas.Manager.Controllers;

[Area(ManagerArea.Name)]
[Authorize(Roles = $"{AppUserRole.Manager},{AppUserRole.Dispatcher}")]
public class ReportsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> CostsByObjects(DateTime? from = null, DateTime? to = null)
    {
        var (f, t) = NormalizeRange(from, to);

        // Work metrics by object from WorkLogs
        var work = await db.WorkLogs.AsNoTracking()
            .Where(w => w.WorkDate >= f && w.WorkDate <= t)
            .Include(w => w.WorkObject)
            .ToListAsync();

        // Costs per object from linked write-offs (WorkObjectId filled by master consumption)
        var writeOffTxs = await db.StockTransactions.AsNoTracking()
            .Where(tx => tx.TransactionType == TransactionType.WriteOff && tx.Date >= f && tx.Date <= t && tx.WorkObjectId != null)
            .ToListAsync();

        var costsByObject = StockTransactionReportHelper.ExpandItems(writeOffTxs)
            .GroupBy(x => x.Tx.WorkObjectId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(x => x.Item.UnitPrice == null ? 0 : x.Item.Quantity * x.Item.UnitPrice.Value));

        var objects = await db.WorkObjects.AsNoTracking().OrderBy(o => o.Name).ToListAsync();

        var rows = objects.Select(o =>
        {
            var wlogs = work.Where(w => w.WorkObjectId == o.Id).ToList();
            var meters = wlogs.Sum(w => w.Meters ?? 0);
            var grids = wlogs.Sum(w => w.Grids ?? 0);
            return new CostsByObjectsViewModel.Row
            {
                ObjectName = o.Name,
                TotalCost = costsByObject.GetValueOrDefault(o.Id),
                TotalMeters = meters,
                TotalGrids = grids,
            };
        }).ToList();

        rows = rows.OrderByDescending(r => r.TotalCost).ThenBy(r => r.ObjectName).ToList();

        return View(new CostsByObjectsViewModel { From = f, To = t, Rows = rows });
    }

    [HttpGet]
    public async Task<IActionResult> CostsByObjectsExcel(DateTime? from = null, DateTime? to = null)
    {
        var view = await CostsByObjects(from, to) as ViewResult;
        var model = view?.Model as CostsByObjectsViewModel;
        if (model is null) return BadRequest();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Затраты");
        ws.Cell(1, 1).Value = "Объект";
        ws.Cell(1, 2).Value = "Сумма затрат";
        ws.Cell(1, 3).Value = "Погонные метры";
        ws.Cell(1, 4).Value = "Решётки";

        for (var i = 0; i < model.Rows.Count; i++)
        {
            var r = model.Rows[i];
            ws.Cell(i + 2, 1).Value = r.ObjectName;
            ws.Cell(i + 2, 2).Value = r.TotalCost;
            ws.Cell(i + 2, 3).Value = r.TotalMeters;
            ws.Cell(i + 2, 4).Value = r.TotalGrids;
        }

        ws.Columns().AdjustToContents();

        await using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"costs-by-objects-{model.From:yyyyMMdd}-{model.To:yyyyMMdd}.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> EquipmentUtilization(DateTime? from = null, DateTime? to = null)
    {
        var (f, t) = NormalizeRange(from, to);

        var issueTxs = await db.StockTransactions.AsNoTracking()
            .Where(tx => tx.TransactionType == TransactionType.Issue && tx.Date >= f && tx.Date <= t)
            .ToListAsync();
        var issues = StockTransactionReportHelper.ExpandItems(issueTxs)
            .Select(x => new { x.Tx.Date, x.Item.InventoryItemId })
            .ToList();

        var returnTxs = await db.StockTransactions.AsNoTracking()
            .Where(tx => tx.TransactionType == TransactionType.Return && tx.Date >= f && tx.Date <= t)
            .ToListAsync();
        var returns = StockTransactionReportHelper.ExpandItems(returnTxs)
            .Select(x => new { x.Tx.Date, x.Item.InventoryItemId })
            .ToList();

        var items = await db.InventoryItems.AsNoTracking()
            .Include(ii => ii.Nomenclature)
            .Where(ii => ii.Nomenclature.IsEquipment && ii.SerialNumber != null)
            .ToListAsync();

        var rows = new List<EquipmentUtilizationViewModel.Row>();
        foreach (var item in items)
        {
            var issueDates = issues.Where(i => i.InventoryItemId == item.Id).Select(i => i.Date).OrderBy(d => d).ToList();
            var returnDates = returns.Where(r => r.InventoryItemId == item.Id).Select(r => r.Date).OrderBy(d => d).ToList();

            // naive pairing: by order
            var pairs = Math.Min(issueDates.Count, returnDates.Count);
            var days = new List<double>();
            for (var i = 0; i < pairs; i++)
            {
                var delta = (returnDates[i] - issueDates[i]).TotalDays;
                if (delta >= 0) days.Add(delta);
            }

            rows.Add(new EquipmentUtilizationViewModel.Row
            {
                Name = item.Nomenclature.Name,
                SerialNumber = item.SerialNumber!,
                IssueCount = issueDates.Count,
                AvgDaysWithMaster = days.Count == 0 ? 0 : days.Average(),
            });
        }

        rows = rows.OrderByDescending(r => r.IssueCount).ThenBy(r => r.Name).ToList();
        return View(new EquipmentUtilizationViewModel { From = f, To = t, Rows = rows });
    }

    [HttpGet]
    public async Task<IActionResult> EquipmentUtilizationExcel(DateTime? from = null, DateTime? to = null)
    {
        var view = await EquipmentUtilization(from, to) as ViewResult;
        var model = view?.Model as EquipmentUtilizationViewModel;
        if (model is null) return BadRequest();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Оборудование");
        ws.Cell(1, 1).Value = "Оборудование";
        ws.Cell(1, 2).Value = "Инв. №";
        ws.Cell(1, 3).Value = "Выдач";
        ws.Cell(1, 4).Value = "Среднее дней";

        for (var i = 0; i < model.Rows.Count; i++)
        {
            var r = model.Rows[i];
            ws.Cell(i + 2, 1).Value = r.Name;
            ws.Cell(i + 2, 2).Value = r.SerialNumber;
            ws.Cell(i + 2, 3).Value = r.IssueCount;
            ws.Cell(i + 2, 4).Value = r.AvgDaysWithMaster;
        }

        ws.Columns().AdjustToContents();

        await using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"equipment-{model.From:yyyyMMdd}-{model.To:yyyyMMdd}.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> ForecastPurchases(int months = 3)
    {
        months = Math.Clamp(months, 1, 36);
        var to = DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);
        var from = to.AddMonths(-months);

        // usage based on write-off transactions
        var writeOffTxs = await db.StockTransactions.AsNoTracking()
            .Where(tx => tx.TransactionType == TransactionType.WriteOff && tx.Date >= from && tx.Date <= to)
            .ToListAsync();

        var invIds = writeOffTxs.SelectMany(t => t.GetItems()).Select(i => i.InventoryItemId).Distinct().ToList();
        var invMap = await db.InventoryItems.AsNoTracking()
            .Include(i => i.Nomenclature)
            .Where(i => invIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id);

        var usage = StockTransactionReportHelper.ExpandItems(writeOffTxs)
            .Where(x => x.Item.InventoryItemId.HasValue && invMap.ContainsKey(x.Item.InventoryItemId.Value))
            .Select(x =>
            {
                var inv = invMap[x.Item.InventoryItemId!.Value];
                return new
                {
                    inv.NomenclatureId,
                    x.Item.Quantity,
                    inv.Nomenclature.Name,
                    inv.Nomenclature.IsEquipment,
                };
            })
            .Where(x => !x.IsEquipment)
            .ToList();

        var avgByMat = usage
            .GroupBy(x => new { x.NomenclatureId, x.Name })
            .Select(g => new
            {
                g.Key.NomenclatureId,
                g.Key.Name,
                AvgMonthly = g.Sum(x => x.Quantity) / months,
            })
            .ToList();

        var centralWarehouseId = await db.Warehouses.AsNoTracking()
            .Where(w => w.Type == WarehouseType.Central)
            .Select(w => w.Id)
            .FirstOrDefaultAsync();

        var centralBalances = await db.StockBalances.AsNoTracking()
            .Where(b => b.WarehouseId == centralWarehouseId && b.Quantity > 0 && !b.InventoryItem.Nomenclature.IsEquipment)
            .GroupBy(b => new { b.InventoryItem.NomenclatureId, b.InventoryItem.Nomenclature.Name })
            .Select(g => new { g.Key.NomenclatureId, g.Key.Name, Qty = g.Sum(x => x.Quantity) })
            .ToListAsync();

        var balanceMap = centralBalances.ToDictionary(x => x.NomenclatureId, x => x.Qty);

        var rows = avgByMat
            .Select(x =>
            {
                var bal = balanceMap.GetValueOrDefault(x.NomenclatureId);
                var rec = x.AvgMonthly;
                return new ForecastPurchasesViewModel.Row
                {
                    MaterialName = x.Name,
                    AvgMonthlyUsage = x.AvgMonthly,
                    RecommendedNextMonth = rec,
                    CentralBalance = bal,
                };
            })
            .Where(r => r.CentralBalance < r.RecommendedNextMonth)
            .OrderByDescending(r => (r.RecommendedNextMonth - r.CentralBalance))
            .ToList();

        return View(new ForecastPurchasesViewModel { Months = months, Rows = rows });
    }

    [HttpGet]
    public async Task<IActionResult> ForecastPurchasesExcel(int months = 3)
    {
        var view = await ForecastPurchases(months) as ViewResult;
        var model = view?.Model as ForecastPurchasesViewModel;
        if (model is null) return BadRequest();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Прогноз");
        ws.Cell(1, 1).Value = "Материал";
        ws.Cell(1, 2).Value = "Среднемес. расход";
        ws.Cell(1, 3).Value = "Рекомендация";
        ws.Cell(1, 4).Value = "Остаток";

        for (var i = 0; i < model.Rows.Count; i++)
        {
            var r = model.Rows[i];
            ws.Cell(i + 2, 1).Value = r.MaterialName;
            ws.Cell(i + 2, 2).Value = r.AvgMonthlyUsage;
            ws.Cell(i + 2, 3).Value = r.RecommendedNextMonth;
            ws.Cell(i + 2, 4).Value = r.CentralBalance;
        }

        ws.Columns().AdjustToContents();

        await using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"forecast-{months}months.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> CostsTrend(DateTime? from = null, DateTime? to = null)
    {
        var (f, t) = NormalizeRange(from, to);

        var trendTxs = await db.StockTransactions.AsNoTracking()
            .Where(tx => tx.TransactionType == TransactionType.WriteOff && tx.Date >= f && tx.Date <= t)
            .ToListAsync();

        var points = StockTransactionReportHelper.ExpandItems(trendTxs)
            .Select(x => new
            {
                Date = x.Tx.Date,
                Cost = x.Item.UnitPrice == null ? 0 : x.Item.Quantity * x.Item.UnitPrice.Value,
            })
            .GroupBy(x => new { x.Date.Year, x.Date.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Cost = g.Sum(x => x.Cost) })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToList();

        var result = points.Select(p => new CostsTrendViewModel.Point
        {
            Month = $"{p.Year:D4}-{p.Month:D2}",
            Cost = p.Cost
        }).ToList();

        return View(new CostsTrendViewModel { From = f, To = t, Points = result });
    }

    [HttpGet]
    public async Task<IActionResult> CostsTrendExcel(DateTime? from = null, DateTime? to = null)
    {
        var view = await CostsTrend(from, to) as ViewResult;
        var model = view?.Model as CostsTrendViewModel;
        if (model is null) return BadRequest();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Динамика");
        ws.Cell(1, 1).Value = "Месяц";
        ws.Cell(1, 2).Value = "Затраты";

        for (var i = 0; i < model.Points.Count; i++)
        {
            var p = model.Points[i];
            ws.Cell(i + 2, 1).Value = p.Month;
            ws.Cell(i + 2, 2).Value = p.Cost;
        }

        ws.Columns().AdjustToContents();

        await using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"costs-trend-{model.From:yyyyMMdd}-{model.To:yyyyMMdd}.xlsx");
    }

    private static (DateTime from, DateTime to) NormalizeRange(DateTime? from, DateTime? to)
    {
        var t = (to ?? DateTime.UtcNow.Date).Date.AddDays(1).AddTicks(-1);
        var f = (from ?? DateTime.UtcNow.Date.AddDays(-30)).Date;
        if (f > t) (f, t) = (t.Date, f.Date.AddDays(1).AddTicks(-1));
        return (f, t);
    }

    [HttpGet]
    public async Task<IActionResult> ExportAll()
    {
        var to = DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);
        var from = to.AddMonths(-3);

        var exportTxs = await db.StockTransactions.AsNoTracking()
            .Where(tx => tx.TransactionType == TransactionType.WriteOff && tx.Date >= from && tx.Date <= to)
            .ToListAsync();

        var exportInvIds = exportTxs.SelectMany(t => t.GetItems()).Select(i => i.InventoryItemId).Distinct().ToList();
        var exportInvMap = await db.InventoryItems.AsNoTracking()
            .Include(i => i.Nomenclature)
            .Where(i => exportInvIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id);

        var costs = StockTransactionReportHelper.ExpandItems(exportTxs)
            .Where(x => x.Item.InventoryItemId.HasValue && exportInvMap.ContainsKey(x.Item.InventoryItemId.Value))
            .Select(x =>
            {
                var inv = exportInvMap[x.Item.InventoryItemId!.Value];
                return new
                {
                    x.Tx.Date,
                    Item = inv.Nomenclature.Name,
                    Qty = x.Item.Quantity,
                    Price = x.Item.UnitPrice ?? 0,
                    Cost = (x.Item.UnitPrice ?? 0) * x.Item.Quantity,
                };
            })
            .OrderBy(x => x.Date)
            .ToList();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Расходы");

        ws.Cell(1, 1).Value = "Дата";
        ws.Cell(1, 2).Value = "Материал";
        ws.Cell(1, 3).Value = "Кол-во";
        ws.Cell(1, 4).Value = "Цена";
        ws.Cell(1, 5).Value = "Сумма";

        for (var i = 0; i < costs.Count; i++)
        {
            var c = costs[i];
            ws.Cell(i + 2, 1).Value = c.Date.ToString("dd.MM.yyyy");
            ws.Cell(i + 2, 2).Value = c.Item;
            ws.Cell(i + 2, 3).Value = c.Qty;
            ws.Cell(i + 2, 4).Value = c.Price;
            ws.Cell(i + 2, 5).Value = c.Cost;
        }

        ws.Columns().AdjustToContents();

        await using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"expenses-{from:yyyyMMdd}-{to:yyyyMMdd}.xlsx");
    }

    [HttpGet]
    public IActionResult ImportData()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportData(IFormFile file)
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

            if (rows == null)
            {
                ModelState.AddModelError("", "Файл пустой");
                return View();
            }

            TempData["Info"] = "Импорт данных выполнен";
            return RedirectToAction("Index", "ManagerHome");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Ошибка: {ex.Message}");
            return View();
        }
    }

    [HttpGet]
    public async Task<IActionResult> WorkReport(DateTime? from = null, DateTime? to = null)
    {
        var (f, t) = NormalizeRange(from, to);

        var logs = await db.WorkLogs.AsNoTracking()
            .Include(w => w.WorkObject)
            .Where(w => w.WorkDate >= f && w.WorkDate <= t)
            .OrderByDescending(w => w.WorkDate)
            .ToListAsync();

        var userIds = logs.Select(w => w.MasterUserId).Distinct().ToList();
        var users = await userManager.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.UserName ?? "");

        var rows = logs.Select(w => new WorkReportViewModel.Row
        {
            MasterName = users.GetValueOrDefault(w.MasterUserId, ""),
            ObjectName = w.WorkObject.Name,
            ZoneName = w.ZoneName,
            WorkDate = w.WorkDate,
            Meters = w.Meters,
            Grids = w.Grids,
            MaterialsUsed = w.MaterialsUsed,
            PhotoPath = w.PhotoPath,
        }).ToList();

        return View(new WorkReportViewModel { From = f, To = t, Rows = rows });
    }
}

