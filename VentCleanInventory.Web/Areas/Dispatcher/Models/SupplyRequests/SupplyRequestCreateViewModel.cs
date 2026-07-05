using System.ComponentModel.DataAnnotations;

namespace VentCleanInventory.Web.Areas.Dispatcher.Models.SupplyRequests;

public class SupplyRequestCreateViewModel
{
    [Display(Name = "Поставщик")]
    public int? SupplierId { get; set; }

    [Display(Name = "Примечание")]
    public string? Note { get; set; }

    public IReadOnlyList<global::VentCleanInventory.Web.Data.Entities.Organization> Suppliers { get; set; } = [];
    public IReadOnlyList<global::VentCleanInventory.Web.Data.Entities.Nomenclature> Nomenclatures { get; set; } = [];
    public List<Line> Lines { get; set; } = [];

    public class Line
    {
        [Display(Name = "Номенклатура")]
        public int? NomenclatureId { get; set; }

        [Display(Name = "Количество")]
        public decimal Quantity { get; set; } = 1;
    }
}
