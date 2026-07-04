using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Areas.Admin.Models.Clients;
using VentCleanInventory.Web.Data;
using VentCleanInventory.Web.Data.Entities;

namespace VentCleanInventory.Web.Areas.Admin.Controllers;

[Area(AdminArea.Name)]
[Authorize(Roles = AppUserRole.Admin)]
public class ClientsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var items = await db.Organizations.AsNoTracking()
            .Where(o => o.Type == OrganizationType.Client)
            .OrderBy(c => c.Name)
            .ToListAsync();
        return View(items);
    }

    [HttpGet]
    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClientCreateViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        if (await db.Organizations.AnyAsync(c => c.Type == OrganizationType.Client && c.Unp == model.Unp.Trim()))
        {
            ModelState.AddModelError(nameof(model.Unp), "Клиент с таким УНП уже существует.");
            return View(model);
        }

        db.Organizations.Add(new Organization
        {
            Type = OrganizationType.Client,
            Unp = model.Unp.Trim(),
            Name = model.OrganizationName.Trim(),
            LegalAddress = model.LegalAddress?.Trim(),
            ContactInfo = model.ContactInfo?.Trim(),
        });

        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.Organizations.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.Type == OrganizationType.Client);
        if (item is null) return NotFound();

        return View(new ClientEditViewModel
        {
            Id = item.Id,
            Unp = item.Unp ?? "",
            OrganizationName = item.Name,
            LegalAddress = item.LegalAddress,
            ContactInfo = item.ContactInfo,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ClientEditViewModel model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);

        if (await db.Organizations.AnyAsync(c => c.Type == OrganizationType.Client && c.Unp == model.Unp.Trim() && c.Id != id))
        {
            ModelState.AddModelError(nameof(model.Unp), "Клиент с таким УНП уже существует.");
            return View(model);
        }

        var item = await db.Organizations.FirstOrDefaultAsync(c => c.Id == id && c.Type == OrganizationType.Client);
        if (item is null) return NotFound();

        item.Unp = model.Unp.Trim();
        item.Name = model.OrganizationName.Trim();
        item.LegalAddress = model.LegalAddress?.Trim();
        item.ContactInfo = model.ContactInfo?.Trim();

        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.Organizations.FirstOrDefaultAsync(c => c.Id == id && c.Type == OrganizationType.Client);
        if (item is null) return NotFound();

        db.Organizations.Remove(item);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
