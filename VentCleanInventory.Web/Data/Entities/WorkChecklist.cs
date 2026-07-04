using System.ComponentModel.DataAnnotations;

namespace VentCleanInventory.Web.Data.Entities;

public class WorkChecklist
{
    public int Id { get; set; }

    public int WorkLogId { get; set; }
    public WorkLog WorkLog { get; set; } = null!;

    public string ItemName { get; set; } = "";

    public bool IsDone { get; set; }

    public string? Note { get; set; }

    [MaxLength(260)]
    public string? PhotoPath { get; set; }
}
