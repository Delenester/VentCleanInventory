using VentCleanInventory.Web.Data.Entities;

namespace VentCleanInventory.Web.Areas.Master.Models;

public class MyWorkViewModel
{
    public List<WorkLog> InProgress { get; set; } = [];
    public List<WorkLog> Completed { get; set; } = [];
}
