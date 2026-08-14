import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest
} from '@angular/common/http';

import { Injectable } from '@angular/core';

import { Observable, catchError, switchMap, throwError } from 'rxjs';

import { AuthService } from './services/auth.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {

  constructor(
    private readonly authService: AuthService
  ) {}

  intercept(
    request: HttpRequest<unknown>,
    next: HttpHandler
  ): Observable<HttpEvent<unknown>> {

    /*
     * Wait until AuthService has finished restoring the
     * persisted session after a browser reload.
     */
    return this.waitForSession(request, next);
  }

  private waitForSession(
    request: HttpRequest<unknown>,
    next: HttpHandler
  ): Observable<HttpEvent<unknown>> {

    return new Observable<HttpEvent<unknown>>((subscriber) => {

      this.authService.initialLoadPromise
        .then(() => {

          const accessToken =
            this.authService.accessToken;

          /*
           * Login / token endpoints must be allowed through
           * without an Authorization header.
           */
          if (
            !accessToken ||
            this.isAuthenticationRequest(request)
          ) {
            next.handle(request).subscribe({
              next: (event) => subscriber.next(event),
              error: (error) => subscriber.error(error),
              complete: () => subscriber.complete()
            });

            return;
          }

          /*
           * Attach the current access token.
           */
          const authenticatedRequest =
            request.clone({
              setHeaders: {
                Authorization: `Bearer ${accessToken}`
              }
            });

          next.handle(authenticatedRequest)
            .pipe(
              catchError((error: HttpErrorResponse) => {

                /*
                 * If the access token is rejected, try the
                 * refresh token once.
                 */
                if (error.status !== 401) {
                  return throwError(() => error);
                }

                return this.authService.tryRefresh()
                  .pipe(
                    switchMap((refreshed) => {

                      if (!refreshed) {
                        return throwError(() => error);
                      }

                      const newAccessToken =
                        this.authService.accessToken;

                      if (!newAccessToken) {
                        return throwError(() => error);
                      }

                      const retryRequest =
                        request.clone({
                          setHeaders: {
                            Authorization:
                              `Bearer ${newAccessToken}`
                          }
                        });

                      return next.handle(retryRequest);
                    })
                  );
              })
            )
            .subscribe({
              next: (event) => subscriber.next(event),
              error: (error) => subscriber.error(error),
              complete: () => subscriber.complete()
            });

        })
        .catch((error) => {
          subscriber.error(error);
        });
    });
  }

  private isAuthenticationRequest(
    request: HttpRequest<unknown>
  ): boolean {

    const url = request.url.toLowerCase();

    return (
      url.includes('/token') ||
      url.includes('/auth/refresh') ||
      url.includes('/auth/logout')
    );
  }
}