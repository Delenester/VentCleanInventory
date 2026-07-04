using System.ComponentModel.DataAnnotations;
using VentCleanInventory.Web.Data;

namespace VentCleanInventory.Web.Models.Account;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Выберите тип регистрации.")]
    [Display(Name = "Тип аккаунта")]
    public AccountType AccountType { get; set; } = AccountType.Client;

    [Required(ErrorMessage = "Введите контактное лицо или ФИО.")]
    [Display(Name = "Контактное лицо / ФИО")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Поле \"{0}\" должно содержать от 2 до {1} символов.")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Введите email")]
    [EmailAddress(ErrorMessage = "Неверный формат email")]
    [Display(Name = "Email (логин)")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Введите УНП организации")]
    [Display(Name = "УНП")]
    [StringLength(9, MinimumLength = 9, ErrorMessage = "УНП должен содержать 9 цифр.")]
    [RegularExpression(@"^\d{9}$", ErrorMessage = "УНП — 9 цифр.")]
    public string Unp { get; set; } = "";

    [Required(ErrorMessage = "Введите наименование организации")]
    [Display(Name = "Наименование организации")]
    [StringLength(300, ErrorMessage = "Поле \"{0}\" должно содержать не более {1} символов.")]
    public string OrganizationName { get; set; } = "";

    [Required(ErrorMessage = "Введите юридический адрес")]
    [Display(Name = "Юридический адрес")]
    [StringLength(500, ErrorMessage = "Поле \"{0}\" должно содержать не более {1} символов.")]
    public string LegalAddress { get; set; } = "";

    [Display(Name = "Контактные данные")]
    [StringLength(500, ErrorMessage = "Поле \"{0}\" должно содержать не более {1} символов.")]
    public string? ContactInfo { get; set; }

    [Display(Name = "Номер договора")]
    [StringLength(50, ErrorMessage = "Поле \"{0}\" должно содержать не более {1} символов.")]
    public string? ContractNumber { get; set; }

    [Display(Name = "Телефон")]
    [StringLength(50, ErrorMessage = "Поле \"{0}\" должно содержать не более {1} символов.")]
    public string? PhoneContact { get; set; }

    [Display(Name = "Расчётный счёт")]
    [StringLength(50, ErrorMessage = "Поле \"{0}\" должно содержать не более {1} символов.")]
    public string? BankAccount { get; set; }

    [Display(Name = "Банк")]
    [StringLength(200, ErrorMessage = "Поле \"{0}\" должно содержать не более {1} символов.")]
    public string? BankName { get; set; }

    [Display(Name = "БИК")]
    [StringLength(20, ErrorMessage = "Поле \"{0}\" должно содержать не более {1} символов.")]
    public string? BIK { get; set; }

    [Required(ErrorMessage = "Введите пароль")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Пароль должен содержать от {2} до {1} символов.")]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = "";

    [DataType(DataType.Password)]
    [Display(Name = "Подтвердите пароль")]
    [Compare("Password", ErrorMessage = "Пароли не совпадают")]
    public string ConfirmPassword { get; set; } = "";
}
