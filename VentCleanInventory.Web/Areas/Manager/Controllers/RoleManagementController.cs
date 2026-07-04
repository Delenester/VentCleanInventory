using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Data;

namespace VentCleanInventory.Web.Areas.Manager.Controllers;

[Area(ManagerArea.Name)]
[Authorize(Roles = AppUserRole.Manager)]
public class RoleManagementController(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager) : Controller
{
    private static readonly string[] RestrictedRoles = [AppUserRole.Admin, AppUserRole.Manager];

    public async Task<IActionResult> Index()
    {
        var users = await userManager.Users.AsNoTracking()
            .Where(u => u.AccountType == AccountType.Internal)
            .OrderBy(u => u.UserName)
            .ToListAsync();

        var allRoles = await roleManager.Roles
            .OrderBy(r => r.Name)
            .ToListAsync();

        var userRoles = new Dictionary<string, IList<string>>();
        foreach (var user in users)
            userRoles[user.Id] = await userManager.GetRolesAsync(user);

        var allRolesList = allRoles
            .Where(r => r.Name != AppUserRole.Client && r.Name != AppUserRole.Supplier)
            .Select(r => r.Name!)
            .ToList();

        var editableUsers = users.Where(u =>
        {
            var roles = userRoles.GetValueOrDefault(u.Id, []);
            return !roles.Contains(AppUserRole.Admin) && !roles.Contains(AppUserRole.Manager);
        }).ToList();

        return View(new RoleManagementViewModel
        {
            Users = editableUsers,
            UserRoles = userRoles,
            AllRoles = allRolesList,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(string userId, string newRole)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        if (user.AccountType != AccountType.Internal)
        {
            TempData["Error"] = "Нельзя менять роль клиентам и поставщикам.";
            return RedirectToAction(nameof(Index));
        }

        if (RestrictedRoles.Contains(newRole))
        {
            TempData["Error"] = "Нельзя назначить роль руководителя или администратора.";
            return RedirectToAction(nameof(Index));
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        await userManager.RemoveFromRolesAsync(user, currentRoles);
        await userManager.AddToRoleAsync(user, newRole);
        await userManager.UpdateAsync(user);

        TempData["Info"] = $"Роль пользователя {user.UserName} изменена на «{newRole}».";
        return RedirectToAction(nameof(Index));
    }
}

public class RoleManagementViewModel
{
    public IReadOnlyList<ApplicationUser> Users { get; set; } = [];
    public Dictionary<string, IList<string>> UserRoles { get; set; } = [];
    public IReadOnlyList<string> AllRoles { get; set; } = [];
}
