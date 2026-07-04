namespace VentCleanInventory.Web.Areas.Client.Models;

public class ClientPortalViewModel
{
    public global::VentCleanInventory.Web.Data.Entities.Organization? Client { get; set; }
    public IReadOnlyList<global::VentCleanInventory.Web.Data.Entities.StockTransaction> Requests { get; set; } = [];
    public Dictionary<string, string> RequestMasterNames { get; set; } = [];
    public IReadOnlyList<WorkLogSummary> CompletedWork { get; set; } = [];
    public IReadOnlyList<global::VentCleanInventory.Web.Data.Entities.Notification> Notifications { get; set; } = [];
}

public class WorkLogSummary
{
    public int WorkLogId { get; set; }
    public string ObjectName { get; set; } = "";
    public string? ZoneName { get; set; }
    public string MasterName { get; set; } = "";
    public DateTime WorkDate { get; set; }
    public decimal? Meters { get; set; }
    public int? Grids { get; set; }
    public string? MaterialsUsed { get; set; }
    public string? PhotoPath { get; set; }
    public string? Description { get; set; }
}
