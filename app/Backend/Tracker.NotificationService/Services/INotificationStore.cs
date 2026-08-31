using Tracker.NotificationService.Models;

namespace Tracker.NotificationService.Services;

public interface INotificationStore
{
    IReadOnlyList<NotificationItem> GetAll(string userId);
    int GetUnreadCount(string userId);
    bool MarkAsRead(string userId, int id);
}