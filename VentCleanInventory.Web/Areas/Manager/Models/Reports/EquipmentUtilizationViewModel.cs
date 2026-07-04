using System.ComponentModel.DataAnnotations;

namespace VentCleanInventory.Web.Areas.Manager.Models.Reports;

public class EquipmentUtilizationViewModel
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
        public required string Name { get; init; }
        public required string SerialNumber { get; init; }
        public int IssueCount { get; init; }
        public double AvgDaysWithMaster { get; init; }
    }
}

