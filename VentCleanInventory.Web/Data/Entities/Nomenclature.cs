using System.ComponentModel.DataAnnotations;

namespace VentCleanInventory.Web.Data.Entities;

public class Nomenclature
{
    public int Id { get; set; }

    [Display(Name = "Наименование")]
    [Required(ErrorMessage = "Введите наименование."), MaxLength(200)]
    public string Name { get; set; } = "";

    [Display(Name = "Единица измерения")]
    [Required(ErrorMessage = "Введите единицу измерения."), MaxLength(50)]
    public string Unit { get; set; } = "";

    [Display(Name = "Оборудование")]
    public bool IsEquipment { get; set; }

    [Display(Name = "Мин. остаток")]
    public decimal MinStockQuantity { get; set; }

    [Display(Name = "Предпочтительный поставщик")]
    public int? PreferredSupplierId { get; set; }
    public Organization? PreferredSupplier { get; set; }

    [Display(Name = "Описание")]
    [MaxLength(1000)]
    public string? Description { get; set; }

    private static readonly HashSet<string> PieceUnits = new(StringComparer.OrdinalIgnoreCase)
    {
        "шт", "шт.", "штук", "piece", "pc", "ед", "ед.", "единица", "единицы", "комплект", "компл.", "упак", "упак."
    };

    public bool IsPieceUnit() => PieceUnits.Contains(Unit?.Trim().ToLower() ?? "");

    public string QuantityStep() => IsPieceUnit() ? "1" : "0.01";

    public string FormatQuantity(decimal qty) => IsPieceUnit() ? qty.ToString("N0") : qty.ToString("N2");
}

