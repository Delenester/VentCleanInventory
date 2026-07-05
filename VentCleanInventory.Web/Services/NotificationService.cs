using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VentCleanInventory.Web.Data;
using VentCleanInventory.Web.Data.Entities;

namespace VentCleanInventory.Web.Services;

public class NotificationService(ApplicationDbContext db, ILogger<NotificationService> logger)
{
    public async Task NotifyUserAsync(string userId, string title, string message, string? link = null)
    {
        db.Notifications.Add(new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Link = link,
        });
        await db.SaveChangesAsync();
        logger.LogInformation("Notification created for user {UserId}: {Title}", userId, title);
    }

    public async Task NotifyUsersAsync(IEnumerable<string> userIds, string title, string message, string? link = null)
    {
        foreach (var uid in userIds)
        {
            db.Notifications.Add(new Notification
            {
                UserId = uid,
                Title = title,
                Message = message,
                Link = link,
            });
        }
        await db.SaveChangesAsync();
        logger.LogInformation("Notifications created for {Count} users: {Title}", userIds.Count(), title);
    }

    public async Task NotifyRoleAsync(UserManager<ApplicationUser> userManager, string role, string title, string message, string? link = null)
    {
        var users = await userManager.GetUsersInRoleAsync(role);
        await NotifyUsersAsync(users.Select(u => u.Id), title, message, link);
    }

    public async Task<int> MarkReadAsync(int notificationId, string userId)
    {
        return await db.Notifications
            .Where(n => n.Id == notificationId && n.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }

    public async Task MarkAllReadAsync(string userId)
    {
        await db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }

    public async Task MarkByLinkAsync(string userId, string link)
    {
        await db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead && n.Link != null && n.Link == link)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }
}
