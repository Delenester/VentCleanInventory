using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Data;

namespace VentCleanInventory.Web.Areas.Manager.Controllers;

[Area(ManagerArea.Name)]
[Authorize(Roles = AppUserRole.Manager)]
public class ClientsController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var clients = await userManager.Users
            .Where(u => u.AccountType == AccountType.Client)
            .OrderBy(u => u.FullName)
            .ToListAsync();

        return View(clients);
    }

    public async Task<IActionResult> Details(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();

        var user = await userManager.FindByIdAsync(id);
        if (user == null || user.AccountType != AccountType.Client) return NotFound();

        var org = user.OrganizationId.HasValue
            ? await db.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == user.OrganizationId)
            : null;

        ViewBag.Organization = org;

        var requests = await db.StockTransactions.AsNoTracking()
            .Where(t => t.ClientId == user.OrganizationId)
            .OrderByDescending(t => t.Date)
            .ToListAsync();

        ViewBag.Requests = requests;

        return View(user);
    }
}
