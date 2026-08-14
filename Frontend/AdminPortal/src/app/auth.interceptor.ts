import {
  HttpErrorResponse,
  HttpInterceptorFn
} from '@angular/common/http';

import { inject } from '@angular/core';

import { environment } from '../environments/environment';

import { AuthService } from './services/auth.service';

import {
  catchError,
  from,
  switchMap,
  throwError
} from 'rxjs';

export const authInterceptor: HttpInterceptorFn = (
  req,
  next
) => {

  const authService =
    inject(AuthService);

  const authBaseUrl =
    environment.authApi.replace(
      /\/api\/v1$/,
      ''
    );

  /*
   * Authentication endpoints must bypass the interceptor.
   *
   * IMPORTANT:
   * /auth/refresh must bypass initialLoadPromise.
   *
   * Otherwise:
   *
   * initialLoadPromise
   *      ↓
   * restoreSession()
   *      ↓
   * refresh()
   *      ↓
   * HttpClient
   *      ↓
   * interceptor
   *      ↓
   * waits for initialLoadPromise
   *      ↓
   * DEADLOCK
   */
  const authBypassPaths = [
    '/token',
    '/auth/refresh',
    '/auth/logout'
  ];

  const isAuthRequest =
    req.url.startsWith(authBaseUrl) &&
    authBypassPaths.some(
      (path) => req.url.endsWith(path)
    );

  if (isAuthRequest) {
    return next(req);
  }

  /*
   * Wait until AuthService has finished restoring
   * the persisted session after a browser reload.
   */
  return from(
    authService.initialLoadPromise
  ).pipe(

    switchMap(() => {

      const token =
        authService.accessToken;

      /*
       * No token.
       *
       * Let the request continue normally.
       * The route guard is responsible for preventing
       * unauthenticated access to protected pages.
       */
      if (!token) {
        return next(req);
      }

      /*
       * Attach the current access token.
       */
      const authReq =
        req.clone({
          setHeaders: {
            Authorization:
              `Bearer ${token}`
          }
        });

      return next(authReq).pipe(

        catchError(
          (error: HttpErrorResponse) => {

            /*
             * Only attempt refresh when the backend
             * explicitly returns 401.
             */
            if (error.status !== 401) {
              return throwError(
                () => error
              );
            }

            /*
             * Try to obtain a new access token.
             */
            return authService.tryRefresh().pipe(

              switchMap((success) => {

                if (
                  !success ||
                  !authService.accessToken
                ) {
                  return throwError(
                    () => error
                  );
                }

                /*
                 * Retry the original request using
                 * the newly refreshed access token.
                 */
                const retryReq =
                  req.clone({
                    setHeaders: {
                      Authorization:
                        `Bearer ${authService.accessToken}`
                    }
                  });

                return next(retryReq);
              })
            );
          }
        )
      );
    })
  );
};