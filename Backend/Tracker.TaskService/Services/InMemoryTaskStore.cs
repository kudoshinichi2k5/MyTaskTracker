using System.Collections.Concurrent;
using Tracker.TaskService.Models;

namespace Tracker.TaskService.Services;

/// <summary>
/// In-memory, per-user, thread-safe task store.
///
/// Previously this was a single `List&lt;TaskItem&gt;` shared by every signed-in
/// user (everyone saw the same two tasks) and mutated with no locking at all,
/// which is unsafe the moment more than one request writes concurrently.
/// This keeps the same "no real database yet" trade-off, but scopes data per
/// user (keyed by the JWT `sub` claim) and uses ConcurrentDictionary so
/// concurrent create/update/delete calls can't corrupt state.
///
/// Swap this for a real persistence layer (EF Core + SQLite/Postgres) before
/// this goes anywhere near real users - data still resets on every restart.
/// </summary>
public class InMemoryTaskStore : ITaskStore
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<int, TaskItem>> _byUser = new();
    private int _nextId = 2; // 1 is the seeded demo task below; Interlocked.Increment starts issuing from 2 upward

    private ConcurrentDictionary<int, TaskItem> BucketFor(string userId)
    {
        return _byUser.GetOrAdd(userId, _ =>
        {
            var bucket = new ConcurrentDictionary<int, TaskItem>();
            // Seed each new user with one starter task so a fresh login isn't a blank board.
            bucket[1] = new TaskItem { Id = 1, Title = "Welcome! Add your first task.", IsCompleted = false };
            return bucket;
        });
    }

    public IReadOnlyList<TaskItem> GetAll(string userId)
    {
        return BucketFor(userId).Values.OrderBy(t => t.Id).ToList();
    }

    public TaskItem Create(string userId, string title)
    {
        var bucket = BucketFor(userId);
        var id = Interlocked.Increment(ref _nextId);
        var task = new TaskItem { Id = id, Title = title, IsCompleted = false };
        bucket[id] = task;
        return task;
    }

    public bool Update(string userId, int id, string? title, bool? isCompleted)
    {
        var bucket = BucketFor(userId);
        if (!bucket.TryGetValue(id, out var task))
        {
            return false;
        }

        if (title is not null)
        {
            task.Title = title;
        }

        if (isCompleted is not null)
        {
            task.IsCompleted = isCompleted.Value;
        }

        return true;
    }

    public bool Delete(string userId, int id)
    {
        return BucketFor(userId).TryRemove(id, out _);
    }

    public IReadOnlyList<UserTaskSummary> GetSummaryForAllUsers()
    {
        return _byUser
            .Select(kvp => new UserTaskSummary(
                UserId: kvp.Key,
                TotalTasks: kvp.Value.Count,
                CompletedTasks: kvp.Value.Values.Count(t => t.IsCompleted)))
            .OrderBy(s => s.UserId)
            .ToList();
    }
}