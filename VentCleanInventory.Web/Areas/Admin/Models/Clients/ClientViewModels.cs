using System.ComponentModel.DataAnnotations;

namespace VentCleanInventory.Web.Areas.Admin.Models.Clients;

public class ClientCreateViewModel
{
    [Display(Name = "УНП")]
    [Required, MaxLength(9)]
    [RegularExpression(@"^\d{9}$", ErrorMessage = "УНП — 9 цифр.")]
    public string Unp { get; set; } = "";

    [Display(Name = "Наименование организации")]
    [Required, MaxLength(300)]
    public string OrganizationName { get; set; } = "";

    [Display(Name = "Юридический адрес")]
    [MaxLength(500)]
    public string? LegalAddress { get; set; }

    [Display(Name = "Контакты")]
    [MaxLength(500)]
    public string? ContactInfo { get; set; }
}

public class ClientEditViewModel : ClientCreateViewModel
{
    public int Id { get; set; }
}
