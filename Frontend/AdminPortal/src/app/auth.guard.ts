import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.hasValidToken) {
    return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
  }

  // This was previously commented out ("Tùy chọn: check role ở đây nếu
  // cần" - "optional, check role here if needed"), which meant ANY
  // authenticated user - not just admins - could reach /dashboard. The
  // backend's AdminOnly policy still blocks the actual data (reports,
  // users, roles) for a non-admin, but they'd land on a shell that only
  // ever shows failed 403s instead of a clear "you don't have access."
  if (!authService.hasRole('admin')) {
    return router.parseUrl('/forbidden');
  }

  return true;
};