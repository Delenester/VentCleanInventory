using System.ComponentModel.DataAnnotations;
using VentCleanInventory.Web.Data;

namespace VentCleanInventory.Web.Areas.Admin.Models.Warehouses;

public class WarehouseIndexViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public WarehouseType Type { get; set; }
    public string MasterName { get; set; } = "";
}

public class WarehouseEditViewModel
{
    public int Id { get; set; }

    [Display(Name = "Название")]
    [Required(ErrorMessage = "Введите название склада.")]
    [MaxLength(150)]
    public string Name { get; set; } = "";

    [Display(Name = "Тип")]
    public WarehouseType Type { get; set; }

    [Display(Name = "Адрес склада")]
    [MaxLength(300)]
    public string? Address { get; set; }

    [Display(Name = "Примечание")]
    [MaxLength(500)]
    public string? Note { get; set; }

    [Display(Name = "Мастер (для мобильного склада)")]
    public string? MasterUserId { get; set; }

    public IReadOnlyList<MasterOption> Masters { get; set; } = [];

    public class MasterOption
    {
        public required string Id { get; init; }
        public required string Display { get; init; }
    }
}