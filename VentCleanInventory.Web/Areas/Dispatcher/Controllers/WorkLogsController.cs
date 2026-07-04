using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Data;
using VentCleanInventory.Web.Models;

namespace VentCleanInventory.Web.Areas.Dispatcher.Controllers;

[Area(DispatcherArea.Name)]
[Authorize(Roles = AppUserRole.Dispatcher)]
public class WorkLogsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1)
    {
        const int pageSize = 15;

        var query = db.WorkLogs.AsNoTracking()
            .Include(w => w.WorkObject)
            .OrderByDescending(w => w.WorkDate);

        var total = await query.CountAsync();
        var logs = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var userIds = logs.Select(w => w.MasterUserId).Distinct().ToList();
        var users = await userManager.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.UserName ?? "");

        ViewBag.Total = total;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);

        return View(logs.Select(w => new WorkLogListItem
        {
            Id = w.Id,
            ObjectName = w.WorkObject.Name,
            MasterName = users.GetValueOrDefault(w.MasterUserId, ""),
            WorkDate = w.WorkDate,
            ZoneName = w.ZoneName,
            IsCompleted = w.IsCompleted,
        }).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var vm = await BuildDetailAsync(id);
        if (vm == null) return NotFound();

        ViewBag.BackUrl = Url.Action(nameof(Index));
        ViewBag.BackText = "К списку работ";

        return View("~/Views/Shared/WorkLogDetails.cshtml", vm);
    }

    private async Task<WorkLogDetailViewModel?> BuildDetailAsync(int id)
    {
        var log = await db.WorkLogs.AsNoTracking()
            .Include(w => w.WorkObject)
            .FirstOrDefaultAsync(w => w.Id == id);
        if (log == null) return null;

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

        return new WorkLogDetailViewModel
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
        };
    }
}

public class WorkLogListItem
{
    public int Id { get; set; }
    public string ObjectName { get; set; } = "";
    public string MasterName { get; set; } = "";
    public DateTime WorkDate { get; set; }
    public string? ZoneName { get; set; }
    public bool IsCompleted { get; set; }
}
