using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Areas.Client.Models;
using VentCleanInventory.Web.Data;
using VentCleanInventory.Web.Data.Entities;

namespace VentCleanInventory.Web.Areas.Client.Controllers;

[Area(ClientArea.Name)]
[Authorize(Roles = AppUserRole.Client)]
public class ClientRequestController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var user = await GetCurrentUserAsync();
        if (user?.OrganizationId == null || user.AccountType != AccountType.Client)
            return RedirectToAction("Index", "ClientHome");

        return View(new ClientRequestViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClientRequestViewModel model)
    {
        var user = await GetCurrentUserAsync();
        if (user?.OrganizationId == null || user.AccountType != AccountType.Client)
            return RedirectToAction("Index", "ClientHome");

        if (!ModelState.IsValid) return View(model);

        var workObject = new WorkObject
        {
            Name = model.ObjectName.Trim(),
            Address = model.ObjectAddress.Trim(),
        };
        db.WorkObjects.Add(workObject);
        await db.SaveChangesAsync();

        string? blueprintPath = null;
        if (model.BlueprintPhoto is { Length: > 0 })
        {
            var uploadsDir = Path.Combine("wwwroot", "uploads", "blueprints");
            Directory.CreateDirectory(uploadsDir);
            var ext = Path.GetExtension(model.BlueprintPhoto.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await model.BlueprintPhoto.CopyToAsync(stream);
            blueprintPath = $"/uploads/blueprints/{fileName}";
        }

        var estimatedCost = (model.Area ?? 0) * ServicePriceList.GetPrice(model.ServiceType);

        var req = new StockTransaction
        {
            TransactionType = TransactionType.Issue,
            UserId = user.Id,
            WorkObjectId = workObject.Id,
            ClientId = user.OrganizationId,
            Date = DateTime.UtcNow,
            RequestStatusValue = RequestStatus.New,
            Note = $"[{model.ServiceType}] {model.Description}",
            Area = model.Area,
            EstimatedCost = Math.Round(estimatedCost, 2),
            BlueprintPhotoPath = blueprintPath,
        };
        req.SetItems(new List<TransactionItemDto>());

        db.StockTransactions.Add(req);
        await db.SaveChangesAsync();

        // Notify managers
        var managers = await userManager.GetUsersInRoleAsync(AppUserRole.Manager);
        foreach (var m in managers)
        {
            db.Notifications.Add(new Notification
            {
                UserId = m.Id,
                Title = "Новая заявка",
                Message = $"Поступила новая заявка №{req.Id} от {user.FullName ?? user.UserName}",
                Link = "/Manager/Request",
            });
        }
        await db.SaveChangesAsync();

        TempData["Success"] = $"Заявка №{req.Id} отправлена. Ожидайте одобрения.";
        return RedirectToAction("Index", "ClientHome");
    }

    [HttpGet]
    public async Task<IActionResult> Index(RequestStatus? status, DateTime? from, DateTime? to, int page = 1)
    {
        var user = await GetCurrentUserAsync();
        if (user?.OrganizationId is not int orgId || user.AccountType != AccountType.Client)
            return RedirectToAction("Index", "ClientHome");

        const int pageSize = 20;

        var query = db.StockTransactions.AsNoTracking()
            .Include(r => r.WorkObject)
            .Where(r => r.ClientId == orgId && r.RequestStatusValue != null);

        if (status.HasValue)
            query = query.Where(r => r.RequestStatusValue == status.Value);

        if (from.HasValue)
            query = query.Where(r => r.Date >= from.Value);

        if (to.HasValue)
            query = query.Where(r => r.Date <= to.Value.AddDays(1));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var masterIds = items.Where(r => !string.IsNullOrEmpty(r.AssignedMasterId)).Select(r => r.AssignedMasterId!).Distinct().ToList();
        var masters = await userManager.Users.Where(u => masterIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.UserName ?? "");

        ViewBag.Masters = masters;
        ViewBag.Total = total;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
        ViewBag.FilterStatus = status;
        ViewBag.FilterFrom = from;
        ViewBag.FilterTo = to;

        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user?.OrganizationId is not int orgId || user.AccountType != AccountType.Client)
            return RedirectToAction("Index", "ClientHome");

        var req = await db.StockTransactions.AsNoTracking()
            .Include(r => r.WorkObject)
            .Include(r => r.Client)
            .FirstOrDefaultAsync(r => r.Id == id && r.ClientId == orgId);

        if (req == null) return NotFound();

        var masterName = "";
        if (!string.IsNullOrWhiteSpace(req.AssignedMasterId))
        {
            var master = await userManager.FindByIdAsync(req.AssignedMasterId);
            if (master != null) masterName = master.FullName ?? master.UserName ?? "";
        }

        var serviceType = "";
        var description = "";
        if (!string.IsNullOrWhiteSpace(req.Note) && req.Note.StartsWith("["))
        {
            var idx = req.Note.IndexOf(']');
            if (idx > 0)
            {
                serviceType = req.Note[1..idx];
                if (idx + 2 < req.Note.Length)
                    description = req.Note[(idx + 2)..];
            }
        }

        return View(new RequestDetailsViewModel
        {
            Request = req,
            MasterName = masterName,
            ServiceType = serviceType,
            Description = description,
            ClientOrg = req.Client,
        });
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return userId is null ? null : await userManager.FindByIdAsync(userId);
    }
}
