namespace VentCleanInventory.Web.Areas.Manager.Models.Reports;

public class WorkReportViewModel
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public List<Row> Rows { get; set; } = [];

    public class Row
    {
        public int Id { get; set; }
        public string MasterName { get; set; } = "";
        public string ObjectName { get; set; } = "";
        public string? ZoneName { get; set; }
        public DateTime WorkDate { get; set; }
        public decimal? Meters { get; set; }
        public int? Grids { get; set; }
        public string? MaterialsUsed { get; set; }
        public string? PhotoPath { get; set; }
        public bool IsCompleted { get; set; }
    }
}
