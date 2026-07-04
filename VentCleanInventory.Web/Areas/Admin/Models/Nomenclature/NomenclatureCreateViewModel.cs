using System.ComponentModel.DataAnnotations;

namespace VentCleanInventory.Web.Areas.Admin.Models.Nomenclature;

public class NomenclatureCreateViewModel
{
    [Display(Name = "Наименование")]
    public string Name { get; set; } = "";

    [Display(Name = "Оборудование")]
    public bool IsEquipment { get; set; }

    [Display(Name = "Ед. измерения")]
    public string? Unit { get; set; }
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
}