using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VentCleanInventory.Web.Data;
using VentCleanInventory.Web.Models;

namespace VentCleanInventory.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    [Authorize]
    public IActionResult Dashboard()
    {
        if (User.IsInRole(AppUserRole.Admin))
            return RedirectToAction("Index", "AdminHome", new { Area = "Admin" });
        if (User.IsInRole(AppUserRole.Dispatcher))
            return RedirectToAction("Index", "DispatcherHome", new { Area = "Dispatcher" });
        if (User.IsInRole(AppUserRole.Master))
            return RedirectToAction("Index", "MasterHome", new { Area = "Master" });
        if (User.IsInRole(AppUserRole.Manager))
            return RedirectToAction("Index", "ManagerHome", new { Area = "Manager" });
        if (User.IsInRole(AppUserRole.Client))
            return RedirectToAction("Index", "ClientHome", new { Area = "Client" });
        if (User.IsInRole(AppUserRole.Supplier))
            return RedirectToAction("Index", "SupplierHome", new { Area = "Supplier" });

        return RedirectToAction("Index", "Home");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
