using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VentCleanInventory.Web.Data;

namespace VentCleanInventory.Web.Areas.Master.Controllers;

[Area(MasterArea.Name)]
[Authorize(Roles = AppUserRole.Master)]
public class MasterHomeController : Controller
{
    public IActionResult Index() => View();
}

