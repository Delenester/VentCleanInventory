using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Data;
using VentCleanInventory.Web.Data.Entities;

namespace VentCleanInventory.Web.Areas.Manager.Controllers;

[Area(ManagerArea.Name)]
[Authorize(Roles = AppUserRole.Manager)]
public class RequestController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var requests = await db.StockTransactions.AsNoTracking()
            .Where(t => t.RequestStatusValue == RequestStatus.New || t.RequestStatusValue == RequestStatus.Approved
                || t.RequestStatusValue == RequestStatus.ClientConfirmed || t.RequestStatusValue == RequestStatus.Assigned)
            .Include(t => t.Client)
            .Include(t => t.WorkObject)
            .OrderByDescending(t => t.Date)
            .ToListAsync();

        var userIds = requests.Select(r => r.UserId).Distinct().ToList();
        var users = await userManager.Users.Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName);
        ViewBag.UserNames = users;

        return View(requests);
    }

    public async Task<IActionResult> Details(int id)
    {
        var req = await db.StockTransactions.AsNoTracking()
            .Include(t => t.Client)
            .Include(t => t.WorkObject)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (req == null) return NotFound();

        var user = await userManager.FindByIdAsync(req.UserId);
        ViewBag.ClientUser = user;
        ViewBag.MasterList = await userManager.GetUsersInRoleAsync(AppUserRole.Master);

        return View(req);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, decimal estimatedCost, string managerNote,
        string assignedMasterId, DateTime? plannedStartDate, DateTime? plannedEndDate)
    {
        var req = await db.StockTransactions.FindAsync(id);
        if (req == null) return NotFound();

        if (req.RequestStatusValue != RequestStatus.New && req.RequestStatusValue != RequestStatus.ClientConfirmed)
        {
            TempData["Error"] = "Заявку можно одобрить только со статусами «Новая» или «Договор подтверждён».";
            return RedirectToAction(nameof(Index));
        }

        req.RequestStatusValue = RequestStatus.Approved;
        req.EstimatedCost = estimatedCost;
        req.ManagerNote = managerNote?.Trim();
        req.AssignedMasterId = assignedMasterId;
        req.PlannedStartDate = plannedStartDate;
        req.PlannedEndDate = plannedEndDate;

        req.ContractNumber = $"Д-{DateTime.UtcNow:yyyyMMdd}-{req.Id}";

        await db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(req.UserId))
        {
            var masterName = "";
            if (!string.IsNullOrWhiteSpace(assignedMasterId))
            {
                var master = await userManager.FindByIdAsync(assignedMasterId);
                if (master != null) masterName = master.FullName ?? master.UserName ?? "";
            }
            db.Notifications.Add(new Notification
            {
                UserId = req.UserId,
                Title = "Заявка одобрена — требуется подтверждение договора",
                Message = $"Заявка №{req.Id} одобрена. Стоимость: {estimatedCost:N2} руб. Договор: {req.ContractNumber}." +
                    $"{(string.IsNullOrWhiteSpace(masterName) ? "" : $" Назначен мастер: {masterName}.")} Пожалуйста, подтвердите согласие с договором в личном кабинете.",
                Link = "/Client/ClientHome",
            });
            await db.SaveChangesAsync();
        }

        TempData["Success"] = $"Заявка №{id} одобрена. Договор: {req.ContractNumber}";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string managerNote)
    {
        var req = await db.StockTransactions.FindAsync(id);
        if (req == null) return NotFound();

        if (req.RequestStatusValue != RequestStatus.New && req.RequestStatusValue != RequestStatus.ClientConfirmed)
        {
            TempData["Error"] = "Заявку можно отклонить только со статусами «Новая» или «Договор подтверждён».";
            return RedirectToAction(nameof(Index));
        }

        req.RequestStatusValue = RequestStatus.Rejected;
        req.ManagerNote = managerNote?.Trim();
        await db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(req.UserId))
        {
            db.Notifications.Add(new Notification
            {
                UserId = req.UserId,
                Title = "Заявка отклонена",
                Message = $"Ваша заявка №{req.Id} отклонена. Причина: {managerNote}",
                Link = "/Client/ClientHome",
            });
            await db.SaveChangesAsync();
        }

        TempData["Info"] = $"Заявка №{id} отклонена.";
        return RedirectToAction(nameof(Index));
    }
}
