import {
  HttpErrorResponse,
  HttpInterceptorFn
} from '@angular/common/http';

import { inject } from '@angular/core';

import {
  catchError,
  switchMap,
  throwError
} from 'rxjs';

import { environment } from '../environments/environment';
import { AuthService } from './services/auth.service';

export const authInterceptor: HttpInterceptorFn = (
  req,
  next
) => {
  const authService = inject(AuthService);

  const authBaseUrl =
    environment.authApi.replace(
      /\/api\/v1$/,
      ''
    );

  const bypassPaths = [
    '/token',
    '/verify',
    '/auth/refresh',
    '/auth/logout'
  ];

  const isAuthEndpoint =
    req.url.startsWith(authBaseUrl) &&
    bypassPaths.some(
      path => req.url.endsWith(path)
    );

  /*
   * /verify is used during application startup.
   * Do not wait for initialLoadPromise here.
   */
  if (
    isAuthEndpoint &&
    req.url.endsWith('/verify')
  ) {
    const token =
      authService.accessToken;

    if (!token) {
      return next(req);
    }

    return next(
      req.clone({
        setHeaders: {
          Authorization:
            `Bearer ${token}`
        }
      })
    );
  }

  if (isAuthEndpoint) {
    return next(req);
  }

  const token =
    authService.accessToken;

  const authenticatedRequest =
    token
      ? req.clone({
          setHeaders: {
            Authorization:
              `Bearer ${token}`
          }
        })
      : req;

  return next(
    authenticatedRequest
  ).pipe(
    catchError(
      (error: HttpErrorResponse) => {
        if (
          error.status !== 401 ||
          !token
        ) {
          return throwError(
            () => error
          );
        }

        return authService
          .tryRefresh()
          .pipe(
            switchMap(success => {
              const refreshedToken =
                authService.accessToken;

              if (
                !success ||
                !refreshedToken
              ) {
                return throwError(
                  () => error
                );
              }

              return next(
                req.clone({
                  setHeaders: {
                    Authorization:
                      `Bearer ${refreshedToken}`
                  }
                })
              );
            })
          );
      }
    )
  );
};