using System.ComponentModel.DataAnnotations;

namespace VentCleanInventory.Web.Data.Entities;

public class Feedback
{
    public int Id { get; set; }

    public int WorkLogId { get; set; }
    public WorkLog WorkLog { get; set; } = null!;

    [MaxLength(128)]
    public string ClientUserId { get; set; } = "";

    public int Rating { get; set; }

    [MaxLength(2000)]
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }
}
