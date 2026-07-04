using System.ComponentModel.DataAnnotations;

namespace VentCleanInventory.Web.Areas.Admin.Models.Suppliers;

public class SupplierCreateViewModel
{
    [Display(Name = "Название")]
    [Required, MaxLength(200)]
    public string Name { get; set; } = "";

    [Display(Name = "УНП")]
    [MaxLength(9)]
    [RegularExpression(@"^$|^\d{9}$", ErrorMessage = "УНП — 9 цифр.")]
    public string? Unp { get; set; }

    [Display(Name = "Юридический адрес")]
    [MaxLength(500)]
    public string? LegalAddress { get; set; }

    [Display(Name = "Контакты")]
    [MaxLength(500)]
    public string? ContactInfo { get; set; }
}

public class SupplierEditViewModel
{
    public int Id { get; set; }

    [Display(Name = "Название")]
    [Required, MaxLength(200)]
    public string Name { get; set; } = "";

    [Display(Name = "УНП")]
    [MaxLength(9)]
    [RegularExpression(@"^$|^\d{9}$", ErrorMessage = "УНП — 9 цифр.")]
    public string? Unp { get; set; }

    [Display(Name = "Юридический адрес")]
    [MaxLength(500)]
    public string? LegalAddress { get; set; }

    [Display(Name = "Контакты")]
    [MaxLength(500)]
    public string? ContactInfo { get; set; }
}