using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Areas.Admin.Models.Users;
using VentCleanInventory.Web.Data;

namespace VentCleanInventory.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Администратор")]
public class UsersController(
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var users = await userManager.Users.OrderBy(u => u.UserName).ToListAsync();
        
        var model = users.Select(u => new UserListItemViewModel
        {
            Id = u.Id,
            UserName = u.UserName!,
            FullName = u.FullName ?? "",
            Email = u.Email ?? "",
            IsApproved = u.IsApproved,
            Roles = new List<string>()
        }).ToList();

        foreach (var userVm in model)
        {
            var user = await userManager.FindByIdAsync(userVm.Id);
            if (user != null)
            {
                userVm.Roles = (await userManager.GetRolesAsync(user)).ToList();
            }
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var exists = await userManager.FindByNameAsync(model.Login);
        if (exists != null)
        {
            ModelState.AddModelError("Login", "Пользователь с таким логином уже существует.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Login,
            FullName = model.FullName?.Trim() ?? "",
            Email = model.Email?.Trim() ?? "",
            IsApproved = true,
            AccountType = AccountType.Internal,
        };

        var result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(model.Role))
        {
            await userManager.AddToRoleAsync(user, model.Role);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var roles = await userManager.GetRolesAsync(user);

        return View(new UserEditViewModel
        {
            Id = user.Id,
            UserName = user.UserName!,
            FullName = user.FullName ?? "",
            Email = user.Email ?? "",
            Role = roles.FirstOrDefault()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, UserEditViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Any())
        {
            await userManager.RemoveFromRolesAsync(user, currentRoles);
        }

        user.FullName = model.FullName?.Trim() ?? "";
        user.Email = model.Email?.Trim() ?? "";

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(model.Role))
        {
            await userManager.AddToRoleAsync(user, model.Role);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join("; ", result.Errors.Select(e => e.Description));
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string id, string newPassword)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            TempData["Error"] = "Введите новый пароль.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        if (newPassword.Length < 8)
        {
            TempData["Error"] = "Пароль должен содержать не менее 8 символов.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        if (!newPassword.Any(char.IsDigit))
        {
            TempData["Error"] = "Пароль должен содержать хотя бы одну цифру.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, newPassword);

        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join("; ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Edit), new { id });
        }

        TempData["Success"] = $"Пароль для «{user.UserName}» изменён.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        user.IsApproved = true;
        await userManager.UpdateAsync(user);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        await userManager.DeleteAsync(user);
        return RedirectToAction(nameof(Index));
    }
}
