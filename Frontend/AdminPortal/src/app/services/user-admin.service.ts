import { Injectable, inject } from '@angular/core';

import {
  HttpClient,
  HttpErrorResponse
} from '@angular/common/http';

import {
  Observable,
  OperatorFunction,
  throwError
} from 'rxjs';

import { catchError } from 'rxjs/operators';

import { environment } from '../../environments/environment';

export interface AdminUser {
  id: string;
  username: string;
  email: string | null;
  enabled: boolean;
  roles: string[];
}

@Injectable({
  providedIn: 'root'
})
export class UserAdminService {
  private readonly http = inject(HttpClient);

  /**
   * Users and roles are exposed by TaskService's
   * AdminOnly-protected endpoints.
   *
   * TaskService:
   *   GET    /api/v1/admin/users
   *   GET    /api/v1/admin/roles
   *   POST   /api/v1/admin/users/{userId}/roles/{roleName}
   *   DELETE /api/v1/admin/users/{userId}/roles/{roleName}
   */
  private readonly apiUrl =
    `${environment.taskApi}/admin`;

  getUsers(): Observable<AdminUser[]> {
    return this.http
      .get<AdminUser[]>(
        `${this.apiUrl}/users`
      )
      .pipe(
        this.handleError<AdminUser[]>(
          'load users'
        )
      );
  }

  getRoles(): Observable<string[]> {
    return this.http
      .get<string[]>(
        `${this.apiUrl}/roles`
      )
      .pipe(
        this.handleError<string[]>(
          'load roles'
        )
      );
  }

  assignRole(
    userId: string,
    role: string
  ): Observable<void> {
    return this.http
      .post<void>(
        `${this.apiUrl}/users/${encodeURIComponent(userId)}/roles/${encodeURIComponent(role)}`,
        {}
      )
      .pipe(
        this.handleError<void>(
          `assign role "${role}"`
        )
      );
  }

  removeRole(
    userId: string,
    role: string
  ): Observable<void> {
    return this.http
      .delete<void>(
        `${this.apiUrl}/users/${encodeURIComponent(userId)}/roles/${encodeURIComponent(role)}`
      )
      .pipe(
        this.handleError<void>(
          `remove role "${role}"`
        )
      );
  }

  private handleError<T>(
    action: string
  ): OperatorFunction<T, T> {
    return catchError(
      (error: HttpErrorResponse) => {
        console.error(
          `[UserAdminService] Failed to ${action}`,
          {
            status: error.status,
            message: error.message,
            error: error.error
          }
        );

        return throwError(
          () => error
        );
      }
    );
  }
}