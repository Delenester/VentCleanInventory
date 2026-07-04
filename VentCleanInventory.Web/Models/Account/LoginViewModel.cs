using System.ComponentModel.DataAnnotations;

namespace VentCleanInventory.Web.Models.Account;

public class LoginViewModel
{
    [Required(ErrorMessage = "Введите логин.")]
    [Display(Name = "Логин")]
    public string Login { get; set; } = "";

    [Required(ErrorMessage = "Введите пароль.")]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = "";

    [Display(Name = "Запомнить меня")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

