using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace VentCleanInventory.Web.Data;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = "";
    public bool IsApproved { get; set; } = false;

    public AccountType AccountType { get; set; } = AccountType.Internal;

    [MaxLength(260)]
    public string? PhotoPath { get; set; }

    public int? OrganizationId { get; set; }
    public Entities.Organization? Organization { get; set; }

    [Display(Name = "Номер договора")]
    [MaxLength(50)]
    public string? ContractNumber { get; set; }

    [Display(Name = "Телефон")]
    [MaxLength(50)]
    public string? PhoneContact { get; set; }

    [Display(Name = "Расчётный счёт")]
    [MaxLength(50)]
    public string? BankAccount { get; set; }

    [Display(Name = "Банк")]
    [MaxLength(200)]
    public string? BankName { get; set; }

    [Display(Name = "БИК")]
    [MaxLength(20)]
    public string? BIK { get; set; }
}
