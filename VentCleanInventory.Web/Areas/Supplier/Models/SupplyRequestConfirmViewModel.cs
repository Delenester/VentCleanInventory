using System.ComponentModel.DataAnnotations;

namespace VentCleanInventory.Web.Areas.Supplier.Models;

public class SupplyRequestConfirmViewModel
{
    public int RequestId { get; set; }
    public string RequestNumber { get; set; } = "";

    [Display(Name = "Комментарий")]
    public string? Note { get; set; }

    public List<Line> Items { get; set; } = [];

    public class Line
    {
        public int SupplyRequestItemId { get; set; }
        public string NomenclatureName { get; set; } = "";
        public string Unit { get; set; } = "";

        [Display(Name = "Запрошено")]
        public decimal OrderedQuantity { get; set; }

        [Display(Name = "Подтверждаемое кол-во")]
        [Range(0, double.MaxValue, ErrorMessage = "Укажите корректное количество")]
        public decimal ConfirmedQuantity { get; set; }

        [Display(Name = "Цена за ед.")]
        [Range(0, double.MaxValue, ErrorMessage = "Укажите корректную цену")]
        public decimal? UnitPrice { get; set; }

        [Display(Name = "Примечание")]
        public string? Note { get; set; }
    }
}
