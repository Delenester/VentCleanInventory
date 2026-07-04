using System.ComponentModel.DataAnnotations;

namespace VentCleanInventory.Web.Areas.Admin.Models.Users;

public class UserListItemViewModel
{
    public string Id { get; set; } = "";
    public string UserName { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public bool IsApproved { get; set; }
    public List<string> Roles { get; set; } = [];
}

public class UserCreateViewModel
{
    [Display(Name = "Логин")]
    [Required, MaxLength(100)]
    public string Login { get; set; } = "";

    [Display(Name = "Пароль")]
    [Required, MinLength(8, ErrorMessage = "Пароль должен содержать минимум 8 символов.")]
    public string Password { get; set; } = "";

    [Display(Name = "ФИО")]
    [MaxLength(200)]
    public string? FullName { get; set; }

    [Display(Name = "Email")]
    [EmailAddress]
    public string? Email { get; set; }

    [Display(Name = "Роль")]
    [Required]
    public string? Role { get; set; }
}

public class UserEditViewModel
{
    public string Id { get; set; } = "";

    public string UserName { get; set; } = "";

    [Display(Name = "ФИО")]
    [MaxLength(200)]
    public string? FullName { get; set; }

    [Display(Name = "Email")]
    [EmailAddress]
    public string? Email { get; set; }

    [Display(Name = "Роль")]
    public string? Role { get; set; }
}
