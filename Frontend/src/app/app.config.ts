import { ApplicationConfig, importProvidersFrom } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { authInterceptor } from './auth.interceptor';
import { OAuthModule, OAuthStorage } from 'angular-oauth2-oidc';

// Khai báo hàm trỏ thẳng vào localStorage của trình duyệt
export function storageFactory(): OAuthStorage {
  return localStorage;
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    importProvidersFrom(OAuthModule.forRoot()),
    // Tiêm Storage toàn cục vào hệ thống
    { provide: OAuthStorage, useFactory: storageFactory } 
  ]
};