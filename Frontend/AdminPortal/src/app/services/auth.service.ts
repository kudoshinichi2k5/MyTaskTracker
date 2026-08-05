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
    this.oauthService.logOut();
  }

  get hasValidToken(): boolean {
    return this.oauthService.hasValidAccessToken();
  }

  /**
   * Keycloak realm role check.
   * AdminPortal is only for users with "admin" role.
   * Server-side enforcement still applies.
   */
  get isAdmin(): boolean {
    const claims = this.oauthService.getIdentityClaims() as { realm_access?: { roles?: string[] } } | null;
    return !!claims?.realm_access?.roles?.includes('admin');
  }
}
