import {
  HttpErrorResponse,
  HttpInterceptorFn
} from '@angular/common/http';

import { inject } from '@angular/core';

import {
  catchError,
  switchMap,
  throwError,
  from,
  map
} from 'rxjs';

import { environment } from '../environments/environment';
import { AuthService } from './services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  const authBaseUrl = environment.authApi.replace(/\/api\/v1$/, '');

  const isAuthEndpoint =
    req.url.startsWith(authBaseUrl) &&
    (req.url.endsWith('/token') ||
      req.url.endsWith('/auth/refresh') ||
      req.url.endsWith('/auth/logout'));

  // Skip auth headers for login/refresh/logout
  if (isAuthEndpoint) {
    return next(req);
  }

  // Convert the promise into an observable
  return from(authService.initialLoadPromise).pipe(
    map(() => {
      const token = authService.accessToken;
      return token
        ? req.clone({
            setHeaders: { Authorization: `Bearer ${token}` }
          })
        : req;
    }),
    switchMap(authenticatedRequest =>
      next(authenticatedRequest).pipe(
        catchError((error: HttpErrorResponse) => {
          if (error.status !== 401 || !authService.accessToken) {
            return throwError(() => error);
          }

          return authService.tryRefresh().pipe(
            switchMap(success => {
              const refreshedToken = authService.accessToken;
              if (!success || !refreshedToken) {
                return throwError(() => error);
              }

              const retryRequest = req.clone({
                setHeaders: {
                  Authorization: `Bearer ${refreshedToken}`
                }
              });

              return next(retryRequest);
            })
          );
        })
      )
    )
  );
};
