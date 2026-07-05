using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Data;
using VentCleanInventory.Web.Data.Entities;
using System.Text.Json;

namespace VentCleanInventory.Web.Areas.Master.Controllers;

[Area(MasterArea.Name)]
[Authorize(Roles = AppUserRole.Master)]
public class ChecklistController(ApplicationDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> ForWork(int workLogId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Forbid();

        var log = await db.WorkLogs.AsNoTracking()
            .Include(w => w.WorkObject)
            .FirstOrDefaultAsync(w => w.Id == workLogId && w.MasterUserId == userId);
        if (log == null) return NotFound();

        var items = await db.WorkChecklists.AsNoTracking()
            .Where(c => c.WorkLogId == workLogId)
            .OrderBy(c => c.Id)
            .ToListAsync();

        if (items.Count == 0)
        {
            items = GetDefaultChecklist().Select(name => new WorkChecklist
            {
                WorkLogId = workLogId,
                ItemName = name,
                IsDone = false,
            }).ToList();
            db.WorkChecklists.AddRange(items);
            await db.SaveChangesAsync();
        }

        ViewBag.WorkLog = log;
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(int workLogId, Dictionary<int, bool> checks, Dictionary<int, string>? notes)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Forbid();

        var log = await db.WorkLogs.FindAsync(workLogId);
        if (log == null || log.MasterUserId != userId) return NotFound();

        var items = await db.WorkChecklists
            .Where(c => c.WorkLogId == workLogId)
            .ToListAsync();

        foreach (var item in items)
        {
            if (checks.TryGetValue(item.Id, out var done))
                item.IsDone = done;
            if (notes != null && notes.TryGetValue(item.Id, out var note))
                item.Note = note;
        }

        log.ChecklistDone = items.All(i => i.IsDone);
        log.ChecklistData = JsonSerializer.Serialize(items.Select(i => new { i.ItemName, i.IsDone, i.Note }));

        await db.SaveChangesAsync();
        TempData["Success"] = "Чек-лист сохранён.";
        return RedirectToAction("Index", "MyWork");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadChecklistPhoto(int id, IFormFile photo)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Forbid();

        var item = await db.WorkChecklists.Include(c => c.WorkLog).FirstOrDefaultAsync(c => c.Id == id);
        if (item == null || item.WorkLog?.MasterUserId != userId) return NotFound();

        if (photo is { Length: > 0 })
        {
            var ext = Path.GetExtension(photo.FileName).ToLowerInvariant();
            if (ext is ".jpg" or ".jpeg" or ".png")
            {
                var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "checklist");
                Directory.CreateDirectory(dir);
                var fileName = $"check-{item.Id}{ext}";
                var filePath = Path.Combine(dir, fileName);
                await using (var stream = new FileStream(filePath, FileMode.Create))
                    await photo.CopyToAsync(stream);
                item.PhotoPath = $"/uploads/checklist/{fileName}";
                await db.SaveChangesAsync();
            }
        }

        return RedirectToAction("ForWork", new { workLogId = item.WorkLogId });
    }

    private static string[] GetDefaultChecklist() =>
    [
        "Вентиляция разобрана/открыта",
        "Каналы прочищены от загрязнений",
        "Решётки сняты и промыты",
        "Фильтры заменены/очищены",
        "Вентиляция собрана обратно",
        "Проверена тяга",
        "Рабочее место убрано",
        "Фотоотчёт сделан"
    ];
}
