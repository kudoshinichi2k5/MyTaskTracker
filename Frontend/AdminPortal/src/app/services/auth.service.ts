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
   * Keep AdminPortal session isolated from CustomerApp.
   */
  private readonly storageKey = 'adminportal_session';

  private session: Session | null = this.loadSession();

  /*
   * AuthGuard waits for this promise before checking authentication.
   */
  public readonly initialLoadPromise: Promise<boolean>;

  public lastError: string | null = null;

  constructor() {
    this.initialLoadPromise = this.restoreSession();
  }

  /*
   * Restore the persisted AdminPortal session.
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
   * Persist authentication data.
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
   * Remove authentication data.
   */
  private clearSession(): void {
    this.session = null;
    localStorage.removeItem(this.storageKey);
  }

  /*
   * Restore the AdminPortal session after browser reload.
   *
   * Do not call /verify for every reload when the access token
   * is still locally unexpired.
   */
  private async restoreSession(): Promise<boolean> {
    if (!this.session) {
      return false;
    }

    const remainingLifetime =
      this.session.expiresAt - Date.now();

    /*
     * The persisted access token is still within its lifetime.
     */
    if (remainingLifetime > 30_000) {
      return true;
    }

    /*
     * Access token is expired or nearly expired.
     * Attempt refresh using the persisted refresh token.
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
   * Login.
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
   * Refresh access token.
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
   * Used by the HTTP interceptor after a 401.
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
   * Explicit logout.
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

  get accessToken(): string | null {
    return this.session?.accessToken ?? null;
  }

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