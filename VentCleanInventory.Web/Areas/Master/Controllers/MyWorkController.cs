using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Areas.Master.Models;
using VentCleanInventory.Web.Data;
using VentCleanInventory.Web.Data.Entities;
using VentCleanInventory.Web.Models;

namespace VentCleanInventory.Web.Areas.Master.Controllers;

[Area(MasterArea.Name)]
[Authorize(Roles = AppUserRole.Master)]
public class MyWorkController(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Forbid();

        var allLogs = await db.WorkLogs.AsNoTracking()
            .Include(w => w.WorkObject)
            .Where(w => w.MasterUserId == userId)
            .OrderByDescending(w => w.WorkDate)
            .ToListAsync();

        return View(new MyWorkViewModel
        {
            InProgress = allLogs.Where(w => !w.IsCompleted).ToList(),
            Completed = allLogs.Where(w => w.IsCompleted).ToList(),
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Forbid();

        var vm = await BuildDetailAsync(id);
        if (vm == null || vm.MasterName != (await userManager.FindByIdAsync(userId))?.FullName) return NotFound();

        ViewBag.BackUrl = Url.Action(nameof(Index));
        ViewBag.BackText = "К моим работам";

        return View("~/Views/Shared/WorkLogDetails.cshtml", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkCompleted(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Forbid();

        var log = await db.WorkLogs.FirstOrDefaultAsync(w => w.Id == id && w.MasterUserId == userId);
        if (log is null) return NotFound();

        log.IsCompleted = true;
        await db.SaveChangesAsync();

        TempData["Info"] = "Работа отмечена как выполненная.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadPhoto(int id, IFormFile photo)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Forbid();

        var log = await db.WorkLogs.FirstOrDefaultAsync(w => w.Id == id && w.MasterUserId == userId);
        if (log is null) return NotFound();

        if (photo is { Length: > 0 })
        {
            var ext = Path.GetExtension(photo.FileName).ToLowerInvariant();
            if (ext is ".jpg" or ".jpeg" or ".png")
            {
                var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "work");
                Directory.CreateDirectory(dir);
                var fileName = $"work-{log.Id}{ext}";
                var filePath = Path.Combine(dir, fileName);
                await using (var stream = new FileStream(filePath, FileMode.Create))
                    await photo.CopyToAsync(stream);
                log.PhotoPath = $"/uploads/work/{fileName}";
                log.IsCompleted = true;
                await db.SaveChangesAsync();

                TempData["Info"] = "Фото добавлено, работа завершена.";
            }
            else
            {
                TempData["Error"] = "Допустимы только JPG и PNG.";
            }
        }

        return RedirectToAction(nameof(Index));
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
