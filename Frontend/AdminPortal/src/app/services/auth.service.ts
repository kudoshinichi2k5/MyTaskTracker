import { Injectable } from '@angular/core';
import { OAuthService, AuthConfig } from 'angular-oauth2-oidc';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  public initialLoadPromise: Promise<boolean>;
  public lastError: string | null = null;

  constructor(private oauthService: OAuthService) {
    this.initialLoadPromise = this.configureOAuth();
  }

  private configureOAuth(): Promise<boolean> {
    const authConfig: AuthConfig = {
      issuer: environment.oauth.issuer,
      redirectUri: environment.oauth.redirectUri,
      clientId: environment.oauth.clientId,
      scope: environment.oauth.scope,
      responseType: 'code',
      requireHttps: environment.production,
      strictDiscoveryDocumentValidation: false
    };

    this.oauthService.configure(authConfig);

    return this.oauthService
      .loadDiscoveryDocumentAndTryLogin()
      .then((success) => {
        // Clean up URL after OAuth redirect
        if (window.location.search.includes('code=') || window.location.search.includes('state=')) {
          const cleanUrl = window.location.origin + window.location.pathname;
          window.history.replaceState({}, document.title, cleanUrl);
        }
        this.lastError = null;
        return success;
      })
      .catch((err) => {
        console.error('[AuthService] OAuth initialization failed:', err);
        this.lastError = "We couldn't reach the sign-in service. Please try again in a moment.";
        return false;
      });
  }

  login() {
    try {
      this.lastError = null;
      this.oauthService.initCodeFlow();
    } catch (err) {
      console.error('[AuthService] Failed to start SSO redirect:', err);
      this.lastError = 'Unable to start sign-in. Please check your connection and try again.';
    }
  }

  logout() {
    // See CustomerApp's AuthService for the full explanation: logOut() only
    // sends id_token_hint when an ID token is present in storage, with no
    // fallback, and Keycloak then rejects the request with
    // "Missing parameters: id_token_hint".
    if (this.oauthService.getIdToken()) {
      this.oauthService.logOut();
      return;
    }

    this.logoutWithoutIdToken();
  }

  private logoutWithoutIdToken() {
    this.oauthService.logOut(true);

    const logoutUrl = this.oauthService.logoutUrl;
    if (!logoutUrl) {
      window.location.href = environment.oauth.redirectUri;
      return;
    }

    const params = new URLSearchParams({
      client_id: environment.oauth.clientId,
      post_logout_redirect_uri: environment.oauth.redirectUri
    });
    window.location.href = `${logoutUrl}${logoutUrl.includes('?') ? '&' : '?'}${params.toString()}`;
  }

  get hasValidToken(): boolean {
    return this.oauthService.hasValidAccessToken();
  }

  /**
   * Keycloak realm role check.
   * AdminPortal is only for users with "admin" role.
   * Server-side enforcement still applies.
   *
   * Reads realm_access.roles from the ACCESS token, not
   * getIdentityClaims() (which decodes the ID token). Tracker.TaskService's
   * AdminOnly policy maps roles from the access token it receives as the
   * Bearer header (see Program.cs's OnTokenValidated) - those are two
   * different tokens, and Keycloak doesn't guarantee they carry the same
   * claims unless every relevant client scope's protocol mapper is
   * configured to include realm roles in both. Checking the ID token here
   * risks a mismatch: a user this guard sends to /forbidden might actually
   * be allowed by the API (extra clicks to reach a working screen), or one
   * it lets through might get 403'd by every request (dead-end dashboard
   * with only failed loads). Checking the access token directly makes this
   * guard's decision match what the API will do, by construction.
   */
  get isAdmin(): boolean {
    const token = this.oauthService.getAccessToken();
    if (!token) return false;

    try {
      const payloadSegment = token.split('.')[1];
      const normalized = payloadSegment.replace(/-/g, '+').replace(/_/g, '/');
      const payload = JSON.parse(atob(normalized));
      const roles: string[] = payload?.realm_access?.roles ?? [];
      return roles.includes('admin');
    } catch (err) {
      console.error('[AuthService] Failed to decode access token roles:', err);
      return false;
    }
  }
}