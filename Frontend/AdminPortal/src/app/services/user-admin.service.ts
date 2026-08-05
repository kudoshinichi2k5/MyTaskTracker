import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, OperatorFunction, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface AdminUser {
  id: string;
  username: string;
  email: string | null;
  enabled: boolean;
  roles: string[];
}

@Injectable({ providedIn: 'root' })
export class UserAdminService {
  private http = inject(HttpClient);
  // Users/Roles endpoints live on Tracker.TaskService itself (under
  // /api/v1/admin) rather than a separate service, since they're a thin
  // proxy over Keycloak's Admin API guarded by the same AdminOnly policy
  // as the existing /api/v1/tasks/admin/summary report endpoint.
  private apiUrl = environment.taskApi;

  getUsers(): Observable<AdminUser[]> {
    return this.http
      .get<AdminUser[]>(`${this.apiUrl}/admin/users`)
      .pipe(this.handleError<AdminUser[]>('load users'));
  }

  getRoles(): Observable<string[]> {
    return this.http
      .get<string[]>(`${this.apiUrl}/admin/roles`)
      .pipe(this.handleError<string[]>('load roles'));
  }

  assignRole(userId: string, role: string): Observable<void> {
    return this.http
      .post<void>(`${this.apiUrl}/admin/users/${userId}/roles/${role}`, {})
      .pipe(this.handleError<void>(`assign role "${role}"`));
  }

  removeRole(userId: string, role: string): Observable<void> {
    return this.http
      .delete<void>(`${this.apiUrl}/admin/users/${userId}/roles/${role}`)
      .pipe(this.handleError<void>(`remove role "${role}"`));
  }

  private handleError<T>(action: string): OperatorFunction<T, T> {
    return catchError((err: HttpErrorResponse) => {
      console.error(`[UserAdminService] Failed to ${action}:`, err.status, err.message);
      return throwError(() => err) as Observable<T>;
    });
  }
}