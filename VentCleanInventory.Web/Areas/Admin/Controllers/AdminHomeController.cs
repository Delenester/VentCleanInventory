using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VentCleanInventory.Web.Data;

namespace VentCleanInventory.Web.Areas.Admin.Controllers;

[Area(AdminArea.Name)]
[Authorize(Roles = AppUserRole.Admin)]
public class AdminHomeController : Controller
{
    public IActionResult Index() => View();
}

