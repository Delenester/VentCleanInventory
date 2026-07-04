namespace VentCleanInventory.Web.Areas.Dispatcher.Models.Requests;

public class RequestListItemViewModel
{
    public int Id { get; init; }
    public DateTime DateCreated { get; init; }
    public string MasterName { get; init; } = "";
    public string ObjectName { get; init; } = "";
    public VentCleanInventory.Web.Data.RequestStatus Status { get; init; }
    public int ItemsCount { get; init; }
}

