namespace Tracker.NotificationService.Models;

public class NotificationItem
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
}