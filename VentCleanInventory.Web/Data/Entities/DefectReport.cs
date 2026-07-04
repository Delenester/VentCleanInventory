using System.ComponentModel.DataAnnotations;

namespace VentCleanInventory.Web.Data.Entities;

public class DefectReport
{
    public int Id { get; set; }

    public string MasterUserId { get; set; } = "";

    public int WorkObjectId { get; set; }
    public WorkObject WorkObject { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    [Display(Name = "Описание брака")]
    [Required]
    [MaxLength(2000)]
    public string Description { get; set; } = "";

    [Display(Name = "Зона/площадка")]
    [MaxLength(200)]
    public string? ZoneName { get; set; }

    [MaxLength(260)]
    public string? PhotoPath { get; set; }
}
