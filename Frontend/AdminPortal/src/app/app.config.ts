import { ApplicationConfig, importProvidersFrom, APP_INITIALIZER } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { authInterceptor } from './auth.interceptor';
import { OAuthModule, OAuthStorage } from 'angular-oauth2-oidc';
import { AuthService } from './services/auth.service';

/**
 * Same reasoning as CustomerApp's app.config.ts: both apps share one origin
 * under sub-application deployment (/app1, /app2), so localStorage must be
 * namespaced per app or they'll silently overwrite each other's tokens.
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
  return new PrefixedStorage('adminportal_');
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