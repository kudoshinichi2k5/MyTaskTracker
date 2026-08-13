import {
  HttpErrorResponse,
  HttpInterceptorFn
} from '@angular/common/http';

import { inject } from '@angular/core';

import {
  catchError,
  throwError
} from 'rxjs';

import { environment } from '../environments/environment';
import { AuthService } from './services/auth.service';

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

  /*
   * /token, refresh and logout don't need
   * an access token.
   */
  if (
    isAuthEndpoint &&
    !req.url.endsWith('/verify')
  ) {
    return next(req);
  }

  /*
   * /verify is called by AuthService while
   * initialLoadPromise is being resolved.
   *
   * Therefore it must NEVER wait for
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

  /*
   * Normal protected API requests.
   *
   * At this point AuthService has already
   * completed its initial session validation.
   */
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
            catchError(() =>
              throwError(
                () => error
              )
            ),
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
};