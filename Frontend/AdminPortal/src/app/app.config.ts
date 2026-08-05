import { ApplicationConfig, importProvidersFrom, APP_INITIALIZER } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { authInterceptor } from './auth.interceptor';
import { OAuthModule, OAuthStorage } from 'angular-oauth2-oidc';
import { AuthService } from './services/auth.service';

export function storageFactory(): OAuthStorage {
  return localStorage;
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