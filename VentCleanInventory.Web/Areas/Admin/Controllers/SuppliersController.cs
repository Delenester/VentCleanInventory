using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Areas.Admin.Models.Suppliers;
using VentCleanInventory.Web.Data;
using VentCleanInventory.Web.Data.Entities;

namespace VentCleanInventory.Web.Areas.Admin.Controllers;

[Area(AdminArea.Name)]
[Authorize(Roles = AppUserRole.Admin)]
public class SuppliersController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var items = await db.Organizations.AsNoTracking()
            .Where(o => o.Type == OrganizationType.Supplier)
            .OrderBy(s => s.Name)
            .ToListAsync();
        return View(items);
    }

    [HttpGet]
    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SupplierCreateViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        db.Organizations.Add(new Organization
        {
            Type = OrganizationType.Supplier,
            Name = model.Name.Trim(),
            Unp = model.Unp?.Trim(),
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
            .FirstOrDefaultAsync(s => s.Id == id && s.Type == OrganizationType.Supplier);
        if (item is null) return NotFound();

        return View(new SupplierEditViewModel
        {
            Id = item.Id,
            Name = item.Name,
            Unp = item.Unp,
            LegalAddress = item.LegalAddress,
            ContactInfo = item.ContactInfo,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SupplierEditViewModel model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);

        var item = await db.Organizations.FirstOrDefaultAsync(s => s.Id == id && s.Type == OrganizationType.Supplier);
        if (item is null) return NotFound();

        item.Name = model.Name.Trim();
        item.Unp = model.Unp?.Trim();
        item.LegalAddress = model.LegalAddress?.Trim();
        item.ContactInfo = model.ContactInfo?.Trim();

        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.Organizations.FirstOrDefaultAsync(s => s.Id == id && s.Type == OrganizationType.Supplier);
        if (item is null) return NotFound();

        db.Organizations.Remove(item);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
