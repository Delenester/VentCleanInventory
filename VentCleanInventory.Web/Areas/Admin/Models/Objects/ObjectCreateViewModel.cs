using System.ComponentModel.DataAnnotations;

namespace VentCleanInventory.Web.Areas.Admin.Models.Objects;

public class ObjectCreateViewModel
{
    [Display(Name = "Название")]
    [Required(ErrorMessage = "Введите название объекта.")]
    [MaxLength(200)]
    public string Name { get; set; } = "";

    [Display(Name = "Тип вентиляции")]
    [MaxLength(100)]
    public string? VentSystemType { get; set; }

    [Display(Name = "Сложность доступа")]
    [MaxLength(100)]
    public string? AccessDifficulty { get; set; }

    [Display(Name = "Удалённость")]
    [MaxLength(100)]
    public string? Distance { get; set; }

    public int? ZoneId { get; set; }
    public string? MasterUserId { get; set; }

    [Display(Name = "Адрес")]
    [MaxLength(300)]
    public string? Address { get; set; }

    public string? Description { get; set; }
}

public class ObjectEditViewModel
{
    public int Id { get; set; }

    [Display(Name = "Название")]
    [Required(ErrorMessage = "Введите название объекта.")]
    [MaxLength(200)]
    public string Name { get; set; } = "";

    [Display(Name = "Тип вентиляции")]
    [MaxLength(100)]
    public string? VentSystemType { get; set; }

    [Display(Name = "Сложность доступа")]
    [MaxLength(100)]
    public string? AccessDifficulty { get; set; }

    [Display(Name = "Удалённость")]
    [MaxLength(100)]
    public string? Distance { get; set; }

    public int? ZoneId { get; set; }
    public string? MasterUserId { get; set; }

    [Display(Name = "Адрес")]
    [MaxLength(300)]
    public string? Address { get; set; }

    public string? Description { get; set; }
}