import { Component, ElementRef, HostListener, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationService, NotificationItem } from '../../services/notification.service';

@Component({
  selector: 'app-notification-bell',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notification-bell.component.html',
  styleUrl: './notification-bell.component.css'
})
export class NotificationBellComponent implements OnInit {
  notifications: NotificationItem[] = [];
  isOpen = false;

  private notificationService = inject(NotificationService);
  private elementRef = inject(ElementRef);

  get unreadCount(): number {
    return this.notifications.filter((n) => !n.isRead).length;
  }

  ngOnInit() {
    this.load();
  }

  toggle() {
    this.isOpen = !this.isOpen;
  }

  // Close the dropdown when the user clicks anywhere outside of it.
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    if (this.isOpen && !this.elementRef.nativeElement.contains(event.target)) {
      this.isOpen = false;
    }
  }

  markAsRead(notification: NotificationItem) {
    if (notification.isRead) return;

    notification.isRead = true; // optimistic update
    this.notificationService.markAsRead(notification.id).subscribe({
      error: () => (notification.isRead = false)
    });
  }

  private load() {
    this.notificationService.getNotifications().subscribe({
      next: (data) => (this.notifications = data),
      error: () => (this.notifications = [])
    });
  }
}