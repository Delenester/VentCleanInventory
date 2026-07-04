using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Areas.Admin.Models.Objects;
using VentCleanInventory.Web.Data;
using VentCleanInventory.Web.Data.Entities;

namespace VentCleanInventory.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Администратор")]
public class ObjectsController(
    ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var items = await db.WorkObjects.AsNoTracking().OrderBy(o => o.Name).ToListAsync();
        return View(items);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ObjectCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        db.WorkObjects.Add(new WorkObject
        {
            Name = model.Name.Trim(),
            VentSystemType = model.VentSystemType?.Trim(),
            AccessDifficulty = model.AccessDifficulty?.Trim(),
            Distance = model.Distance?.Trim(),
        });

        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.WorkObjects.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
        if (item is null) return NotFound();

        return View(new ObjectEditViewModel
        {
            Id = item.Id,
            Name = item.Name,
            VentSystemType = item.VentSystemType,
            AccessDifficulty = item.AccessDifficulty,
            Distance = item.Distance,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ObjectEditViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var item = await db.WorkObjects.FirstOrDefaultAsync(o => o.Id == id);
        if (item is null) return NotFound();

        item.Name = model.Name.Trim();
        item.VentSystemType = model.VentSystemType?.Trim();
        item.AccessDifficulty = model.AccessDifficulty?.Trim();
        item.Distance = model.Distance?.Trim();

        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.WorkObjects.FindAsync(id);
        if (item is null) return NotFound();

        db.WorkObjects.Remove(item);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
