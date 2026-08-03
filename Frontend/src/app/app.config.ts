import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { routes } from './app.routes';
import { authInterceptor } from './auth.interceptor'; // Import interceptor

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    // Đăng ký HttpClient và Interceptor tại đây
    provideHttpClient(withInterceptors([authInterceptor])) 
  ]
};