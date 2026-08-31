import {
  HttpErrorResponse,
  HttpInterceptorFn
} from '@angular/common/http';

import { inject } from '@angular/core';

import {
  catchError,
  from,
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

  /*
   * These requests are part of the authentication
   * bootstrap process and must not wait for
   * initialLoadPromise.
   */
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
   * /verify is special:
   *
   * AuthService calls /verify while resolving
   * initialLoadPromise.
   *
   * Therefore /verify must directly use the
   * stored token instead of waiting for
   * initialLoadPromise.
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

  /*
   * Normal application requests wait until
   * session restoration has finished.
   */
  return from(
    authService.initialLoadPromise
  ).pipe(
    switchMap(() => {
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
    })
  );
};