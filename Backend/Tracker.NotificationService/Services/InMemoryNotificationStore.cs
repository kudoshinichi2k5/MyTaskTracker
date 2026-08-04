using System.Collections.Concurrent;
using Tracker.NotificationService.Models;

namespace Tracker.NotificationService.Services;

/// <summary>
/// In-memory, per-user, thread-safe notification store. Same pattern and
/// same caveat as Tracker.TaskService's InMemoryTaskStore: this used to be
/// one static list shared by every signed-in user; now it's scoped per user
/// (by JWT `sub` claim) and safe under concurrent access. Still resets on
/// restart - swap for a real store once notifications have a real event
/// source (task-due-soon, task-assigned, etc).
/// </summary>
public class InMemoryNotificationStore : INotificationStore
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<int, NotificationItem>> _byUser = new();

    private ConcurrentDictionary<int, NotificationItem> BucketFor(string userId)
    {
        return _byUser.GetOrAdd(userId, _ =>
        {
            var bucket = new ConcurrentDictionary<int, NotificationItem>();
            bucket[1] = new NotificationItem
            {
                Id = 1,
                Message = "Welcome to TaskTracker! Your workspace is ready.",
                IsRead = false
            };
            return bucket;
        });
    }

    public IReadOnlyList<NotificationItem> GetAll(string userId)
    {
        return BucketFor(userId).Values.OrderByDescending(n => n.Id).ToList();
    }

    public int GetUnreadCount(string userId)
    {
        return BucketFor(userId).Values.Count(n => !n.IsRead);
    }

    public bool MarkAsRead(string userId, int id)
    {
        if (!BucketFor(userId).TryGetValue(id, out var notification))
        {
            return false;
        }

        notification.IsRead = true;
        return true;
    }
}