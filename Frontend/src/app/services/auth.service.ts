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
      requireHttps: false
    };
    
    this.oauthService.configure(authConfig);
    
    // 👇 BỔ SUNG DÒNG NÀY: Ép thư viện lưu Token vào Local Storage
    this.oauthService.setStorage(localStorage);

    return this.oauthService.loadDiscoveryDocumentAndTryLogin();
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