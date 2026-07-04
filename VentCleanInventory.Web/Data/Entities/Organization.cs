using System.ComponentModel.DataAnnotations;

namespace VentCleanInventory.Web.Data.Entities;

public class Organization
{
    public int Id { get; set; }

    public OrganizationType Type { get; set; }

    [Display(Name = "Наименование")]
    [Required(ErrorMessage = "Введите наименование."), MaxLength(300)]
    public string Name { get; set; } = "";

    [Display(Name = "УНП")]
    [MaxLength(9)]
    [RegularExpression(@"^\d{9}$", ErrorMessage = "УНП — 9 цифр.")]
    public string? Unp { get; set; }

    [Display(Name = "Юридический адрес")]
    [MaxLength(500)]
    public string? LegalAddress { get; set; }

    [Display(Name = "Контактные данные")]
    [MaxLength(500)]
    public string? ContactInfo { get; set; }
}
