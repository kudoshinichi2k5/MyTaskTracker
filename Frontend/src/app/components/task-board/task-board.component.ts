import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TaskService, TaskItem } from '../../services/task.service';
import { AuthService } from '../../services/auth.service';
import { NotificationService, NotificationItem } from '../../services/notification.service';

@Component({
  selector: 'app-task-board',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './task-board.component.html',
  styleUrl: './task-board.component.css'
})
export class TaskBoardComponent implements OnInit {
  tasks: TaskItem[] = [];
  loadError = false;
  taskService = inject(TaskService);
  authService = inject(AuthService);

  notifications: NotificationItem[] = [];
  notificationsOpen = false;
  notificationService = inject(NotificationService);

  get completedCount(): number {
    return this.tasks.filter(t => t.isCompleted).length;
  }

  get progressPercent(): number {
    if (this.tasks.length === 0) return 0;
    return Math.round((this.completedCount / this.tasks.length) * 100);
  }

  get unreadCount(): number {
    return this.notifications.filter(n => !n.isRead).length;
  }

  ngOnInit() {
    this.taskService.getTasks().subscribe({
      next: (data) => (this.tasks = data),
      error: () => (this.loadError = true)
    });

    // Notifications are a separate microservice (Tracker.NotificationService).
    // A failure here shouldn't block the task board, so it's kept independent
    // of loadError and just leaves the bell showing zero.
    this.notificationService.getNotifications().subscribe({
      next: (data) => (this.notifications = data),
      error: () => (this.notifications = [])
    });
  }

  toggleNotifications() {
    this.notificationsOpen = !this.notificationsOpen;
  }

  markAsRead(notification: NotificationItem) {
    if (notification.isRead) {
      return;
    }
    notification.isRead = true;
    this.notificationService.markAsRead(notification.id).subscribe({
      error: () => {
        // Roll back on failure so the badge count stays accurate.
        notification.isRead = false;
      }
    });
  }

  logout() {
    this.authService.logout();
  }
}