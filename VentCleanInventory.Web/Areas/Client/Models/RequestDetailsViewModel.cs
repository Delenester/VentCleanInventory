using VentCleanInventory.Web.Data.Entities;

namespace VentCleanInventory.Web.Areas.Client.Models;

public class RequestDetailsViewModel
{
    public StockTransaction Request { get; set; } = null!;
    public string MasterName { get; set; } = "";
    public string ServiceType { get; set; } = "";
    public string Description { get; set; } = "";
    public Organization? ClientOrg { get; set; }
}
