using System.ComponentModel.DataAnnotations;

namespace VentCleanInventory.Web.Data.Entities;

public class WorkLog
{
    public int Id { get; set; }

    public string MasterUserId { get; set; } = "";
    public global::VentCleanInventory.Web.Data.ApplicationUser MasterUser { get; set; } = null!;

    public int WorkObjectId { get; set; }
    public WorkObject WorkObject { get; set; } = null!;

    public DateTime WorkDate { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(200)]
    public string? ZoneName { get; set; }

    public string? MaterialsUsed { get; set; }

    public decimal? Meters { get; set; }

    public int? Grids { get; set; }

    [MaxLength(260)]
    public string? PhotoPath { get; set; }

    public bool IsCompleted { get; set; }

    public bool? ChecklistDone { get; set; }

    public string? ChecklistData { get; set; }
}
