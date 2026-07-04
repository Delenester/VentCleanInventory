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
public class ClientHomeController(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var user = await GetCurrentUserAsync();
        if (user?.OrganizationId is not int orgId ||
            user.AccountType != AccountType.Client)
        {
            return View(new ClientPortalViewModel());
        }

        var client = await db.Organizations.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == orgId && c.Type == OrganizationType.Client);

        var requests = await db.StockTransactions.AsNoTracking()
            .Include(r => r.WorkObject)
            .Where(r => r.ClientId == orgId && r.RequestStatusValue != null)
            .OrderByDescending(r => r.Date)
            .Take(20)
            .ToListAsync();

        var requestMasterIds = requests.Where(r => !string.IsNullOrEmpty(r.AssignedMasterId)).Select(r => r.AssignedMasterId!).Distinct().ToList();
        var requestMasters = await userManager.Users.Where(u => requestMasterIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.UserName ?? "");

        var clientObjectIds = requests.Where(r => r.WorkObjectId.HasValue).Select(r => r.WorkObjectId!.Value).Distinct().ToList();
        var logs = await db.WorkLogs.AsNoTracking()
            .Include(w => w.WorkObject)
            .Where(w => clientObjectIds.Contains(w.WorkObjectId))
            .OrderByDescending(w => w.WorkDate)
            .Take(20)
            .ToListAsync();

        var userIds = logs.Select(w => w.MasterUserId).Distinct().ToList();
        var users = await userManager.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.UserName ?? "");

        var completedWork = logs.Select(w => new WorkLogSummary
        {
            WorkLogId = w.Id,
            ObjectName = w.WorkObject.Name,
            ZoneName = w.ZoneName,
            MasterName = users.GetValueOrDefault(w.MasterUserId, ""),
            WorkDate = w.WorkDate,
            Meters = w.Meters,
            Grids = w.Grids,
            MaterialsUsed = w.MaterialsUsed,
            PhotoPath = w.PhotoPath,
        }).ToList();

        var notifications = await db.Notifications.AsNoTracking()
            .Where(n => n.UserId == user.Id && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Take(20)
            .ToListAsync();

        return View(new ClientPortalViewModel
        {
            Client = client,
            Requests = requests,
            RequestMasterNames = requestMasters,
            CompletedWork = completedWork,
            Notifications = notifications,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Forbid();

        var n = await db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == user.Id);
        if (n != null)
        {
            n.IsRead = true;
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Forbid();

        await db.Notifications
            .Where(n => n.UserId == user.Id && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmContract(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user?.OrganizationId is not int orgId) return Forbid();

        var req = await db.StockTransactions.FirstOrDefaultAsync(r => r.Id == id && r.ClientId == orgId);
        if (req == null) return NotFound();

        if (req.RequestStatusValue != RequestStatus.Approved) return BadRequest();

        req.RequestStatusValue = RequestStatus.ClientConfirmed;
        await db.SaveChangesAsync();

        // Notify manager users
        var managers = await userManager.GetUsersInRoleAsync(AppUserRole.Manager);
        foreach (var m in managers)
        {
            db.Notifications.Add(new Notification
            {
                UserId = m.Id,
                Title = "Клиент подтвердил договор",
                Message = $"Клиент подтвердил договор по заявке №{req.Id}",
                Link = "/Manager/Request",
            });
        }
        await db.SaveChangesAsync();

        TempData["Success"] = "Договор подтверждён. Ожидайте выполнения работ.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelRequest(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user?.OrganizationId is not int orgId) return Forbid();

        var req = await db.StockTransactions.FirstOrDefaultAsync(r => r.Id == id && r.ClientId == orgId);
        if (req == null) return NotFound();

        if (req.RequestStatusValue != RequestStatus.New) return BadRequest("Можно отменить только новую заявку.");

        req.RequestStatusValue = RequestStatus.Rejected;
        await db.SaveChangesAsync();

        TempData["Success"] = $"Заявка №{id} отменена.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return userId is null ? null : await userManager.FindByIdAsync(userId);
    }
}
