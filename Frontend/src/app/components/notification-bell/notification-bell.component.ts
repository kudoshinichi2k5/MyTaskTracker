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
  unreadCount = 0;
  isOpen = false;
  private listLoaded = false;

  private notificationService = inject(NotificationService);
  private elementRef = inject(ElementRef);

  ngOnInit() {
    // Only the count is needed to render the badge, so we use the
    // dedicated /unread-count endpoint instead of pulling the full list
    // on every page load - the full list is only fetched once the
    // dropdown is actually opened (see toggle()).
    this.notificationService.getUnreadCount().subscribe({
      next: (count) => (this.unreadCount = count),
      error: () => (this.unreadCount = 0)
    });
  }

  toggle() {
    this.isOpen = !this.isOpen;
    if (this.isOpen && !this.listLoaded) {
      this.load();
    }
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
    this.unreadCount = Math.max(0, this.unreadCount - 1);
    this.notificationService.markAsRead(notification.id).subscribe({
      error: () => {
        notification.isRead = false;
        this.unreadCount += 1;
      }
    });
  }

  private load() {
    this.notificationService.getNotifications().subscribe({
      next: (data) => {
        this.notifications = data;
        this.listLoaded = true;
      },
      error: () => (this.notifications = [])
    });
  }
}