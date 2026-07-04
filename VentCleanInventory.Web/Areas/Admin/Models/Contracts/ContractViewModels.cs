using System.ComponentModel.DataAnnotations;

namespace VentCleanInventory.Web.Areas.Admin.Models.Contracts;

public class ContractCreateViewModel
{
    [Display(Name = "Номер договора")]
    [Required, MaxLength(50)]
    public string Number { get; set; } = "";

    [Display(Name = "Клиент")]
    [Required]
    public int? ClientId { get; set; }

    [Display(Name = "Поставщик")]
    public int? SupplierId { get; set; }

    [Display(Name = "Дата начала")]
    [Required]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Display(Name = "Дата окончания")]
    public DateTime? EndDate { get; set; }

    [Display(Name = "Описание")]
    [MaxLength(1000)]
    public string? Description { get; set; }

    [Display(Name = "Активен")]
    public bool IsActive { get; set; } = true;

    public IReadOnlyList<global::VentCleanInventory.Web.Data.Entities.Organization> Clients { get; set; } = [];
    public IReadOnlyList<global::VentCleanInventory.Web.Data.Entities.Organization> Suppliers { get; set; } = [];
}

public class ContractEditViewModel : ContractCreateViewModel
{
    public int Id { get; set; }
}
