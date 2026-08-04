import { Injectable, inject } from '@angular/core';
import { OAuthService, AuthConfig } from 'angular-oauth2-oidc';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private oauthService = inject(OAuthService);
  public initialLoadPromise: Promise<boolean>;

  /**
   * Set when discovery/token-exchange fails during bootstrap, or when
   * initCodeFlow() cannot start the redirect. The login page reads this
   * to show a message instead of just going blank.
   */
  public lastError: string | null = null;

  constructor() {
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

    // IMPORTANT: loadDiscoveryDocumentAndTryLogin() rejects if the discovery
    // document can't be fetched or the authorization-code exchange fails
    // (e.g. IdP unreachable, clock skew, reused/expired code). Because this
    // promise is awaited by an APP_INITIALIZER, an unhandled rejection here
    // used to stop Angular from ever bootstrapping <app-root> -- the browser
    // tab just went blank with nothing but a console error. We now catch the
    // failure, record it, and resolve with `false` so the app always
    // finishes bootstrapping and the router can fall back to /login.
    return this.oauthService
      .loadDiscoveryDocumentAndTryLogin()
      .then((success) => {
        // Strip ?code=&state= from the URL right after we're done with them.
        // If we leave them, an F5 on this page would resubmit the SAME
        // (already-used) authorization code and Keycloak would reply with
        // 400 invalid_grant.
        if (window.location.search.includes('code=') || window.location.search.includes('state=')) {
          const cleanUrl = window.location.origin + window.location.pathname;
          window.history.replaceState({}, document.title, cleanUrl);
        }
        this.lastError = null;
        return success;
      })
      .catch((err) => {
        console.error('[AuthService] OAuth initialization failed:', err);
        this.lastError = 'We couldn\'t reach the sign-in service. Please try again in a moment.';
        // Never let the app bootstrap hang/fail because of this - the user
        // is simply "not logged in", which the auth guard already handles.
        return false;
      });
  }

  login() {
    try {
      this.lastError = null;
      this.oauthService.initCodeFlow();
    } catch (err) {
      // initCodeFlow() can throw synchronously (e.g. Web Crypto/PKCE is
      // unavailable because the app is served over plain HTTP on a
      // non-"localhost" host). Previously this exception had nowhere to go:
      // the click handler died mid-navigation and the login page was left
      // rendered but unresponsive, which read as "the page disappears".
      console.error('[AuthService] Failed to start SSO redirect:', err);
      this.lastError = 'Unable to start sign-in. Please check your connection and try again.';
    }
  }

  logout() {
    this.oauthService.logOut();
  }

  get hasValidToken() {
    return this.oauthService.hasValidAccessToken();
  }
}