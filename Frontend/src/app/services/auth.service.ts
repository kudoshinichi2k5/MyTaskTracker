import { Injectable, inject } from '@angular/core';
import { OAuthService, AuthConfig } from 'angular-oauth2-oidc';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private oauthService = inject(OAuthService);

  constructor() {
    this.configureOAuth();
  }

  private configureOAuth() {
    const authConfig: AuthConfig = {
      issuer: environment.oauth.issuer,
      redirectUri: environment.oauth.redirectUri,
      clientId: environment.oauth.clientId,
      scope: environment.oauth.scope,
      responseType: 'code',
      requireHttps: false // Tắt cho môi trường Local
    };
    this.oauthService.configure(authConfig);
    this.oauthService.loadDiscoveryDocumentAndTryLogin();
  }

  login() {
    this.oauthService.initCodeFlow(); // Kích hoạt luồng chuyển hướng
  }

  logout() {
    this.oauthService.logOut();
  }

  get hasValidToken() {
    return this.oauthService.hasValidAccessToken();
  }
}