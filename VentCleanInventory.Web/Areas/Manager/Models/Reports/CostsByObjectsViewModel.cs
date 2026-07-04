using System.ComponentModel.DataAnnotations;

namespace VentCleanInventory.Web.Areas.Manager.Models.Reports;

public class CostsByObjectsViewModel
{
    [Display(Name = "Дата начала")]
    [DataType(DataType.Date)]
    public DateTime? From { get; set; }

    [Display(Name = "Дата окончания")]
    [DataType(DataType.Date)]
    public DateTime? To { get; set; }

    public IReadOnlyList<Row> Rows { get; set; } = [];

    public class Row
    {
        public required string ObjectName { get; init; }
        public decimal TotalCost { get; init; }
        public decimal TotalMeters { get; init; }
        public decimal TotalGrids { get; init; }
    }
}

