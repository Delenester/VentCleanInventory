using System.ComponentModel.DataAnnotations;

namespace VentCleanInventory.Web.Areas.Dispatcher.Models;

public class NomenclatureCreateViewModel
{
    [Display(Name = "Наименование")]
    public string Name { get; set; } = "";

    [Display(Name = "Оборудование")]
    public bool IsEquipment { get; set; }

    [Display(Name = "Ед. измерения")]
    public string? Unit { get; set; }

    [Display(Name = "Мин. остаток для заказа")]
    public decimal MinStockQuantity { get; set; }

    [Display(Name = "Предпочтительный поставщик")]
    public int? PreferredSupplierId { get; set; }
}

public class NomenclatureEditViewModel
{
    public int Id { get; set; }

    [Display(Name = "Наименование")]
    public string Name { get; set; } = "";

    [Display(Name = "Оборудование")]
    public bool IsEquipment { get; set; }

    [Display(Name = "Ед. измерения")]
    public string? Unit { get; set; }

    [Display(Name = "Мин. остаток для заказа")]
    public decimal MinStockQuantity { get; set; }

    [Display(Name = "Предпочтительный поставщик")]
    public int? PreferredSupplierId { get; set; }
}
