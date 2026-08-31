import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface NotificationItem {
  id: number;
  message: string;
  isRead: boolean;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private http = inject(HttpClient);
  private apiUrl = environment.notificationApi;

  getNotifications(): Observable<NotificationItem[]> {
    return this.http.get<NotificationItem[]>(`${this.apiUrl}/notifications`).pipe(
      catchError((err: HttpErrorResponse) => {
        console.error('[NotificationService] Failed to load notifications:', err.status, err.message);
        return throwError(() => err);
      })
    );
  }

  getUnreadCount(): Observable<number> {
    return this.http.get<{ count: number }>(`${this.apiUrl}/notifications/unread-count`).pipe(
      map((res) => res.count),
      catchError((err: HttpErrorResponse) => {
        console.error('[NotificationService] Failed to load unread count:', err.status, err.message);
        return throwError(() => err);
      })
    );
  }

  markAsRead(id: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/notifications/${id}/read`, {});
  }
}