using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VentCleanInventory.Web.Areas.Master.Models.Profile;
using VentCleanInventory.Web.Data;

namespace VentCleanInventory.Web.Areas.Master.Controllers;

[Area(MasterArea.Name)]
[Authorize(Roles = AppUserRole.Master)]
public class ProfileController(
    UserManager<ApplicationUser> userManager,
    IWebHostEnvironment env) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return Forbid();

        return View(new ProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email,
            PhotoPath = user.PhotoPath,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileViewModel model)
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return Forbid();

        model.FullName = user.FullName;
        model.Email = user.Email;

        if (model.Photo is null)
        {
            ModelState.AddModelError(nameof(model.Photo), "Выберите файл для загрузки.");
            model.PhotoPath = user.PhotoPath;
            return View(model);
        }

        var ext = Path.GetExtension(model.Photo.FileName).ToLowerInvariant();
        if (ext is not ".jpg" and not ".jpeg" and not ".png")
        {
            ModelState.AddModelError(nameof(model.Photo), "Допустимы только JPEG и PNG.");
            model.PhotoPath = user.PhotoPath;
            return View(model);
        }

        if (model.Photo.Length > 5 * 1024 * 1024)
        {
            ModelState.AddModelError(nameof(model.Photo), "Максимальный размер — 5 МБ.");
            model.PhotoPath = user.PhotoPath;
            return View(model);
        }

        var uploadsDir = Path.Combine(env.WebRootPath, "uploads", "profiles");
        Directory.CreateDirectory(uploadsDir);

        if (!string.IsNullOrWhiteSpace(user.PhotoPath))
        {
            var oldPath = Path.Combine(env.WebRootPath, user.PhotoPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(oldPath))
                System.IO.File.Delete(oldPath);
        }

        var fileName = $"{user.Id}{ext}";
        var fullPath = Path.Combine(uploadsDir, fileName);
        await using (var fs = System.IO.File.Create(fullPath))
        {
            await model.Photo.CopyToAsync(fs);
        }

        user.PhotoPath = $"/uploads/profiles/{fileName}";
        await userManager.UpdateAsync(user);

        TempData["Info"] = "Фото профиля обновлено.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return userId is null ? null : await userManager.FindByIdAsync(userId);
    }
}
