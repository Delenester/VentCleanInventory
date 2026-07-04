using System.ComponentModel.DataAnnotations;

namespace VentCleanInventory.Web.Data.Entities;

public class WorkObject
{
    public int Id { get; set; }

    [Display(Name = "Название")]
    [Required(ErrorMessage = "Введите название объекта."), MaxLength(200)]
    public string Name { get; set; } = "";

    [Display(Name = "Адрес")]
    [MaxLength(300)]
    public string? Address { get; set; }

    [Display(Name = "Сложность доступа")]
    [MaxLength(100)]
    public string? AccessDifficulty { get; set; }

    [Display(Name = "Тип вентиляции")]
    [MaxLength(100)]
    public string? VentSystemType { get; set; }

    [Display(Name = "Удалённость")]
    [MaxLength(100)]
    public string? Distance { get; set; }
}

