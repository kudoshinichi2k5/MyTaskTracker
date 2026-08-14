import { Injectable, inject } from '@angular/core';

import {
  HttpClient,
  HttpErrorResponse
} from '@angular/common/http';

import { Router } from '@angular/router';

import {
  Observable,
  catchError,
  firstValueFrom,
  map,
  of,
  tap,
  throwError
} from 'rxjs';

import { environment } from '../../environments/environment';

interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  username: string;
  roles: string[];
}

interface Session {
  accessToken: string;
  refreshToken: string;
  expiresAt: number;
  username: string;
  roles: string[];
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  /*
   * Keep CustomerApp and AdminPortal sessions isolated.
   */
  private readonly storageKey = 'customerapp_session';

  private session: Session | null = this.loadSession();

  /*
   * Route guards wait for this promise before checking authentication.
   */
  public readonly initialLoadPromise: Promise<boolean>;

  public lastError: string | null = null;

  constructor() {
    this.initialLoadPromise = this.restoreSession();
  }

  /*
   * Restore the persisted session from localStorage.
   */
  private loadSession(): Session | null {
    const raw = localStorage.getItem(this.storageKey);

    if (!raw) {
      return null;
    }

    try {
      const parsed = JSON.parse(raw) as Session;

      if (
        !parsed.accessToken ||
        !parsed.refreshToken ||
        !parsed.expiresAt
      ) {
        return null;
      }

      return parsed;
    } catch {
      localStorage.removeItem(this.storageKey);
      return null;
    }
  }

  /*
   * Persist the authentication session.
   */
  private saveSession(res: AuthResponse): void {
    this.session = {
      accessToken: res.accessToken,
      refreshToken: res.refreshToken,
      expiresAt: new Date(res.expiresAt).getTime(),
      username: res.username,
      roles: res.roles
    };

    localStorage.setItem(
      this.storageKey,
      JSON.stringify(this.session)
    );
  }

  /*
   * Clear the current authentication session.
   */
  private clearSession(): void {
    this.session = null;
    localStorage.removeItem(this.storageKey);
  }

  /*
   * Restore authentication after a browser reload.
   *
   * IMPORTANT:
   * Do not call /verify immediately when the access token is still
   * locally valid. The protected APIs will still validate the opaque
   * token server-side.
   *
   * This prevents a temporary /verify failure from incorrectly
   * logging the user out after a page reload.
   */
  private async restoreSession(): Promise<boolean> {
    if (!this.session) {
      return false;
    }

    const remainingLifetime =
      this.session.expiresAt - Date.now();

    /*
     * Access token is still valid for more than 30 seconds.
     * Keep the existing session.
     */
    if (remainingLifetime > 30_000) {
      return true;
    }

    /*
     * Access token is expired or about to expire.
     * Use the refresh token.
     */
    try {
      await firstValueFrom(this.refresh());

      return true;
    } catch {
      this.clearSession();

      return false;
    }
  }

  /*
   * Login using the OAuth-style password grant endpoint
   * currently implemented by AuthService.
   */
  login(
    username: string,
    password: string
  ): Observable<void> {
    this.lastError = null;

    const body = new URLSearchParams({
      grant_type: 'password',
      username,
      password
    });

    return this.http.post<{
      accessToken: string;
      refreshToken: string;
      tokenType: string;
      expiresIn: number;
      username: string;
      roles: string[];
    }>(
      `${environment.authApi.replace(/\/api\/v1$/, '')}/token`,
      body.toString(),
      {
        headers: {
          'Content-Type':
            'application/x-www-form-urlencoded'
        }
      }
    ).pipe(
      tap((res) => {
        this.saveSession({
          accessToken: res.accessToken,
          refreshToken: res.refreshToken,
          expiresAt: new Date(
            Date.now() + res.expiresIn * 1000
          ).toISOString(),
          username: res.username,
          roles: res.roles
        });
      }),

      map(() => void 0),

      catchError((err: HttpErrorResponse) => {
        this.lastError =
          err.status === 400 || err.status === 401
            ? 'Sai tài khoản hoặc mật khẩu.'
            : 'Lỗi kết nối máy chủ.';

        return throwError(() => err);
      })
    );
  }

  /*
   * Exchange the refresh token for a new access token.
   */
  refresh(): Observable<AuthResponse> {
    if (!this.session?.refreshToken) {
      return throwError(
        () => new Error('No refresh session')
      );
    }

    return this.http.post<AuthResponse>(
      `${environment.authApi}/auth/refresh`,
      {
        refreshToken: this.session.refreshToken
      }
    ).pipe(
      tap((res) => {
        this.saveSession(res);
      })
    );
  }

  /*
   * Used by the HTTP interceptor when an API returns 401.
   */
  tryRefresh(): Observable<boolean> {
    return this.refresh().pipe(
      map(() => true),

      catchError(() => {
        this.clearSession();

        return of(false);
      })
    );
  }

  /*
   * Logout explicitly requested by the user.
   */
  logout(): void {
    const refreshToken =
      this.session?.refreshToken;

    this.clearSession();

    void this.router.navigate(['/login']);

    if (refreshToken) {
      this.http.post(
        `${environment.authApi}/auth/logout`,
        {
          refreshToken
        }
      ).subscribe({
        error: () => {}
      });
    }
  }

  /*
   * Current access token.
   */
  get accessToken(): string | null {
    return this.session?.accessToken ?? null;
  }

  /*
   * Local expiration check.
   *
   * The backend remains responsible for validating the opaque token.
   */
  get hasValidToken(): boolean {
    return !!this.session &&
      this.session.expiresAt > Date.now();
  }

  get username(): string {
    return this.session?.username ?? '';
  }

  get roles(): string[] {
    return this.session?.roles ?? [];
  }

  hasRole(role: string): boolean {
    return this.roles.includes(role);
  }
}