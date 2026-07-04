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
            TempData["Info"] = "Укажите путь для сохранения бэкапа.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var path = await backupService.RunBackupAsync(directoryPath.Trim());
            TempData["Info"] = $"Бэкап создан: {path}";
        }
        catch (Exception ex)
        {
            TempData["Info"] = $"Ошибка бэкапа: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }
}
