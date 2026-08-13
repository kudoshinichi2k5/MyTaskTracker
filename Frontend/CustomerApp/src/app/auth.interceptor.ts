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
   * These endpoints MUST bypass the normal
   * authentication initialization flow.
   *
   * /verify is especially important:
   *
   * AuthService.initialLoadPromise
   *       -> /verify
   *
   * Therefore /verify cannot wait for
   * initialLoadPromise or it creates a deadlock.
   */
  const authBypassPaths = [
    '/token',
    '/verify',
    '/auth/refresh',
    '/auth/logout'
  ];

  const isAuthEndpoint =
    req.url.startsWith(authBaseUrl) &&
    authBypassPaths.some(
      path => req.url.endsWith(path)
    );

  if (isAuthEndpoint) {
    /*
     * /verify still needs the current access token.
     */
    if (req.url.endsWith('/verify')) {
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

    return next(req);
  }

  /*
   * Wait until AuthService finishes restoring
   * and validating the persisted session.
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
            /*
             * Only attempt refresh for an
             * authenticated request.
             */
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

                  const retryRequest =
                    req.clone({
                      setHeaders: {
                        Authorization:
                          `Bearer ${refreshedToken}`
                      }
                    });

                  return next(
                    retryRequest
                  );
                })
              );
          }
        )
      );
    })
  );
};  