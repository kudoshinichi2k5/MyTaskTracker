import { Injectable, inject } from '@angular/core';
import { OAuthService, AuthConfig } from 'angular-oauth2-oidc';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private oauthService = inject(OAuthService);
  public initialLoadPromise: Promise<boolean>;

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

    return this.oauthService
      .loadDiscoveryDocumentAndTryLogin()
      .then((success) => {
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
    // 1. Lấy token định danh ra trước khi xóa
    const idToken = this.oauthService.getIdToken();
    const issuer = environment.oauth.issuer; 
    const clientId = environment.oauth.clientId;
    const redirectUri = environment.oauth.redirectUri;

    // 2. Truyền `true` để ép thư viện xóa sạch token trong bộ nhớ mà KHÔNG tự động chuyển hướng
    this.oauthService.logOut(true);

    // 3. Tự tay lắp ráp URL Keycloak Logout chống đạn
    const keycloakLogoutEndpoint = `${issuer}/protocol/openid-connect/logout`;
    
    if (idToken) {
      // Ưu tiên dùng id_token_hint nếu có
      window.location.href = `${keycloakLogoutEndpoint}?id_token_hint=${idToken}&post_logout_redirect_uri=${encodeURIComponent(redirectUri)}`;
    } else {
      // Fallback chuẩn OIDC: dùng client_id nếu mất token
      window.location.href = `${keycloakLogoutEndpoint}?client_id=${clientId}&post_logout_redirect_uri=${encodeURIComponent(redirectUri)}`;
    }
  }

  get hasValidToken() {
    return this.oauthService.hasValidAccessToken();
  }
}