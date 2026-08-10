import { ApplicationConfig, importProvidersFrom, APP_INITIALIZER } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { authInterceptor } from './auth.interceptor';
import { OAuthModule, OAuthStorage } from 'angular-oauth2-oidc';
import { AuthService } from './services/auth.service';

/**
 * CustomerApp and AdminPortal are deployed as sub-applications (/app1,
 * /app2) under the SAME origin in Testing/Staging/Production. Browser
 * storage (localStorage) is scoped by ORIGIN ONLY, not by path - so without
 * a namespace, both apps would read/write the exact same "access_token" /
 * "id_token" keys angular-oauth2-oidc uses internally and silently stomp on
 * each other's session. This is exactly what caused Admin Portal to send
 * the Customer App's id_token_hint to Keycloak on logout.
 */
class PrefixedStorage implements OAuthStorage {
  constructor(private prefix: string) {}

  getItem(key: string): string | null {
    return localStorage.getItem(this.prefix + key);
  }

  removeItem(key: string): void {
    localStorage.removeItem(this.prefix + key);
  }

  setItem(key: string, data: string): void {
    localStorage.setItem(this.prefix + key, data);
  }
}

export function storageFactory(): OAuthStorage {
  return new PrefixedStorage('customerapp_');
}

export function initializeOAuth(authService: AuthService) {
  return () => authService.initialLoadPromise;
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    importProvidersFrom(OAuthModule.forRoot()),
    { provide: OAuthStorage, useFactory: storageFactory },
    {
      provide: APP_INITIALIZER,
      useFactory: initializeOAuth,
      deps: [AuthService],
      multi: true
    }
  ]
};