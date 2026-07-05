using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Areas.Supplier.Models;
using VentCleanInventory.Web.Data;
using VentCleanInventory.Web.Data.Entities;
using VentCleanInventory.Web.Services;

namespace VentCleanInventory.Web.Areas.Supplier.Controllers;

[Area(SupplierArea.Name)]
[Authorize(Roles = AppUserRole.Supplier)]
public class SupplyRequestsController(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext db,
    NotificationService notificationService) : Controller
{
    public async Task<IActionResult> Index(SupplyRequestStatus? status, DateTime? from, DateTime? to, int page = 1)
    {
        var user = await GetCurrentUserAsync();
        if (user?.OrganizationId is not int orgId || user.AccountType != AccountType.Supplier)
        {
            ViewBag.Total = 0;
            ViewBag.Page = 1;
            ViewBag.PageSize = 0;
            ViewBag.TotalPages = 0;
            ViewBag.FilterStatus = status;
            ViewBag.FilterFrom = from;
            ViewBag.FilterTo = to;
            return View(Array.Empty<SupplyRequest>());
        }

        const int pageSize = 20;
        var query = db.SupplyRequests.AsNoTracking()
            .Include(r => r.Items).ThenInclude(i => i.Nomenclature)
            .Where(r => r.OrganizationId == orgId && r.Status != SupplyRequestStatus.New);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        if (from.HasValue)
            query = query.Where(r => r.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(r => r.CreatedAt <= to.Value.AddDays(1));

        var total = await query.CountAsync();
        var requests = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Total = total;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
        ViewBag.FilterStatus = status;
        ViewBag.FilterFrom = from;
        ViewBag.FilterTo = to;

        return View(requests);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user?.OrganizationId is not int orgId) return Forbid();

        var req = await db.SupplyRequests.AsNoTracking()
            .Include(r => r.Organization)
            .Include(r => r.Items).ThenInclude(i => i.Nomenclature)
            .FirstOrDefaultAsync(r => r.Id == id && r.OrganizationId == orgId);

        if (req is null) return NotFound();

        return View(req);
    }

    [HttpGet]
    public async Task<IActionResult> Confirm(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user?.OrganizationId is not int orgId) return Forbid();

        var req = await db.SupplyRequests.AsNoTracking()
            .Include(r => r.Items).ThenInclude(i => i.Nomenclature)
            .FirstOrDefaultAsync(r => r.Id == id && r.OrganizationId == orgId);

        if (req is null || req.Status != SupplyRequestStatus.Sent) return NotFound();

        var model = new SupplyRequestConfirmViewModel
        {
            RequestId = req.Id,
            RequestNumber = req.Number,
            Note = req.Note,
            Items = req.Items.Select(i => new SupplyRequestConfirmViewModel.Line
            {
                SupplyRequestItemId = i.Id,
                NomenclatureName = i.Nomenclature?.Name ?? "",
                Unit = i.Nomenclature?.Unit ?? "",
                OrderedQuantity = i.Quantity,
                ConfirmedQuantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Note = i.Note,
            }).ToList(),
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id, SupplyRequestConfirmViewModel model)
    {
        if (id != model.RequestId) return BadRequest();

        var user = await GetCurrentUserAsync();
        if (user?.OrganizationId is not int orgId) return Forbid();

        var req = await db.SupplyRequests
            .Include(r => r.Organization)
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == id && r.OrganizationId == orgId);

        if (req is null || req.Status != SupplyRequestStatus.Sent) return NotFound();

        var hasItems = model.Items.Any(i => i.ConfirmedQuantity > 0);

        if (!hasItems)
        {
            ModelState.AddModelError("", "Подтвердите хотя бы одну позицию.");
            return View(model);
        }

        foreach (var line in model.Items.Where(i => i.ConfirmedQuantity > 0))
        {
            var reqItem = req.Items.FirstOrDefault(i => i.Id == line.SupplyRequestItemId);
            if (reqItem is null) continue;

            if (line.ConfirmedQuantity > reqItem.Quantity)
            {
                ModelState.AddModelError("", $"По позиции «{reqItem.Nomenclature?.Name}» указано количество больше запрошенного ({reqItem.Quantity:N1}).");
                return View(model);
            }

            if (line.UnitPrice is null || line.UnitPrice <= 0)
            {
                ModelState.AddModelError("", $"По позиции «{reqItem.Nomenclature?.Name}» укажите цену поставки.");
                return View(model);
            }

            reqItem.ConfirmedQuantity = line.ConfirmedQuantity;
            reqItem.UnitPrice = line.UnitPrice;
            reqItem.Note = line.Note;
        }

        req.Status = SupplyRequestStatus.Confirmed;
        req.Note = model.Note;

        await notificationService.NotifyRoleAsync(userManager, AppUserRole.Admin,
            "Запрос на поставку подтверждён",
            $"Поставщик «{req.Organization?.Name}» подтвердил запрос №{req.Number}.",
            $"/Admin/SupplyRequests");

        await notificationService.NotifyRoleAsync(userManager, AppUserRole.Manager,
            "Запрос на поставку подтверждён",
            $"Поставщик «{req.Organization?.Name}» подтвердил запрос №{req.Number}.",
            $"/Admin/SupplyRequests");

        await db.SaveChangesAsync();
        TempData["Success"] = $"Запрос №{req.Number} подтверждён.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user?.OrganizationId is not int orgId) return Forbid();

        var req = await db.SupplyRequests.FirstOrDefaultAsync(r => r.Id == id && r.OrganizationId == orgId);
        if (req is null || req.Status != SupplyRequestStatus.Sent)
            return NotFound();

        req.Status = SupplyRequestStatus.Cancelled;
        await db.SaveChangesAsync();

        await notificationService.NotifyRoleAsync(userManager, AppUserRole.Dispatcher,
            "Запрос отклонён поставщиком",
            $"Поставщик отклонил запрос №{req.Number}.",
            "/Dispatcher/SupplyRequests");

        TempData["Success"] = $"Запрос №{req.Number} отклонён.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return userId is null ? null : await userManager.FindByIdAsync(userId);
    }
}
