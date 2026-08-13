import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, catchError, firstValueFrom, map, of, tap, throwError } from 'rxjs';
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

  // CÁCH LY BỘ NHỚ: app1 dùng customerapp_session, app2 dùng adminportal_session
  private readonly storageKey = 'adminportal_session'; 
  
  private session: Session | null = this.loadSession();
  public readonly initialLoadPromise: Promise<boolean>;
  public lastError: string | null = null;

  constructor() {
    this.initialLoadPromise = this.trySilentRefresh();
  }

  private loadSession(): Session | null {
    const raw = localStorage.getItem(this.storageKey);
    if (!raw) return null;
    try { return JSON.parse(raw) as Session; } catch { return null; }
  }

  private saveSession(res: AuthResponse) {
    this.session = {
      accessToken: res.accessToken,
      refreshToken: res.refreshToken,
      expiresAt: new Date(res.expiresAt).getTime(),
      username: res.username,
      roles: res.roles
    };
    localStorage.setItem(this.storageKey, JSON.stringify(this.session));
  }

  private clearSession() {
    this.session = null;
    localStorage.removeItem(this.storageKey);
  }

  private async trySilentRefresh(): Promise<boolean> {
    if (!this.session) return false;

    const isStillValid = await firstValueFrom(this.verifyCurrentAccessToken()).catch(() => false);
    if (!isStillValid) {
      this.clearSession();
      return false;
    }

    if (this.session.expiresAt - Date.now() > 30_000) return true;

    try {
      await firstValueFrom(this.refresh());
      return true;
    } catch {
      this.clearSession();
      return false;
    }
  }

  login(username: string, password: string): Observable<void> {
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
    }>(`${environment.authApi.replace(/\/api\/v1$/, '')}/token`, body.toString(), {
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' }
    }).pipe(
      tap((res) => this.saveSession({
        accessToken: res.accessToken,
        refreshToken: res.refreshToken,
        expiresAt: new Date(Date.now() + res.expiresIn * 1000).toISOString(),
        username: res.username,
        roles: res.roles
      })),
      map(() => void 0),
      catchError((err: HttpErrorResponse) => {
        this.lastError = err.status === 401 ? 'Sai tài khoản hoặc mật khẩu.' : "Lỗi kết nối máy chủ.";
        return throwError(() => err);
      })
    );
  }

  refresh(): Observable<AuthResponse> {
    if (!this.session) return throwError(() => new Error('No session'));
    return this.http.post<AuthResponse>(`${environment.authApi}/auth/refresh`, { refreshToken: this.session.refreshToken })
      .pipe(tap((res) => this.saveSession(res)));
  }

  tryRefresh(): Observable<boolean> {
    return this.refresh().pipe(map(() => true), catchError(() => { this.clearSession(); return of(false); }));
  }

  private verifyCurrentAccessToken(): Observable<boolean> {
    if (!this.session?.accessToken) {
      return of(false);
    }

    return this.http
      .get<{ active: boolean }>(`${environment.authApi.replace(/\/api\/v1$/, '')}/verify`, {
        headers: { Authorization: `Bearer ${this.session.accessToken}` }
      })
      .pipe(
        map((res) => !!res.active),
        catchError(() => of(false))
      );
  }

  logout() {
    const refreshToken = this.session?.refreshToken;
    this.clearSession();
    void this.router.navigate(['/login']);

    if (refreshToken) {
      this.http.post(`${environment.authApi}/auth/logout`, { refreshToken }).subscribe({ error: () => {} });
    }
  }

  get accessToken(): string | null { return this.session?.accessToken ?? null; }
  get hasValidToken(): boolean { return !!this.session && this.session.expiresAt > Date.now(); }
  get username(): string {
    return this.session?.username ?? '';
  }
  get roles(): string[] { return this.session?.roles ?? []; }
  hasRole(role: string): boolean { return this.roles.includes(role); }
}
