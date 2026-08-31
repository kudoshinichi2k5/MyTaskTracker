import { inject } from '@angular/core';

import {
  CanActivateFn,
  Router
} from '@angular/router';

import { AuthService } from './services/auth.service';

export const authGuard: CanActivateFn = async (
  route,
  state
) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  /*
   * Restore the persisted AdminPortal session
   * before checking authentication.
   */
  await authService.initialLoadPromise;

  if (!authService.hasValidToken) {
    return router.createUrlTree(
      ['/login'],
      {
        queryParams: {
          returnUrl: state.url
        }
      }
    );
  }

  /*
   * AdminPortal is restricted to users
   * with the admin role.
   */
  if (!authService.hasRole('admin')) {
    return router.parseUrl('/forbidden');
  }

  return true;
};