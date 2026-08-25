using Microsoft.EntityFrameworkCore;
using Tracker.NotificationService.Data;
using Tracker.NotificationService.Models;

namespace Tracker.NotificationService.Services;

public sealed class EfNotificationStore
    : INotificationStore
{
    private readonly NotificationDbContext _db;

    public EfNotificationStore(
        NotificationDbContext db)
    {
        _db = db;
    }

    public IReadOnlyList<NotificationItem>
        GetAll(string userId)
    {
        return _db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.Id)
            .ToList();
    }

    public int GetUnreadCount(
        string userId)
    {
        return _db.Notifications
            .Count(n =>
                n.UserId == userId &&
                !n.IsRead);
    }

    public bool MarkAsRead(
        string userId,
        int id)
    {
        var notification =
            _db.Notifications.FirstOrDefault(
                n => n.Id == id &&
                     n.UserId == userId);

        if (notification is null)
        {
            return false;
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            _db.SaveChanges();
        }

        return true;
    }
}