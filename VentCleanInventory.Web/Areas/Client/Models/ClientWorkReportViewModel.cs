namespace VentCleanInventory.Web.Areas.Client.Models;

public class ClientWorkReportViewModel
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public IReadOnlyList<WorkLogSummary> Rows { get; set; } = [];
}
