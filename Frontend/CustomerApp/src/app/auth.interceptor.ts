import {
  HttpErrorResponse,
  HttpInterceptorFn
} from '@angular/common/http';

import { inject } from '@angular/core';

import { catchError, switchMap, throwError } from 'rxjs';

import { environment } from '../environments/environment';
import { AuthService } from './services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  const token = authService.accessToken;

  const authBaseUrl = environment.authApi.replace(
    /\/api\/v1$/,
    ''
  );

  const authBypassPaths = [
    '/token',
    '/auth/refresh',
    '/auth/logout'
  ];

  const isAuthRequest =
    req.url.startsWith(authBaseUrl);

  const shouldBypassAuth =
    isAuthRequest &&
    authBypassPaths.some(path =>
      req.url.endsWith(path)
    );

  if (shouldBypassAuth) {
    return next(req);
  }

  const authReq = token
    ? req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      })
    : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401 || !token) {
        return throwError(() => error);
      }

      return authService.tryRefresh().pipe(
        switchMap(success => {
          const refreshedToken =
            authService.accessToken;

          if (!success || !refreshedToken) {
            return throwError(() => error);
          }

          const retryReq = req.clone({
            setHeaders: {
              Authorization:
                `Bearer ${refreshedToken}`
            }
          });

          return next(retryReq);
        })
      );
    })
  );
};