using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VentCleanInventory.Web.Data;
using VentCleanInventory.Web.Services;

namespace VentCleanInventory.Web.Areas.Admin.Controllers;

[Area(AdminArea.Name)]
[Authorize(Roles = AppUserRole.Admin)]
public class BackupController(BackupService backupService) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Run(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            TempData["Error"] = "Укажите путь для сохранения бэкапа.";
            return RedirectToAction(nameof(Index));
        }

        var path = directoryPath.Trim();

        if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            TempData["Error"] = "Путь содержит недопустимые символы.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var result = await backupService.RunBackupAsync(path);
            TempData["Success"] = $"Бэкап создан: {result}";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Ошибка бэкапа: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }
}
