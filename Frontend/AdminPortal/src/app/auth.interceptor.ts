import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { environment } from '../environments/environment';
import { AuthService } from './services/auth.service';
import { catchError, switchMap, throwError } from 'rxjs';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const token = authService.accessToken;
  const authBaseUrl = environment.authApi.replace(/\/api\/v1$/, '');

  if (req.url.startsWith(authBaseUrl)) {
    return next(req);
  }

  let authReq = req;
  if (token) {
    authReq = req.clone({
      setHeaders: { Authorization: `Bearer ${token}` }
    });
  }

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      // Nếu API trả về 401 Unauthorized và chưa retry
      if (error.status === 401 && token) {
        return authService.tryRefresh().pipe(
          switchMap((success) => {
            if (success && authService.accessToken) {
              // Refresh thành công, gắn token mới và gọi lại API
              const retriedReq = req.clone({
                setHeaders: { Authorization: `Bearer ${authService.accessToken}` }
              });
              return next(retriedReq);
            }
            // Refresh thất bại, văng lỗi ra để đẩy về trang Login
            return throwError(() => error);
          })
        );
      }
      return throwError(() => error);
    })
  );
};
