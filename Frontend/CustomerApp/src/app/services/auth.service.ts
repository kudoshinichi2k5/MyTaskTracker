import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
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

  // CÁCH LY BỘ NHỚ: app1 dùng customerapp_session, app2 dùng adminportal_session
  private readonly storageKey = 'customerapp_session'; 
  
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
    return this.http.post<AuthResponse>(`${environment.authApi}/auth/login`, { username, password }).pipe(
      tap((res) => this.saveSession(res)),
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

  logout() {
    const refreshToken = this.session?.refreshToken;
    this.clearSession();
    if (refreshToken) {
      this.http.post(`${environment.authApi}/auth/logout`, { refreshToken }).subscribe({ error: () => {} });
    }
  }

  get accessToken(): string | null { return this.session?.accessToken ?? null; }
  get hasValidToken(): boolean { return !!this.session && this.session.expiresAt > Date.now(); }
}