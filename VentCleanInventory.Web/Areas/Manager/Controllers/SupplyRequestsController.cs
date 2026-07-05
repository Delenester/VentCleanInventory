using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Data;
using VentCleanInventory.Web.Data.Entities;

namespace VentCleanInventory.Web.Areas.Manager.Controllers;

[Area(ManagerArea.Name)]
[Authorize(Roles = AppUserRole.Manager)]
public class SupplyRequestsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(SupplyRequestStatus? status = null)
    {
        var query = db.SupplyRequests.AsNoTracking()
            .Include(r => r.Organization)
            .Include(r => r.Items).ThenInclude(i => i.Nomenclature)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        var items = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        return View(items);
    }
}
