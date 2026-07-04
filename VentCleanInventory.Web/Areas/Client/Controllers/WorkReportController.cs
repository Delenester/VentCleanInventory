using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Areas.Client.Models;
using VentCleanInventory.Web.Data;
using VentCleanInventory.Web.Data.Entities;
using VentCleanInventory.Web.Models;

namespace VentCleanInventory.Web.Areas.Client.Controllers;

[Area(ClientArea.Name)]
[Authorize(Roles = AppUserRole.Client)]
public class WorkReportController(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(DateTime? from = null, DateTime? to = null)
    {
        var user = await GetCurrentUserAsync();
        if (user?.OrganizationId is not int orgId || user.AccountType != AccountType.Client)
            return View(new ClientWorkReportViewModel());

        var t = (to ?? DateTime.UtcNow.Date).Date.AddDays(1).AddTicks(-1);
        var f = (from ?? DateTime.UtcNow.Date.AddDays(-30)).Date;
        if (f > t) (f, t) = (t.Date, f.Date.AddDays(1).AddTicks(-1));

        var clientObjectIds = await db.StockTransactions.AsNoTracking()
            .Where(r => r.ClientId == orgId && r.WorkObjectId != null)
            .Select(r => r.WorkObjectId!.Value)
            .Distinct()
            .ToListAsync();

        var logs = await db.WorkLogs.AsNoTracking()
            .Include(w => w.WorkObject)
            .Where(w => clientObjectIds.Contains(w.WorkObjectId) && w.WorkDate >= f && w.WorkDate <= t)
            .OrderByDescending(w => w.WorkDate)
            .ToListAsync();

        var userIds = logs.Select(w => w.MasterUserId).Distinct().ToList();
        var users = await userManager.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.UserName ?? "");

        var rows = logs.Select(l => new WorkLogSummary
        {
            WorkLogId = l.Id,
            ObjectName = l.WorkObject.Name,
            ZoneName = l.ZoneName,
            MasterName = users.GetValueOrDefault(l.MasterUserId, ""),
            WorkDate = l.WorkDate,
            Meters = l.Meters,
            Grids = l.Grids,
            MaterialsUsed = l.MaterialsUsed,
            PhotoPath = l.PhotoPath,
        }).ToList();

        return View(new ClientWorkReportViewModel { From = f, To = t, Rows = rows });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user?.OrganizationId is not int orgId || user.AccountType != AccountType.Client)
            return NotFound();

        var clientObjectIds = await db.StockTransactions.AsNoTracking()
            .Where(r => r.ClientId == orgId && r.WorkObjectId != null)
            .Select(r => r.WorkObjectId!.Value)
            .Distinct()
            .ToListAsync();

        var log = await db.WorkLogs.AsNoTracking()
            .Include(w => w.WorkObject)
            .FirstOrDefaultAsync(w => w.Id == id && clientObjectIds.Contains(w.WorkObjectId));

        if (log is null) return NotFound();

        var master = await userManager.FindByIdAsync(log.MasterUserId);
        var checklist = await db.WorkChecklists.AsNoTracking()
            .Where(c => c.WorkLogId == id).ToListAsync();
        var relatedRequest = await db.StockTransactions.AsNoTracking()
            .Where(r => r.WorkLogId == id)
            .Select(r => new { r.Id, r.RequestStatusValue, r.ContractNumber })
            .FirstOrDefaultAsync();
        var defects = await db.DefectReports.AsNoTracking()
            .Where(d => d.WorkObjectId == log.WorkObjectId && d.MasterUserId == log.MasterUserId)
            .ToListAsync();

        ViewBag.BackUrl = Url.Action("Index", "ClientHome");
        ViewBag.BackText = "На главную";

        return View("~/Views/Shared/WorkLogDetails.cshtml", new WorkLogDetailViewModel
        {
            Id = log.Id,
            ObjectName = log.WorkObject.Name,
            ObjectAddress = log.WorkObject.Address ?? "",
            MasterName = master?.FullName ?? master?.UserName ?? "",
            WorkDate = log.WorkDate,
            ZoneName = log.ZoneName,
            Description = log.Description,
            MaterialsUsed = log.MaterialsUsed,
            Meters = log.Meters,
            Grids = log.Grids,
            PhotoPath = log.PhotoPath,
            IsCompleted = log.IsCompleted,
            ChecklistDone = log.ChecklistDone,
            Checklist = checklist.Select(c => new ChecklistItem
            {
                ItemName = c.ItemName,
                IsDone = c.IsDone,
                Note = c.Note,
                PhotoPath = c.PhotoPath,
            }).ToList(),
            RelatedRequestId = relatedRequest?.Id,
            RelatedRequestStatus = relatedRequest?.RequestStatusValue?.ToString(),
            ContractNumber = relatedRequest?.ContractNumber,
            Defects = defects.Select(d => new DefectItem
            {
                Id = d.Id,
                Description = d.Description,
                ZoneName = d.ZoneName,
                PhotoPath = d.PhotoPath,
                CreatedAt = d.CreatedAt,
            }).ToList(),
        });
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return userId is null ? null : await userManager.FindByIdAsync(userId);
    }
}
