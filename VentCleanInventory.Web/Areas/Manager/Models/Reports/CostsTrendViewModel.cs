using System.ComponentModel.DataAnnotations;

namespace VentCleanInventory.Web.Areas.Manager.Models.Reports;

public class CostsTrendViewModel
{
    [Display(Name = "Дата начала")]
    [DataType(DataType.Date)]
    public DateTime? From { get; set; }

    [Display(Name = "Дата окончания")]
    [DataType(DataType.Date)]
    public DateTime? To { get; set; }

    public IReadOnlyList<Point> Points { get; set; } = [];

    public class Point
    {
        public required string Month { get; init; } // yyyy-MM
        public decimal Cost { get; init; }
    }
}

