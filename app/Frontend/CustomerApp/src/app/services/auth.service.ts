import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import {
  Observable,
  catchError,
  finalize,
  firstValueFrom,
  map,
  of,
  shareReplay,
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

interface PendingAuthorization {
  verifier: string;
  returnUrl: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private readonly storageKey = 'customerapp_session';
  private session: Session | null = this.loadSession();
  private refreshRequest: Observable<AuthResponse> | null = null;

  public readonly initialLoadPromise: Promise<boolean>;
  public lastError: string | null = null;

  constructor() {
    this.initialLoadPromise = this.restoreSession();
  }

  private loadSession(): Session | null {
    const raw = localStorage.getItem(this.storageKey);
    if (!raw) return null;

    try {
      const parsed = JSON.parse(raw) as Session;
      return parsed.accessToken && parsed.refreshToken && parsed.expiresAt
        ? parsed
        : null;
    } catch {
      localStorage.removeItem(this.storageKey);
      return null;
    }
  }

  private saveSession(res: AuthResponse): void {
    this.session = {
      accessToken: res.accessToken,
      refreshToken: res.refreshToken,
      expiresAt: new Date(res.expiresAt).getTime(),
      username: res.username,
      roles: res.roles
    };
    localStorage.setItem(this.storageKey, JSON.stringify(this.session));
  }

  private clearSession(): void {
    this.session = null;
    localStorage.removeItem(this.storageKey);
  }

  private async restoreSession(): Promise<boolean> {
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

  /* Starts Authorization Code + PKCE. The SPA never posts credentials to /connect/token. */
  beginLogin(returnUrl = '/tasks'): void {
    void this.startAuthorization(returnUrl).catch(() => {
      this.lastError = 'Unable to start sign-in.';
    });
  }

  private async startAuthorization(returnUrl: string): Promise<void> {
    this.lastError = null;
    const verifier = this.createRandomValue(64);
    const state = this.createRandomValue(32);
    const challenge = await this.createCodeChallenge(verifier);

    sessionStorage.setItem(
      `oauth_pkce:${state}`,
      JSON.stringify({ verifier, returnUrl: this.safeReturnUrl(returnUrl) })
    );

    const authorizeUrl = new URL(`${this.oauthBaseUrl}/connect/authorize`);
    authorizeUrl.search = new URLSearchParams({
      response_type: 'code',
      client_id: environment.oauthClientId,
      redirect_uri: this.redirectUri,
      scope: 'api',
      state,
      code_challenge: challenge,
      code_challenge_method: 'S256'
    }).toString();
    window.location.assign(authorizeUrl.toString());
  }

  completeAuthorization(code: string, state: string): Observable<string> {
    const raw = sessionStorage.getItem(`oauth_pkce:${state}`);
    sessionStorage.removeItem(`oauth_pkce:${state}`);
    if (!raw) return throwError(() => new Error('Missing or invalid OAuth state.'));

    let pending: PendingAuthorization;
    try {
      pending = JSON.parse(raw) as PendingAuthorization;
    } catch {
      return throwError(() => new Error('Invalid OAuth state.'));
    }

    const body = new URLSearchParams({
      grant_type: 'authorization_code',
      client_id: environment.oauthClientId,
      code,
      redirect_uri: this.redirectUri,
      code_verifier: pending.verifier
    });

    return this.http.post<AuthResponse>(
      `${this.oauthBaseUrl}/connect/token`,
      body.toString(),
      { headers: { 'Content-Type': 'application/x-www-form-urlencoded' } }
    ).pipe(
      tap((res) => this.saveSession(res)),
      map(() => this.safeReturnUrl(pending.returnUrl))
    );
  }

  /* Account creation is followed by the same authorization-code flow. */
  register(username: string, email: string, password: string): Observable<void> {
    this.lastError = null;
    return this.http.post<{ username: string }>(
      `${environment.authApi}/auth/register`,
      { username, email, password }
    ).pipe(
      map(() => void 0),
      catchError((err: HttpErrorResponse) => {
        this.lastError = err.status === 400
          ? this.extractValidationError(err) ?? 'Please check your details and try again.'
          : 'Unable to reach the server.';
        return throwError(() => err);
      })
    );
  }

  private extractValidationError(err: HttpErrorResponse): string | null {
    const errors = err.error?.errors as Record<string, string[]> | undefined;
    if (!errors) return null;
    const firstKey = Object.keys(errors)[0];
    return firstKey ? errors[firstKey]?.[0] ?? null : null;
  }

  refresh(): Observable<AuthResponse> {
    if (!this.session?.refreshToken) {
      return throwError(() => new Error('No refresh session'));
    }
    if (this.refreshRequest) return this.refreshRequest;

    const body = new URLSearchParams({
      grant_type: 'refresh_token',
      client_id: environment.oauthClientId,
      refresh_token: this.session.refreshToken
    });
    this.refreshRequest = this.http.post<AuthResponse>(
      `${this.oauthBaseUrl}/connect/token`,
      body.toString(),
      { headers: { 'Content-Type': 'application/x-www-form-urlencoded' } }
    ).pipe(
      tap((res) => this.saveSession(res)),
      finalize(() => { this.refreshRequest = null; }),
      shareReplay({ bufferSize: 1, refCount: false })
    );
    return this.refreshRequest;
  }

  tryRefresh(): Observable<boolean> {
    return this.refresh().pipe(
      map(() => true),
      catchError(() => {
        this.clearSession();
        return of(false);
      })
    );
  }

  logout(): void {
    const refreshToken = this.session?.refreshToken;
    this.clearSession();
    void this.router.navigate(['/login']);
    if (!refreshToken) return;

    const body = new URLSearchParams({
      client_id: environment.oauthClientId,
      token: refreshToken,
      token_type_hint: 'refresh_token'
    });
    this.http.post(`${this.oauthBaseUrl}/connect/revocation`, body.toString(), {
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' }
    }).subscribe({ error: () => {} });
  }

  get accessToken(): string | null { return this.session?.accessToken ?? null; }
  get hasValidToken(): boolean { return !!this.session && this.session.expiresAt > Date.now(); }
  get username(): string { return this.session?.username ?? ''; }
  get roles(): string[] { return this.session?.roles ?? []; }
  hasRole(role: string): boolean { return this.roles.includes(role); }

  private get oauthBaseUrl(): string { return environment.authApi.replace(/\/api\/v1$/, ''); }
  private get redirectUri(): string { return `${window.location.origin}/oauth/callback`; }

  private createRandomValue(byteLength: number): string {
    const bytes = crypto.getRandomValues(new Uint8Array(byteLength));
    let binary = '';
    bytes.forEach((byte) => (binary += String.fromCharCode(byte)));
    return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=/g, '');
  }

  private async createCodeChallenge(verifier: string): Promise<string> {
    const digest = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(verifier));
    let binary = '';
    new Uint8Array(digest).forEach((byte) => (binary += String.fromCharCode(byte)));
    return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=/g, '');
  }

  private safeReturnUrl(returnUrl: string): string {
    return returnUrl.startsWith('/') && !returnUrl.startsWith('//') ? returnUrl : '/tasks';
  }
}
