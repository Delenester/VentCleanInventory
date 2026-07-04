using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Data;

namespace VentCleanInventory.Web.Areas.Manager.Controllers;

[Area(ManagerArea.Name)]
[Authorize(Roles = AppUserRole.Manager)]
public class ManagerHomeController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Forbid();

        var notifications = await db.Notifications.AsNoTracking()
            .Where(n => n.UserId == user.Id && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Take(20)
            .ToListAsync();

        return View(notifications);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id)
    {
        var user = await userManager.GetUserAsync(User);
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
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Forbid();

        await db.Notifications
            .Where(n => n.UserId == user.Id && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

        return RedirectToAction(nameof(Index));
    }
}

