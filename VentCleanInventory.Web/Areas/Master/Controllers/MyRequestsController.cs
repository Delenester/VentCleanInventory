using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Areas.Master.Models.MyRequests;
using VentCleanInventory.Web.Data;
using VentCleanInventory.Web.Data.Entities;

namespace VentCleanInventory.Web.Areas.Master.Controllers;

[Area(MasterArea.Name)]
[Authorize(Roles = AppUserRole.Master)]
public class MyRequestsController(ApplicationDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Forbid();

        var requests = await db.StockTransactions.AsNoTracking()
            .Include(r => r.WorkObject)
            .Where(r => r.UserId == userId && r.RequestStatusValue != null)
            .OrderByDescending(r => r.Date)
            .ToListAsync();

        return View(requests);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        return View(await BuildAsync(new RequestCreateViewModel()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RequestCreateViewModel model)
    {
        model = await BuildAsync(model);

        if (!ModelState.IsValid) return View(model);

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Forbid();

        var items = model.Lines
            .Where(l => l.NomenclatureId.HasValue && l.Quantity > 0)
            .Select(l =>
            {
                var nom = db.Nomenclatures.AsNoTracking().FirstOrDefault(n => n.Id == l.NomenclatureId!.Value);
                return new TransactionItemDto
                {
                    NomenclatureId = l.NomenclatureId!.Value,
                    NomenclatureName = nom?.Name,
                    Quantity = l.Quantity,
                };
            })
            .ToList();

        if (items.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Добавьте хотя бы одну позицию.");
            return View(model);
        }

        var req = new StockTransaction
        {
            TransactionType = TransactionType.Issue,
            UserId = userId,
            WorkObjectId = model.WorkObjectId!.Value,
            ClientId = model.ClientId,
            SupplierId = model.SupplierId,
            Date = DateTime.UtcNow,
            RequestStatusValue = RequestStatus.New,
            Note = $"Заявка от {User.Identity?.Name}",
        };
        req.SetItems(items);

        db.StockTransactions.Add(req);
        await db.SaveChangesAsync();

        TempData["Info"] = $"Заявка отправлена диспетчеру (№{req.Id}).";
        return RedirectToAction(nameof(Create));
    }

    private async Task<RequestCreateViewModel> BuildAsync(RequestCreateViewModel model)
    {
        model.Objects = await db.WorkObjects.AsNoTracking().OrderBy(o => o.Name).ToListAsync();
        model.Clients = await db.Organizations.AsNoTracking()
            .Where(o => o.Type == OrganizationType.Client)
            .OrderBy(c => c.Name)
            .ToListAsync();
        model.Suppliers = await db.Organizations.AsNoTracking()
            .Where(o => o.Type == OrganizationType.Supplier)
            .OrderBy(s => s.Name)
            .ToListAsync();
        model.Nomenclatures = await db.Nomenclatures.AsNoTracking().OrderBy(n => n.Name).ToListAsync();
        model.Lines ??= [];
        if (model.Lines.Count == 0) model.Lines.Add(new RequestCreateViewModel.Line());
        return model;
    }
}
