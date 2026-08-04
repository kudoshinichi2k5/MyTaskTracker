import { Injectable, inject } from '@angular/core';
import { OAuthService, AuthConfig } from 'angular-oauth2-oidc';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private oauthService = inject(OAuthService);
  public initialLoadPromise: Promise<boolean>; 

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

      return this.oauthService.loadDiscoveryDocumentAndTryLogin().then((success) => {
        // Dọn ?code=&state= khỏi URL ngay sau khi xử lý xong.
        // Nếu không dọn, F5 lại trang sẽ gửi lại authorization code CŨ (đã dùng 1 lần)
        // -> Keycloak trả 400 invalid_grant.
        if (window.location.search.includes('code=') || window.location.search.includes('state=')) {
          const cleanUrl = window.location.origin + window.location.pathname;
          window.history.replaceState({}, document.title, cleanUrl);
        }
        return success;
      });
  }

  login() {
    this.oauthService.initCodeFlow();
  }

  logout() {
    this.oauthService.logOut();
  }

  get hasValidToken() {
    return this.oauthService.hasValidAccessToken();
  }
}