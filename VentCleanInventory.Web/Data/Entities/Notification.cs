namespace VentCleanInventory.Web.Data.Entities;

public class Notification
{
    public int Id { get; set; }

    public string UserId { get; set; } = "";
    public ApplicationUser User { get; set; } = null!;

    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string? Link { get; set; }

    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
