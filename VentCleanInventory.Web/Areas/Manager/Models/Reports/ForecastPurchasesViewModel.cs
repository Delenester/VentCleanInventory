using System.ComponentModel.DataAnnotations;

namespace VentCleanInventory.Web.Areas.Manager.Models.Reports;

public class ForecastPurchasesViewModel
{
    [Display(Name = "Период (мес.)")]
    [Range(1, 36)]
    public int Months { get; set; } = 3;

    public IReadOnlyList<Row> Rows { get; set; } = [];

    public class Row
    {
        public required string MaterialName { get; init; }
        public decimal AvgMonthlyUsage { get; init; }
        public decimal RecommendedNextMonth { get; init; }
        public decimal CentralBalance { get; init; }
    }
}

