using System.ComponentModel.DataAnnotations;

namespace VentCleanInventory.Web.Areas.Master.Models.Profile;

public class ProfileViewModel
{
    public string FullName { get; set; } = "";
    public string? Email { get; set; }
    public string? PhotoPath { get; set; }

    [Display(Name = "Фото")]
    public IFormFile? Photo { get; set; }
}
