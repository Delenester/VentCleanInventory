using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Data;
using VentCleanInventory.Web.Data.Entities;

namespace VentCleanInventory.Web.Areas.Master.Controllers;

[Area(MasterArea.Name)]
[Authorize(Roles = AppUserRole.Master)]
public class DefectsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Forbid();

        var defects = await db.DefectReports.AsNoTracking()
            .Include(d => d.WorkObject)
            .Where(d => d.MasterUserId == userId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        return View(defects);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Forbid();

        ViewBag.WorkObjects = await db.WorkObjects.AsNoTracking().OrderBy(o => o.Name).ToListAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int workObjectId, string description, string? zoneName, IFormFile? photo)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Forbid();

        if (workObjectId <= 0 || string.IsNullOrWhiteSpace(description))
        {
            ViewBag.WorkObjects = await db.WorkObjects.AsNoTracking().OrderBy(o => o.Name).ToListAsync();
            TempData["Error"] = "Заполните все обязательные поля.";
            return View();
        }

        var report = new DefectReport
        {
            MasterUserId = userId,
            WorkObjectId = workObjectId,
            Description = description.Trim(),
            ZoneName = zoneName?.Trim(),
            CreatedAt = DateTime.UtcNow,
        };

        if (photo is { Length: > 0 })
        {
            var ext = Path.GetExtension(photo.FileName).ToLowerInvariant();
            if (ext is ".jpg" or ".jpeg" or ".png")
            {
                var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "defects");
                Directory.CreateDirectory(dir);
                var fileName = $"defect-{DateTime.UtcNow:yyyyMMddHHmmss}{ext}";
                var filePath = Path.Combine(dir, fileName);
                await using (var stream = new FileStream(filePath, FileMode.Create))
                    await photo.CopyToAsync(stream);
                report.PhotoPath = $"/uploads/defects/{fileName}";
            }
            else
            {
                ViewBag.WorkObjects = await db.WorkObjects.AsNoTracking().OrderBy(o => o.Name).ToListAsync();
                TempData["Error"] = "Допустимы только JPG и PNG.";
                return View();
            }
        }

        db.DefectReports.Add(report);
        await db.SaveChangesAsync();

        TempData["Success"] = "Отчёт о браке сохранён.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Forbid();

        var report = await db.DefectReports.FirstOrDefaultAsync(d => d.Id == id && d.MasterUserId == userId);
        if (report == null) return NotFound();

        db.DefectReports.Remove(report);
        await db.SaveChangesAsync();

        TempData["Success"] = "Отчёт удалён.";
        return RedirectToAction(nameof(Index));
    }
}
